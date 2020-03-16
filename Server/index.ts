import * as net from 'net'

const PORT = 7777

enum Codes {
  noop = 0, // [0]
  start = 1, // [1, playerNumber: int]
  newPlayerDestination = 2, // [2, positionX: float]
}

const server = net.createServer()
let waitingQueue: net.Socket[] = []

server.on('connection', (socket) => {
  waitingQueue.push(socket)
  socket.on('close', dropSocket(socket))
  socket.on('error', dropSocket(socket))
  if (waitingQueue.length < 2) return

  const [p1, p2] = waitingQueue
  p1.pipe(p2)
  p2.pipe(p1)

  p1.write(toTelepathyMsg(`[${Codes.start}, 0]`))
  p2.write(toTelepathyMsg(`[${Codes.start}, 1]`))

  waitingQueue = []
})

server.on('listening', () => {
  console.log(`listening on port ${PORT}`)
})

server.listen(PORT)

function toTelepathyMsg(data: string) {
  const dataLength = data.length
  const encodedData = Buffer.from(`0000${data}`, 'ascii')
  encodedData[0] = (dataLength & 0xff000000) >> 24
  encodedData[1] = (dataLength & 0xff0000) >> 16
  encodedData[2] = (dataLength & 0xff00) >> 8
  encodedData[3] = (dataLength & 0xff) >> 0

  return encodedData
}

function dropSocket(socket) {
  return (err) => {
    if (err) console.error(err)
    waitingQueue = waitingQueue.filter((it) => it !== socket)
  }
}
