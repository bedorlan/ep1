import 'isomorphic-fetch'

const fbUrl = 'https://graph.facebook.com/v6.0/'
const accessToken = ''

export async function getNamesFor(ids: string[]): Promise<{ [id: string]: { short_name: string } }> {
  const idsParam = ids.join(',')
  const url = `${fbUrl}/?ids=${idsParam}&fields=short_name&${getAcessTokenParam()}`
  const response = await fetch(url)
  return await response.json()
}

export interface IFriend {
  short_name: string
  id: string
}

export async function getFriendsOf(fbId: string) {
  const url = `${fbUrl}/${fbId}/friends?fields=short_name&${getAcessTokenParam()}`
  const response = await fetch(url)
  // todo: check paging
  const { data, paging }: { data: IFriend[]; paging: unknown } = await response.json()
  return data
}

export async function debugToken(token: string) {
  const url = `${fbUrl}/debug_token?input_token=${token}&${getAcessTokenParam()}`
  const response = await fetch(url)
  const data: { data: { is_valid: boolean; user_id: string } } = await response.json()
  return data.data
}

function getAcessTokenParam() {
  return `access_token=${accessToken}`
}
