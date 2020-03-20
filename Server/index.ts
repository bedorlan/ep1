import * as net from 'net'
import * as Event from 'events'

const PORT = 7777

enum Codes {
  noop = 0, // [0]
  start = 1, // [1, playerNumber: int]
  newPlayerDestination = 2, // [2, positionX: float]
  newVoters = 3, // [3, ...voters: [id: number, positionX: number]]
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

function dropSocket(socket) {
  return (err) => {
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
  private p1: net.Socket
  private p2: net.Socket
  private votersCentral = new VotersCentral()

  constructor(players: net.Socket[]) {
    this.p1 = players[0]
    this.p2 = players[1]
  }

  Start() {
    sendTo(this.p1, [Codes.start, 0])
    sendTo(this.p2, [Codes.start, 1])

    this.p1.pipe(this.p2)
    this.p2.pipe(this.p1)

    this.votersCentral.on('newVotersPack', this.SendVotersPack.bind(this))
    this.votersCentral.Start()
  }

  private SendVotersPack(pack: [number, number][]) {
    const msg = [Codes.newVoters, ...pack]
    sendTo(this.p1, msg)
    sendTo(this.p2, msg)
  }
}

const VOTERS_PER_SECOND = 4

class VotersCentral extends Event {
  public Start() {
    this.newVotersTimer = setInterval(this.GenerateVotersPack.bind(this), 1000)
  }

  public Stop() {
    clearInterval(this.newVotersTimer)
    this.newVotersTimer = null
  }

  private newVotersTimer: NodeJS.Timeout

  private voterSeq = 0
  // the id is the index, the position the value
  private voters: number[] = []

  private GenerateVotersPack() {
    const votersToSend: [number, number][] = []
    for (let i = 0; i < VOTERS_PER_SECOND; ++i) {
      const positionX = GenerateVoterPositionX()
      this.voters.push(positionX)
      votersToSend.push([this.voterSeq, positionX])
      ++this.voterSeq
    }
    this.emit('newVotersPack', votersToSend)
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

function sendTo(socket: net.Socket, msg) {
  socket.write(toTelepathyMsg(JSON.stringify(msg) + '\n'))
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
