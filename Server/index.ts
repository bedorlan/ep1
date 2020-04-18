import * as lodash from 'lodash'
import * as net from 'net'
import { PassThrough, Readable, Writable, pipeline, Transform, TransformCallback } from 'stream'

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
  introduce = 17, // [(17), (playerNumber: int), (playerName: string)]
}

const MAP_WIDTH = 200

const server = net.createServer()

type Duplex = { in: Readable; out: Writable }
let waitingQueue: (Duplex & { socket: net.Socket })[] = []

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
        }

        cb(null, msg)
      },
    }),
    duplex.in,
    socketClosed.bind(null, socket),
  )

  pipeline(
    duplex.out,
    new Transform({
      objectMode: true,
      transform(obj, encoding, cb) {
        if (!process.env.LATENCY) this.push(obj)
        else setTimeout(() => this.push(obj), Number.parseInt(process.env.LATENCY))
        cb()
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
    socketClosed.bind(null, socket),
  )

  sendTo(duplex, [Codes.hello])
  waitingQueue.push(duplex)

  tryStartMatch()
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

let waitingForPlayersTimeout: NodeJS.Timeout | undefined
function tryStartMatch(timeout?: boolean) {
  clearBadSockets()

  if ((!timeout && waitingQueue.length < 4) || (timeout && waitingQueue.length < 2)) {
    if (!waitingForPlayersTimeout || timeout) {
      if (waitingQueue.length === 0) {
        waitingForPlayersTimeout = undefined
        return
      }
      console.info('not enough players. waiting')
      const waitTime = process.env.LOBBY_WAIT_TIME ? Number.parseInt(process.env.LOBBY_WAIT_TIME) : 60000
      waitingForPlayersTimeout = setTimeout(tryStartMatch.bind(null, true), waitTime)
    }
    return
  }

  if (waitingForPlayersTimeout) clearTimeout(waitingForPlayersTimeout)
  waitingForPlayersTimeout = undefined

  waitingQueue.forEach((it) => {
    it.socket.on('close', matchOver.bind(null, waitingQueue))
    it.socket.on('error', matchOver.bind(null, waitingQueue))
  })

  new Match(waitingQueue).Start()
  waitingQueue = []
}

function onGuessTime(player: Duplex, msg: any[]) {
  const playerGuess = msg[1] as number
  const offset = Date.now() - playerGuess
  sendTo(player, [Codes.guessTime, offset])
}

function socketClosed(socket: net.Socket, err: any) {
  console.info('socket closed')
  if (err && err.code !== 'ERR_STREAM_PREMATURE_CLOSE') {
    console.info({ err })
  }
  if (!socket.destroyed) socket.destroy()
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

function matchOver(players: typeof waitingQueue, err: any) {
  console.info('match over', err)
  players.forEach((it) => {
    if (!it.socket.destroyed) it.socket.destroy()
    it.in.destroy()
    it.out.destroy()
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
  private players: Duplex[]
  private votersCentral: VotersCentral

  constructor(players: Duplex[]) {
    this.players = players = lodash.shuffle(players)
    this.votersCentral = new VotersCentral(players)
  }

  Start() {
    this.players.forEach((player, index) => {
      sendTo(player, [Codes.start, index, this.players.length])
    })

    this.votersCentral.Start()

    const codesMap: { [code in Codes]?: (player: number, msg: any[]) => void } = {
      [Codes.newPlayerDestination]: this.resendToOthers,
      [Codes.projectileFired]: (player: number, msg: any[]) => {
        this.resendToOthers(player, msg)
        this.votersCentral.ProjectileFired(player, msg)
      },
      [Codes.tryConvertVoter]: this.votersCentral.TryConvertVoter,
      [Codes.tryClaimVoter]: this.votersCentral.TryClaimVoter,
      [Codes.tryAddVotes]: this.votersCentral.TryAddVotes,
      [Codes.log]: this.logReceived,
      [Codes.newAlly]: this.resendToOthers,
      [Codes.destroyProjectile]: this.resendToOthers,
      [Codes.introduce]: this.resendToOthers,
    } as const

    this.players.forEach((player, index) => {
      player.in.on('end', this.Stop)
      player.in.on('close', this.Stop)
      player.in.on('error', this.Stop)

      player.in.on('data', (msg) => {
        const code = msg[0] as Codes
        if (!(code in codesMap)) {
          console.info('unmapped code', code)
          return
        }

        codesMap[code]!(index, msg)
      })
    })

    console.info('new match started!')
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

  private readonly Stop = () => {
    this.players.forEach((player) => {
      player.in.destroy()
      player.out.destroy()
    })
    this.votersCentral.Stop()
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

  private voterSeq = 0
  private readonly voters: Voter[] = []
  private readonly centralBases: (number | undefined)[]

  constructor(private players: Duplex[]) {
    this.centralBases = Array(this.players.length).fill(undefined)
  }

  public Start() {
    this.myInterval = setInterval(this.PeriodicTasks, 1000)
  }

  public Stop() {
    if (this.myInterval) clearInterval(this.myInterval)
    this.myInterval = undefined
  }

  private readonly PeriodicTasks = () => {
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

    const reply = [Codes.voterClaimed, voterId, player]
    this.players.forEach((it) => {
      sendTo(it, reply)
    })
  }

  public readonly TryAddVotes = (player: number, msg: any[]) => {
    const [code, playerNumberToAddVotes, votes] = msg

    // todo: count the votes!

    const reply = [Codes.votesAdded, playerNumberToAddVotes, votes]
    this.players.forEach((it) => {
      sendTo(it, reply)
    })
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
      const generateVoters = () =>
        this.SendVotersToAll(lodash.times(5).fill(projectilePositionX).map(this.GenerateVoterCloseTo))

      generateVoters()
      // todo: check that the match has not ended
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

function sendTo(duplex: Duplex, msg: any[]) {
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
