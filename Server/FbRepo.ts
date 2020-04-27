import 'isomorphic-fetch'

const fbUrl = 'https://graph.facebook.com/v6.0/'
const accessToken = '591967211411620%7C5SIbfmYv4ry7RF6E905sSnVunXc'

export async function getNamesFor(ids: string[]): Promise<{ [id: string]: { short_name: string } }> {
  const idsParam = ids.join(',')
  const url = `${fbUrl}/?ids=${idsParam}&fields=short_name&${getAcessTokenParam()}`
  const response = await fetch(url)
  return await response.json()
}

function getAcessTokenParam() {
  return `access_token=${accessToken}`
}
