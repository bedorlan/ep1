import { Transform, TransformCallback } from 'stream'

export class TelepathyInputTransformer extends Transform {
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

export function toTelepathyMsg(data: string) {
  const dataLength = data.length
  const encodedData = Buffer.from(`0000${data}`, 'ascii')
  encodedData[0] = (dataLength & 0xff000000) >> 24
  encodedData[1] = (dataLength & 0xff0000) >> 16
  encodedData[2] = (dataLength & 0xff00) >> 8
  encodedData[3] = (dataLength & 0xff) >> 0

  return encodedData
}
