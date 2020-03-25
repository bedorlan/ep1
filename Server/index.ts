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
  projectileFired = 6, // [(6), (player: int), (destinationVector: [x, y: floats]), (timeWhenReach: long), (projectileType: int)]
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
  const duplex = { socket, in: new PassThrough({ objectMode: true }), out: new PassThrough({ objectMode: true }) }

  createInterface(socket).on('line', (raw) => {
    let msg: any[]
    try {
      const char = raw.charAt(4)
      if (char !== '[') {
        throw 'invalid raw: ' + raw
      }

      msg = getTelepathyMsg(raw)
    } catch (err) {
      console.error(err)
      socket.destroy()
      return
    }

    const code = msg[0]
    if (code === Codes.guessTime) {
      onGuessTime(duplex, msg)
      return
    }

    duplex.in.write(msg)
  })

  const sendMsg = (obj: any) => {
    const msg = toTelepathyMsg(JSON.stringify(obj) + '\n')
    socket.write(msg)
  }

  duplex.out.on('data', (obj) => {
    if (!process.env.LATENCY) {
      sendMsg(obj)
    } else {
      const latency = Number.parseInt(process.env.LATENCY)
      setTimeout(sendMsg.bind(null, obj), latency)
    }
  })

  waitingQueue.push(duplex)
  waitingQueue = waitingQueue.filter((it) => !it.socket.destroyed && it.socket.readable && it.socket.writable)
  if (waitingQueue.length < 2) return

  waitingQueue.forEach(({ socket }) => {
    socket.once('close', destroyAll.bind(null, waitingQueue))
    socket.once('error', destroyAll.bind(null, waitingQueue))
  })

  new Match(waitingQueue).Start()
  waitingQueue = []
})

function destroyAll(duplexes: (Duplex & { socket: net.Socket })[]) {
  duplexes.forEach((duplex) => {
    if (duplex.socket.destroyed) return
    duplex.socket.destroy()
    duplex.in.destroy()
    duplex.out.destroy()
  })
}

function onGuessTime(player: Duplex, msg: any[]) {
  const playerGuess = msg[1] as number
  const offset = Date.now() - playerGuess
  sendTo(player, [Codes.guessTime, offset])
}

const PORT = process.env.PORT || 7777

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
    const votesPerSecond = Math.ceil(this.players.length / 2)
    const votersToSend: [number, number][] = []
    for (let i = 0; i < votesPerSecond; ++i) {
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
