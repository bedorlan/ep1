import * as crypto from 'crypto'
import { RSA_PKCS1_PADDING } from 'constants'

const privateKey = ''

export function decryptRsa(data: Buffer) {
  return crypto.privateDecrypt({ key: privateKey, padding: RSA_PKCS1_PADDING }, data)
}
