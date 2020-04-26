import * as lodash from 'lodash'
import * as net from 'net'
import { PassThrough, Readable, Writable, pipeline, Transform, TransformCallback } from 'stream'

import * as ScoresRepo from './ScoresRepo'
import { multiElo, initialScore } from './Elo'

enum Codes {
  noop = 0, // [0]
  start = 1, // [(1), (playerNumber: int), (totalNumberOfPlayers: int)]
  newPlayerDestination = 2, // [(2), (positionX: float), (timeWhenReach: long), (playerNumber: int)]
  newVoters = 3, // [3, ...voters: [id: int, positionX: float]]
  guessTime = 5, // to server: [5, guessedTime: int], from server: [5, deltaGuess: int]
  projectileFired = 6, // [(6), (player: int), (destinationVector: [x, y: floats]), (timeWhenReach: long), (projectileType: int), (targetPlayer?: int), (projectileId: string)]
  tryConvertVoter = 7, // [(7), (voterId: int), (player: int), (time: long)]
  voterConverted = 8, // [(8), (voterId: int), (player: int)]
  tryClaimVoter = 9, // [(9), (voterId: int)]
  voterClaimed = 10, // [(10), (voterId: int), (player: int)]
  hello = 11, // [(11)]
  tryAddVotes = 12, // [(12), (playerNumber: int), (votes: int)]
  votesAdded = 13, // [(13), (playerNumber: int), (votes: int)]
  log = 14, // [(14), (condition: string), (stackTrace: string), (type: string)]
  newAlly = 15, // [(15), (playerNumber: int), (projectileType: int)]
  destroyProjectile = 16, // [(16), (projectileId: string)]
  introduce = 17, // [(17), (playerNumber: int), (playerName: string), (fbId?: string)]
  matchOver = 18, // [(18)]
  newScores = 19, // [(19), ([votesPlayer1: number, scorePlayer1: number, diffScorePlayer1: number]), ...]
  joinAllQueue = 20, // [(20)]
}

const MAP_WIDTH = 190
const MATCH_TIME = Number.parseFloat(process.env.MATCH_TIME || '4') * 60 * 1000

const server = net.createServer()

type Player = {
  inactive?: boolean
  in: Readable
  out: Writable
  fbId?: string
  playWithFriends?: boolean
}
type PlayerWithSocket = Player & { socket: net.Socket }
let waitingQueue: PlayerWithSocket[] = []
let matchesRunning = 0

setInterval(tryStartMatch, Number.parseFloat(process.env.LOBBY_WAIT_TIME || '60') * 1000)

server.on('connection', async (socket) => {
  try {
    await safeWaitForHello(socket)
  } catch (err) {
    console.info(err)
    socket.destroy()
    return
  }

  const duplex = {
    socket,
    in: new PassThrough({ objectMode: true }),
    out: new PassThrough({ objectMode: true }),
  }

  pipeline(
    socket,
    new TelepathyInputTransformer(),
    new Transform({
      objectMode: true,
      transform(obj, encoding, cb) {
        let msg: any[]
        try {
          msg = JSON.parse(obj)
        } catch (err) {
          console.info(err + '\nmsg=' + obj.toString())
          return cb()
        }
        const code = msg[0]
        if (code === Codes.guessTime) {
          onGuessTime(duplex, msg)
          return cb()
        } else if (code === Codes.joinAllQueue) {
          waitingQueue.push(duplex)
          return cb()
        }

        cb(null, msg)
      },
    }),
    duplex.in,
    socketClosed.bind(null, duplex),
  )

  pipeline(
    duplex.out,
    new Transform({
      objectMode: true,
      transform(obj, encoding, cb) {
        if (!process.env.LATENCY) cb(null, obj)
        else setTimeout(() => cb(null, obj), Number.parseInt(process.env.LATENCY))
      },
    }),
    new Transform({
      writableObjectMode: true,
      readableObjectMode: false,
      transform(obj, encoding, cb) {
        const msg = toTelepathyMsg(JSON.stringify(obj))
        cb(null, msg)
      },
    }),
    socket,
    socketClosed.bind(null, duplex),
  )

  sendTo(duplex, [Codes.hello])
})

