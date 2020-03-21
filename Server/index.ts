import * as net from 'net'
import { createInterface } from 'readline'

const PORT = 7777

enum Codes {
  noop = 0, // [0]
  start = 1, // [1, playerNumber: int]
  newPlayerDestination = 2, // [2, positionX: float, timeToReach: int]
  newVoters = 3, // [3, ...voters: [id: int, positionX: float]]
  measureLatency = 4, // [4]
}

const server = net.createServer()
let waitingQueue: net.Socket[] = []

server.on('connection', (socket) => {
  waitingQueue.push(socket)
  socket.on('close', dropSocket(socket))
  socket.on('error', dropSocket(socket))
  if (waitingQueue.length < 2) return

  new Match(waitingQueue).Start()
  waitingQueue = []
})

function dropSocket(socket: net.Socket) {
  return (err: any) => {
    if (err) console.error(err)
    waitingQueue = waitingQueue.filter((it) => it !== socket)
  }
}

server.on('listening', () => {
  console.log(`listening on port ${PORT}`)
})

server.listen(PORT)

const MAP_WIDTH = 200

class Match {
  private votersCentral: VotersCentral
  private latencyCentral: LatencyCentral

  constructor(private players: net.Socket[]) {
    this.votersCentral = new VotersCentral(players)
    this.latencyCentral = new LatencyCentral(players)
  }

  Start() {
    this.players.forEach((player, index) => {
      sendTo(player, [Codes.start, index])
    })

    this.latencyCentral.Start()
    this.votersCentral.Start()

    const codesMap: { [code in Codes]?: (player: number, msg: any[]) => void } = {
      [Codes.measureLatency]: this.latencyCentral.OnLatencyResponse,
      [Codes.newPlayerDestination]: this.latencyCentral.OnNewPlayerDestination,
    } as const

    this.players.forEach((player, index) => {
      // const otherPlayers = this.players.filter((other) => other !== player)

      player.on('close', this.Stop)
      player.on('error', this.Stop)

      createInterface({ input: player }).on('line', (raw) => {
        const msg = getTelepathyMsg(raw)
        const code = msg[0] as Codes
        if (!(code in codesMap)) {
          console.error('unmapped code', code)
        }

        codesMap[code]!(index, msg)
      })
    })

    console.log('new match started!')
  }

  private readonly Stop = () => {
    this.players.forEach((player) => player.end())
    this.latencyCentral.Stop()
    this.votersCentral.Stop()
  }
}

const VOTERS_PER_SECOND = 4

class VotersCentral {
  private newVotersInterval?: NodeJS.Timeout

  // the id is the index, the position the value
  private voters: number[] = []
  private voterSeq = 0

  constructor(private players: net.Socket[]) {}

  public Start() {
    this.newVotersInterval = setInterval(this.SendVotersPack, 1000)
  }

  public Stop() {
    if (this.newVotersInterval) clearInterval(this.newVotersInterval)
    this.newVotersInterval = undefined
  }

  private readonly SendVotersPack = () => {
    const votersToSend: [number, number][] = []
    for (let i = 0; i < VOTERS_PER_SECOND; ++i) {
      const positionX = GenerateVoterPositionX()
      this.voters.push(positionX)
      votersToSend.push([this.voterSeq, positionX])
      ++this.voterSeq
    }

    const msg = [Codes.newVoters, ...votersToSend]
    this.players.forEach((player) => {
      sendTo(player, msg)
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
  return (voterPosition - 0.5) * MAP_WIDTH
}

class LatencyCentral {
  private playersLatency: number[]
  private timers: Array<number | undefined>
  private latencyMeasureInterval?: NodeJS.Timeout

  constructor(private players: net.Socket[]) {
    this.playersLatency = Array(players.length).fill(0)
    this.timers = Array(players.length).fill(undefined)
  }

  Start() {
    this.latencyMeasureInterval = setInterval(this.measureLatency, 5000)
    // this.measureLatency()
  }

  Stop() {
    if (this.latencyMeasureInterval) clearInterval(this.latencyMeasureInterval)
    this.latencyMeasureInterval = undefined
  }

  private readonly measureLatency = () => {
    this.players.forEach((player) => {
      sendTo(player, [Codes.measureLatency])
    })
    this.timers.fill(Date.now())
  }

  readonly OnLatencyResponse = (player: number, msg: any[]) => {
    const startTime = this.timers[player]
    if (!startTime) return

    this.timers[player] = undefined
    const newLatency = Math.round((Date.now() - startTime) / 2)
    const oldLatency = this.playersLatency[player] || newLatency
    const theLatency = (this.playersLatency[player] = Math.round(oldLatency * 0.66 + newLatency * 0.34))
    console.log('player latency', player, theLatency)
  }

  readonly OnNewPlayerDestination = (player: number, msg: any[]) => {
    const [code, positionX, timeToReach] = msg
    this.players.forEach((other, otherIndex) => {
      if (otherIndex === player) return
      const latency = this.playersLatency[player] + this.playersLatency[otherIndex]
      const newMsg = [code, positionX, timeToReach - latency]
      sendTo(other, newMsg)
    })
  }
}

function sendTo(socket: net.Socket, msg: any[]) {
  socket.write(toTelepathyMsg(JSON.stringify(msg) + '\n'))
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
