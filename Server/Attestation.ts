import 'isomorphic-fetch'
import { decode } from 'jsonwebtoken'
import * as uuid from 'uuid'

const API_KEY = ''

export function getNonce() {
  return uuid.v4().replace(/-/g, '')
}

type JsonJwt = {
  atn: string | null
  atn_error: string | null
  atn_error_msg: string | null
  no_attest_support?: boolean
}

type SafetyNetPayload = {
  timestampMs: number
  nonce: string
  apkPackageName: string
  apkCertificateDigestSha256: string
  ctsProfileMatch: boolean
  basicIntegrity: boolean
}

export async function verifyJwt(nonce: string, jsonJwtRaw: string) {
  try {
    if (Boolean(process.env.NO_ATTEST)) return true

    const jsonJwt: JsonJwt = JSON.parse(jsonJwtRaw)
    const { atn, atn_error, atn_error_msg } = jsonJwt
    if (atn_error || atn_error_msg || !atn) {
      console.info('error extracting atn', jsonJwt)
      return false
    }

    const validSignature = await validateSignature(atn)
    if (!validSignature) {
      console.info('signature not valid', atn)
      return false
    }

    const result = decode(atn, { json: true }) as SafetyNetPayload | null
    if (!result || result.nonce !== nonce || result.apkPackageName !== 'com.bedorlan.ep1' || !result.basicIntegrity) {
      console.info('attesting not valid', { nonce }, result || jsonJwt)
      return false
    }

    return true
  } catch (err) {
    console.info(err, jsonJwtRaw)
    return false
  }
}

async function validateSignature(signedAttestation: string) {
  const url = `https://www.googleapis.com/androidcheck/v1/attestations/verify?key=${API_KEY}`
  const method = 'POST'
  const headers = { 'content-type': 'application/json' }
  const body = JSON.stringify({ signedAttestation })
  const response = await fetch(url, { method, headers, body })
  const json = await response.json()
  return Boolean(json.isValidSignature)
}