function safeWaitForHello(socket: net.Socket) {
  const BYTES_TO_READ = 8 // Codes.hello: 0004[11]
  return new Promise((resolve, reject) => {
    socket.on('readable', () => {
      if (socket.readableLength < BYTES_TO_READ) return

      const data = socket.read(BYTES_TO_READ).toString()
      let code: number
      try {
        ;[code] = JSON.parse(data.slice(4))
      } catch (err) {
        return reject(err + `\ndata=${data}`)
      }
      if (code !== Codes.hello) return reject('weird data: ' + data)

      socket.removeAllListeners()
      resolve()
    })

    socket.on('close', endSocket)
    socket.on('error', endSocket)
    function endSocket(err: any) {
      socket.removeAllListeners()
      reject(err)
    }
  })
}

function tryStartMatch() {
  clearBadSockets()

  while (waitingQueue.length >= 2) {
    // todo: sort by rank
    // todo: watch for friends only
    const matchPlayers = waitingQueue.slice(0, 4)
    waitingQueue = waitingQueue.slice(4)
    new Match(matchPlayers).Start()
  }
}

function onGuessTime(player: Player, msg: any[]) {
  const playerGuess = msg[1] as number
  const offset = Date.now() - playerGuess
  sendTo(player, [Codes.guessTime, offset])
}

function socketClosed(player: PlayerWithSocket, err: any) {
  if (player.inactive) return

  if (err) {
    console.info({ err })
    if (!player.socket.destroyed) player.socket.destroy()
  }

  clearBadSockets()
}

function clearBadSockets() {
  waitingQueue = waitingQueue.filter((it) => {
    const isGood = !it.socket.destroyed && it.socket.readable && it.socket.writable
    if (isGood) return true

    it.in.destroy()
    it.out.destroy()
    return false
  })
}

const PORT = process.env.PORT ? Number.parseInt(process.env.PORT) : 7777

server.on('listening', () => {
  console.info(`listening on port ${PORT}`)
})

server.on('error', (err) => {
  console.info('server error:', err)
})

server.listen(PORT)

class Match {
  private players: Player[]
  private votersCentral: VotersCentral
  private matchTimer?: NodeJS.Timeout
  private matchEnded = false

  constructor(players: Player[]) {
    this.players = players = lodash.shuffle(players)
    this.votersCentral = new VotersCentral(players)
  }

  Start() {
    const codesMap = {
      [Codes.newPlayerDestination]: this.resendToOthers,
      [Codes.projectileFired]: (player, msg) => {
        this.resendToOthers(player, msg)
        this.votersCentral.ProjectileFired(player, msg)
      },
      [Codes.tryConvertVoter]: this.votersCentral.TryConvertVoter,
      [Codes.tryClaimVoter]: this.votersCentral.TryClaimVoter,
      [Codes.tryAddVotes]: this.votersCentral.TryAddVotes,
      [Codes.log]: this.logReceived,
      [Codes.newAlly]: this.resendToOthers,
      [Codes.destroyProjectile]: this.resendToOthers,
      [Codes.introduce]: (player, msg) => {
        this.onIntroduce(player, msg)
        this.resendToOthers(player, msg)
      },
    } as { [code in Codes]: (player: number, msg: any[]) => void }

    this.players.forEach((player, index) => {
      const stopPlayer = this.StopPlayer.bind(this, index)
      player.in.on('end', stopPlayer)
      player.in.on('close', stopPlayer)
      player.in.on('error', stopPlayer)

      player.in.on('data', (msg) => {
        const code = msg[0] as Codes
        if (!(code in codesMap)) {
          console.info('unmapped code', code)
          return
        }

        codesMap[code](index, msg)
      })
    })

    this.players.forEach((player, index) => {
      sendTo(player, [Codes.start, index, this.players.length])
    })

    this.votersCentral.Start()
    this.matchTimer = setTimeout(this.TryEndMatch, MATCH_TIME)

    ++matchesRunning
    console.info({ matchesRunning })
  }

