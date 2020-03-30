import * as lodash from 'lodash'
import * as net from 'net'
import { createInterface } from 'readline'
import { PassThrough, Readable, Writable } from 'stream'

enum Codes {
  noop = 0, // [0]
  start = 1, // [(1), (playerNumber: int), (totalNumberOfPlayers: int)]
  newPlayerDestination = 2, // [(2), (positionX: float), (timeWhenReach: long), (playerNumber: int)]
  newVoters = 3, // [3, ...voters: [id: int, positionX: float]]
  guessTime = 5, // to server: [5, guessedTime: int], from server: [5, deltaGuess: int]
  projectileFired = 6, // [(6), (player: int), (destinationVector: [x, y: floats]), (timeWhenReach: long), (projectileType: int), (targetPlayer?: int)]
  tryConvertVoter = 7, // [(7), (voterId: int), (player: int), (time: long)]
  voterConverted = 8, // [(8), (voterId: int), (player: int)]
  tryClaimVoter = 9, // [(9), (voterId: int)]
  voterClaimed = 10, // [(10), (voterId: int), (player: int)]
  hello = 11, // [(11)]
  tryAddVotes = 12, // [(12), (playerNumber: int), (votes: int)]
  votesAdded = 13, // [(13), (playerNumber: int), (votes: int)]
  log = 14, // [(14), (condition: string), (stackTrace: string), (type: string)]
}

const MAP_WIDTH = 200

const server = net.createServer()

type Duplex = { in: Readable; out: Writable }
let waitingQueue: (Duplex & { socket: net.Socket })[] = []

server.on('connection', async (socket) => {
  try {
    await safeWaitForHello(socket)
  } catch (err) {
    console.error(err)
    socket.destroy()
    return
  }

  const duplex = {
    socket,
    in: new PassThrough({ objectMode: true }),
    out: new PassThrough({ objectMode: true }),
  }
  sendTo(duplex, [Codes.hello])
  waitingQueue.push(duplex)

  const sendMsgFromDuplexToSocket = (obj: any) => {
    if (!socket.writable) return
    const msg = toTelepathyMsg(JSON.stringify(obj) + '\n')
    socket.write(msg)
  }

  duplex.out.on('data', (obj) => {
    if (!process.env.LATENCY) {
      sendMsgFromDuplexToSocket(obj)
    } else {
      const latency = Number.parseInt(process.env.LATENCY)
      setTimeout(sendMsgFromDuplexToSocket.bind(null, obj), latency)
    }
  })

  const readliner = createInterface({ input: socket, terminal: false })
  readliner.on('line', (raw) => {
    let msg: any[]
    try {
      msg = getTelepathyMsg(raw)
    } catch (err) {
      // is this needed?
      console.error(err)
      socket.destroy()
      readliner.removeAllListeners()
      duplex.in.destroy()
      duplex.out.destroy()
      return
    }

    const code = msg[0]
    if (code === Codes.guessTime) {
      onGuessTime(duplex, msg)
      return
    }

    duplex.in.write(msg)
  })

  socket.on('close', socketClosed.bind(null, socket))
  socket.on('error', socketClosed.bind(null, socket))

  tryStartMatch()
})

function safeWaitForHello(socket: net.Socket) {
  const BYTES_TO_READ = 9 // Codes.hello: 000?[11]\n
  return new Promise((resolve, reject) => {
    socket.on('readable', process)

    function process() {
      if (socket.readableLength < BYTES_TO_READ) return

      const data = socket.read(BYTES_TO_READ).toString()
      let code: number
      try {
        ;[code] = JSON.parse(data.slice(4))
      } catch (err) {
        return reject(err + `\ndata=${data}`)
      }
      if (code !== Codes.hello) return reject('weird data: ' + data)

      socket.off('readable', process)
      resolve()
    }
  })
}

let waitingForPlayersTimeout: NodeJS.Timeout | undefined
function tryStartMatch(timeout?: boolean) {
  if ((!timeout && waitingQueue.length < 4) || (timeout && waitingQueue.length < 2)) {
    if (!waitingForPlayersTimeout || timeout) {
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
  console.error('socket closed', err)
  if (!socket.destroyed) socket.destroy()

  waitingQueue = waitingQueue.filter((it) => {
    const isGood = !it.socket.destroyed && it.socket.readable && it.socket.writable
    if (isGood) return true

    it.in.destroy()
    it.out.destroy()
    return false
  })
}

function matchOver(players: typeof waitingQueue, err: any) {
  console.error('match over', err)
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
  console.error('server error:', err)
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
      [Codes.projectileFired]: this.votersCentral.ProjectileFired,
      [Codes.tryConvertVoter]: this.votersCentral.TryConvertVoter,
      [Codes.tryClaimVoter]: this.votersCentral.TryClaimVoter,
      [Codes.tryAddVotes]: this.votersCentral.TryAddVotes,
      [Codes.log]: this.logReceived,
    } as const

    this.players.forEach((player, index) => {
      player.in.on('end', this.Stop)
      player.in.on('close', this.Stop)
      player.in.on('error', this.Stop)

      player.in.on('data', (msg) => {
        const code = msg[0] as Codes
        if (!(code in codesMap)) {
          console.error('unmapped code', code)
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
      player.out.end()
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
    const votersToSend = lodash
      .times(votesPerSecond)
      .map(GenerateVoterPositionX)
      .map(this.GenerateVoterCloseTo)

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
    const [code, playerOwner, destination, timeWhenReach, projectileType, targetPlayer] = msg

    this.players
      .filter((_, index) => index !== playerOwner)
      .forEach((other) => {
        sendTo(other, msg)
      })

    const CENTRAL_BASE_PROJECTILE_TYPE = 3
    if (projectileType !== CENTRAL_BASE_PROJECTILE_TYPE) return

    const [centralPositionX, centralPositionY] = destination
    this.centralBases[playerOwner] = centralPositionX

    lodash
      .times(3)
      .fill(centralPositionX)
      .map(this.GenerateVoterCloseTo)
      .forEach(this.SendVoterConverted.bind(null, playerOwner))
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
  if (!duplex.out.writable) {
    console.error('unable to send msg', msg)
    return
  }
  duplex.out.write(msg)
}

function getTelepathyMsg(data: string) {
  return JSON.parse(data.slice(4))
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
