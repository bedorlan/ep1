import * as lodashFp from 'lodash/fp'
import * as net from 'net'
import { toTelepathyMsg } from '../Telepathy'

const socket = net.connect(7777)
socket.on('connect', () => {
  const write = lodashFp.pipe(JSON.stringify, toTelepathyMsg, socket.write.bind(socket))
  write([11])
  write([17, -1, 'Ethan', '107271457628980'])
  write([20])
})

socket.on('data', (data) => {
  console.log(data.toString())
})