  private readonly onIntroduce = (player: number, msg: any[]) => {
    const [code, playerNumber, playerName, fbId] = msg
    if (fbId) this.players[player].fbId = fbId
  }

  private readonly resendToOthers = (player: number, msg: any[]) => {
    this.players
      .filter((_, index) => index !== player)
      .forEach((other) => {
        sendTo(other, msg)
      })
  }

  private readonly logReceived = (player: number, msg: any[]) => {
    console.info({ player, msg })
  }

  private readonly StopPlayer = (playerNumber: number, err: any) => {
    const player = this.players[playerNumber]
    if (err) {
      console.info(err)
      player.in.destroy()
      player.out.destroy()
    }

    player.inactive = true
    if (this.matchEnded) return

    // todo: send to others that player disconnected perhaps?
    this.votersCentral.PunishPlayer(playerNumber)
    const playersPlaying = this.playersPlayingNumber()
    if (playersPlaying < 2) this.TryEndMatch()
  }

  private playersPlayingNumber() {
    return this.players.reduce((accum, next) => accum + (next.inactive ? 0 : 1), 0)
  }

  private readonly TryEndMatch = async () => {
    const playersPlaying = this.playersPlayingNumber()
    const isThereAWinner = this.votersCentral.isThereAWinner()
    if (playersPlaying >= 2 && !isThereAWinner) {
      this.matchTimer = setTimeout(() => this.TryEndMatch(), 10000)
      return
    }

    if (this.matchTimer) {
      clearTimeout(this.matchTimer)
      this.matchTimer = undefined
    }

    this.matchEnded = true
    this.votersCentral.Stop()
    this.resendToOthers(-1, [Codes.matchOver])

    const newScores = await this.CalcEloScores()
    const scoresToSave = newScores
      .filter((it) => this.players[it.playerNumber].fbId)
      .map((it) => ({ fb_id: this.players[it.playerNumber].fbId!, score: it.newScore }))
    await this.savePlayersScores(scoresToSave)

    const matchResult = newScores
      .slice()
      .sort((a, b) => a.playerNumber - b.playerNumber)
      .map((it) => [it.votes, it.newScore, it.scoreDiff])

    const msg = [Codes.newScores, ...matchResult]
    this.players.forEach((it) => {
      it.out.end(msg)
    })

    --matchesRunning
    console.info({ matchesRunning })
  }

  private getPlayersScores() {
    return Promise.all(
      this.players.map(async (p) => {
        if (!p.fbId) return initialScore
        const score = await ScoresRepo.getScore(p.fbId)
        if (!score) return initialScore
        return score.score
      }),
    )
  }

  private savePlayersScores(scores: ScoresRepo.IScore[]) {
    return Promise.all(
      scores.map((it) => {
        if (!it.fb_id) return
        const score = { fb_id: it.fb_id, score: it.score }
        return ScoresRepo.putScore(score)
      }),
    )
  }

  private async CalcEloScores() {
    const results = this.votersCentral.getVotesResults()
    const playerPrevScores = await this.getPlayersScores()

    const eloInputs = this.players.map((_, index) => ({
      id: index,
      prevScore: playerPrevScores[index],
      result: results[index],
    }))

    const newScores = multiElo(eloInputs)
    return newScores.map((it) => ({
      ...it,
      playerNumber: it.id,
      votes: results[it.id],
      scoreDiff: it.newScore - playerPrevScores[it.id],
    }))
  }
}

interface Voter {
  positionX: number
  lastHitTime: number
  player: number
  claimed: boolean
}

