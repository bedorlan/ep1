import * as net from 'net'

const PORT = 7777

enum Codes {
  noop = 0,
  start = 1,
  newPlayerDestination = 2,
}

type Packet = {
  [Codes.noop]: [Codes.noop]
  [Codes.start]: [Codes.start]
  [Codes.newPlayerDestination]: [Codes.newPlayerDestination, number]
}

const server = net.createServer()

const tuple: net.Socket[] = []
server.on('connection', (socket) => {
  tuple.push(socket)
  if (tuple.length < 2) return

  const [p1, p2] = tuple
  p1.pipe(p2)
  p2.pipe(p1)

  p1.write(toTelepathyMsg(`[${Codes.start}]`))
  p2.write(toTelepathyMsg(`[${Codes.start}]`))

  tuple.length = 0
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
