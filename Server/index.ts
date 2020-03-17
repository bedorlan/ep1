import * as net from 'net'

const PORT = 7777

enum Codes {
  noop = 0, // [0]
  start = 1, // [1, playerNumber: int]
  newPlayerDestination = 2, // [2, positionX: float]
  newVoters = 3, // [3, ...positionX: float]
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
const VOTERS_TOTAL = 2000
const VOTERS_PER_SECOND = 4

class Match {
  private p1: net.Socket
  private p2: net.Socket
  private createVotersInterval: NodeJS.Timeout

  constructor(players: net.Socket[]) {
    this.p1 = players[0]
    this.p2 = players[1]
  }

  Start() {
    sendTo(this.p1, [Codes.start, 0])
    sendTo(this.p2, [Codes.start, 1])

    this.p1.pipe(this.p2)
    this.p2.pipe(this.p1)

    this.createVotersInterval = setInterval(() => {
      this.CreateVoters()
    }, 1000)
  }

  private CreateVoters() {
    const voters = GenerateVotersPack()
    const msg = [Codes.newVoters, ...voters]
    sendTo(this.p1, msg)
    sendTo(this.p2, msg)
  }
}

function GenerateVotersPack() {
  return new Array(VOTERS_PER_SECOND).fill(undefined).map(GenerateVoter)
}

function GenerateVoter() {
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