class VotersCentral {
  private myInterval?: NodeJS.Timeout
  private matchEnded = false

  private voterSeq = 0
  private readonly voters: Voter[] = []
  private readonly votesCounts: number[]
  private readonly centralBases: (number | undefined)[]

  constructor(private players: Player[]) {
    this.votesCounts = Array(players.length).fill(0)
    this.centralBases = Array(players.length).fill(undefined)
  }

  public Start() {
    this.myInterval = setInterval(this.PeriodicTasks, 1000)
  }

  public Stop() {
    this.matchEnded = true
    if (this.myInterval) clearInterval(this.myInterval)
    this.myInterval = undefined
  }

  private readonly PeriodicTasks = () => {
    if (this.matchEnded) return this.Stop()

    this.SendVotersPack()
    this.SendVotersToCentrals()
  }

  private readonly SendVotersPack = () => {
    const votesPerSecond = Math.ceil(this.players.length / 2)
    const votersToSend = lodash.times(votesPerSecond).map(GenerateVoterPositionX).map(this.GenerateVoterCloseTo)

    this.SendVotersToAll(votersToSend)
  }

  private readonly SendVotersToCentrals = () => {
    this.centralBases.forEach((positionX, player) => {
      if (positionX === undefined) return
      if (Math.random() < 0.7) return

      var voter = this.GenerateVoterCloseTo(positionX)
      this.SendVoterConverted(player, voter)
    })
  }

  private SendVotersToAll(votersToSend: (readonly [number, number])[]) {
    const msg = [Codes.newVoters, ...votersToSend]
    this.players.forEach((player) => {
      sendTo(player, msg)
    })
  }

  public readonly TryConvertVoter = (player: number, msg: any[]) => {
    const NO_PLAYER = -1
    const [code, voterId, playerToConvertTo, time] = msg
    const voter = this.voters[voterId]
    if (voter.claimed) return
    if (playerToConvertTo !== NO_PLAYER && voter.lastHitTime > time) return

    voter.player = playerToConvertTo
    voter.lastHitTime = time

    const reply = [Codes.voterConverted, voterId, playerToConvertTo]
    this.players.forEach((it) => {
      sendTo(it, reply)
    })
  }

  public readonly TryClaimVoter = (player: number, msg: any[]) => {
    const [code, voterId] = msg
    const voter = this.voters[voterId]
    if (voter.claimed) return
    if (voter.player !== player) return

    voter.player = player
    voter.claimed = true

    ++this.votesCounts[player]

    const reply = [Codes.voterClaimed, voterId, player]
    this.players.forEach((it) => {
      sendTo(it, reply)
    })
  }

  public readonly TryAddVotes = (player: number, msg: any[]) => {
    const [code, playerNumberToAddVotes, votes] = msg

    this.votesCounts[playerNumberToAddVotes] += votes

    const reply = [Codes.votesAdded, playerNumberToAddVotes, votes]
    this.players.forEach((it) => {
      sendTo(it, reply)
    })
  }

  PunishPlayer(playerNumber: number) {
    this.votesCounts[playerNumber] = 0
  }

  public readonly ProjectileFired = (player: number, msg: any[]) => {
    const CENTRAL_BASE_PROJECTILE_TYPES = [4, 5, 6]
    const ABSTENTION_PROJECTILE_TYPE = 13

    const [code, playerOwner, destination, timeWhenReach, projectileType, targetPlayer, projectileId] = msg
    const [projectilePositionX, projectilePositionY] = destination

    if (CENTRAL_BASE_PROJECTILE_TYPES.includes(projectileType)) {
      this.centralBases[playerOwner] = projectilePositionX

      lodash
        .times(3)
        .fill(projectilePositionX)
        .map(this.GenerateVoterCloseTo)
        .forEach(this.SendVoterConverted.bind(null, playerOwner))

      return
    }

    if (projectileType === ABSTENTION_PROJECTILE_TYPE) {
      const generateVoters = () => {
        if (this.matchEnded) return
        this.SendVotersToAll(lodash.times(5).fill(projectilePositionX).map(this.GenerateVoterCloseTo))
      }

      generateVoters()
      generateVoters()
      setTimeout(generateVoters, 1000)
      setTimeout(generateVoters, 2000)
    }
  }

