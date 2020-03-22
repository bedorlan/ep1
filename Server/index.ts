import * as lodash from 'lodash'
import * as net from 'net'
import { createInterface } from 'readline'
import { PassThrough, Readable, Writable } from 'stream'

enum Codes {
  noop = 0, // [0]
  start = 1, // [1, playerNumber: int]
  newPlayerDestination = 2, // [2, positionX: float, timeWhenReach: long]
  newVoters = 3, // [3, ...voters: [id: int, positionX: float]]
  guessTime = 5, // to server: [5, guessedTime: int], from server: [5, deltaGuess: int]
  projectileFired = 6, // [6, player: int, destinationVector: [x, y: floats], timeWhenReach: long]
  tryConvertVoter = 7, // [(7), (voterId: int), (player: int), (time: long)]
  voterConverted = 8, // [(8), (voterId: int), (player: int)]
  tryClaimVoter = 9, // [(9), (voterId: int)]
  voterClaimed = 10, // [(10), (voterId: int), (player: int)]
  // if code == 50 .. be careful: ctrl + f Codes.guessTime on server
}

const server = net.createServer()

type Duplex = { in: Readable; out: Writable }
let waitingQueue: (Duplex & { socket: net.Socket })[] = []

server.on('connection', (socket) => {
  const duplex = { socket, in: new PassThrough(), out: new PassThrough() }
  // TODO: remove the latency!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
  // duplex.out.pipe(socket)
  duplex.out.on('data', (data) => {
    setTimeout(() => {
      socket.write(data)
    }, 250)
  })

  createInterface(socket).on('line', (raw) => {
    const code = Number.parseInt(raw.charAt(5))
    if (code !== Codes.guessTime) {
      duplex.in.write(raw + '\n')
      return
    }

    onGuessTime(duplex, getTelepathyMsg(raw))
  })

  waitingQueue.push(duplex)
  socket.on('close', dropStream(duplex))
  socket.on('error', dropStream(duplex))
  if (waitingQueue.length < 2) return

  socket.on('close', closeEverything.bind(null, waitingQueue))
  socket.on('error', closeEverything.bind(null, waitingQueue))
  new Match(waitingQueue).Start()
  waitingQueue = []
})

function dropStream(duplex: { socket: net.Socket }) {
  return (err: any) => {
    if (err) console.error(err)
    duplex.socket.end()
    waitingQueue = waitingQueue.filter((it) => it !== duplex)
  }
}

function closeEverything(sockets: { socket: net.Socket }[]) {
  sockets.forEach(({ socket }) => {
    socket.end()
  })
}

function onGuessTime(player: Duplex, msg: any[]) {
  const playerGuess = msg[1] as number
  const offset = Date.now() - playerGuess
  sendTo(player, [Codes.guessTime, offset])
}

const PORT = 7777

server.on('listening', () => {
  console.info(`listening on port ${PORT}`)
})

server.listen(PORT)

class Match {
  private players: Duplex[]
  private votersCentral: VotersCentral

  constructor(players: Duplex[]) {
    this.players = lodash.shuffle(players)
    this.votersCentral = new VotersCentral(players)
  }

  Start() {
    this.players.forEach((player, index) => {
      sendTo(player, [Codes.start, index])
    })

    this.votersCentral.Start()

    const codesMap: { [code in Codes]?: (player: number, msg: any[]) => void } = {
      [Codes.newPlayerDestination]: this.resendToOthers,
      [Codes.projectileFired]: this.resendToOthers,
      [Codes.tryConvertVoter]: this.votersCentral.TryConvertVoter,
      [Codes.tryClaimVoter]: this.votersCentral.TryClaimVoter,
    } as const

    this.players.forEach((player, index) => {
      player.in.on('close', this.Stop)
      player.in.on('error', this.Stop)

      createInterface(player.in).on('line', (raw) => {
        const msg = getTelepathyMsg(raw)
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
  private newVotersInterval?: NodeJS.Timeout

  // the id is the index, the position the value
  private voters: Voter[] = []
  private voterSeq = 0

  constructor(private players: Duplex[]) {}

  public Start() {
    this.newVotersInterval = setInterval(this.SendVotersPack, 1000)
  }

  public Stop() {
    if (this.newVotersInterval) clearInterval(this.newVotersInterval)
    this.newVotersInterval = undefined
  }

  private readonly SendVotersPack = () => {
    const VOTERS_PER_SECOND = 4
    const votersToSend: [number, number][] = []
    for (let i = 0; i < VOTERS_PER_SECOND; ++i) {
      const positionX = GenerateVoterPositionX()
      const voter = { positionX, lastHitTime: 0, player: -1, claimed: false }
      this.voters.push(voter)
      votersToSend.push([this.voterSeq, positionX])
      ++this.voterSeq
    }

    const msg = [Codes.newVoters, ...votersToSend]
    this.players.forEach((player) => {
      sendTo(player, msg)
    })
  }

  public readonly TryConvertVoter = (player: number, msg: any[]) => {
    const [code, voterId, _, time] = msg
    const voter = this.voters[voterId]
    if (voter.claimed) return
    if (voter.lastHitTime > time) return

    voter.player = player
    voter.lastHitTime = time

    const reply = [Codes.voterConverted, voterId, player]
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
  const MAP_WIDTH = 200
  return (voterPosition - 0.5) * MAP_WIDTH
}

function sendTo(duplex: Duplex, msg: any[]) {
  if (!duplex.out.writable) {
    console.error('unable to send msg', msg)
    return
  }
  duplex.out.write(toTelepathyMsg(JSON.stringify(msg) + '\n'))
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
