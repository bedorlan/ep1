import 'isomorphic-fetch'

const fbUrl = 'https://graph.facebook.com/v6.0/'
const accessToken = '591967211411620%7C5SIbfmYv4ry7RF6E905sSnVunXc'

export async function getNamesFor(ids: string[]): Promise<{ [id: string]: { short_name: string } }> {
  const idsParam = ids.join(',')
  const url = `${fbUrl}/?ids=${idsParam}&fields=short_name&${getAcessTokenParam()}`
  const response = await fetch(url)
  return await response.json()
}

export async function getFriendsOf(fbId: string) {
  const url = `${fbUrl}/${fbId}/friends?fields=short_name&${getAcessTokenParam()}`
  const response = await fetch(url)
  // todo: check paging
  type IFriend = { short_name: string; id: string }
  const { data, paging }: { data: IFriend[]; paging: unknown } = await response.json()
  return data
}

function getAcessTokenParam() {
  return `access_token=${accessToken}`
}