  private readonly SendVoterConverted = (playerOwner: number, voter: readonly [number, number]) => {
    this.SendVotersToAll([voter])
    this.TryConvertVoter(playerOwner, [Codes.tryConvertVoter, voter[0], playerOwner, Date.now()])
  }

  private readonly GenerateVoterCloseTo = (x: number) => {
    let positionX = x + (Math.random() * 20.0 - 10.0)
    positionX = Math.min(positionX, MAP_WIDTH / 2)
    positionX = Math.max(positionX, MAP_WIDTH / -2)

    const voter = { positionX, lastHitTime: 0, player: -1, claimed: false }
    this.voters.push(voter)
    return [this.voterSeq++, positionX] as const
  }

  public isThereAWinner() {
    const sortedVotes = this.votesCounts.slice().sort((a, b) => b - a)
    return sortedVotes[0] > sortedVotes[1]
  }

  public getVotesResults() {
    return Object.fromEntries(this.votesCounts.map((votes, index) => [index, votes] as const))
  }
}

function GenerateVoterPositionX() {
  const forVoterRegion = Math.random()
  const forVoterPosition = Math.random()
  let voterPosition: number
  if (forVoterRegion < 0.26) {
    voterPosition = forVoterPosition * 0.18 + 0
  } else if (forVoterRegion < 0.38) {
    voterPosition = forVoterPosition * 0.13 + 0.18
  } else if (forVoterRegion < 0.78) {
    voterPosition = forVoterPosition * 0.3 + 0.31
  } else if (forVoterRegion < 0.9) {
    voterPosition = forVoterPosition * 0.24 + 0.61
  } else {
    voterPosition = forVoterPosition * 0.15 + 0.85
  }
  return (voterPosition - 0.5) * MAP_WIDTH
}

function sendTo(duplex: Player, msg: any[]) {
  if (duplex.inactive) return
  if (duplex.out.destroyed || !duplex.out.writable) {
    console.info('unable to send msg', msg)
    return
  }
  duplex.out.write(msg)
}

class TelepathyInputTransformer extends Transform {
  constructor() {
    super({ writableObjectMode: false, readableObjectMode: true })
  }

  private msgSize: number | undefined = undefined
  private buffer = Buffer.from('')

  _transform(chunk: Buffer, encoding: string, cb: TransformCallback) {
    this.buffer = Buffer.concat([this.buffer, chunk], this.buffer.length + chunk.length)

    while (true) {
      if (!this.msgSize) {
        if (this.buffer.length < 4) break
        const msg = this.buffer.slice(0, 4)
        this.buffer = this.buffer.slice(4)
        this.msgSize = msg[0] * 256 ** 3 + msg[1] * 256 ** 2 + msg[2] * 256 ** 1 + msg[3] * 256 ** 0
      }

      if (this.buffer.length < this.msgSize) break
      const msg = this.buffer.slice(0, this.msgSize)
      this.buffer = this.buffer.slice(this.msgSize)
      this.push(msg)
      this.msgSize = undefined
    }

    cb()
  }
}

function toTelepathyMsg(data: string) {
  const dataLength = data.length
  const encodedData = Buffer.from(`0000${data}`, 'ascii')
  encodedData[0] = (dataLength & 0xff000000) >> 24
  encodedData[1] = (dataLength & 0xff0000) >> 16
  encodedData[2] = (dataLength & 0xff00) >> 8
  encodedData[3] = (dataLength & 0xff) >> 0

  return encodedData
}
