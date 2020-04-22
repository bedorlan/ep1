import * as lodash from 'lodash'

type EloInput = { id: string; prevScore: number; result: number }
type EloResult = { id: string; newScore: number }

export const initialRating = 1700

export function multiElo(scores: EloInput[]): EloResult[] {
  const resultsMap = Object.fromEntries(scores.map((it) => [it.id, it.prevScore]))

  scores = scores.sort((a, b) => a.result - b.result)
  const matches = lodash.times(scores.length - 1).map((_, i) => [
    { id: scores[i].id, result: 0 },
    { id: scores[i + 1].id, result: 1 },
  ])
  matches.forEach((tuple) => {
    const [a, b] = tuple
    const match = [
      { id: a.id, result: a.result, prevScore: resultsMap[a.id] },
      { id: b.id, result: b.result, prevScore: resultsMap[b.id] },
    ] as const
    const [r1, r2] = Elo(match)
    resultsMap[r1.id] = r1.newScore
    resultsMap[r2.id] = r2.newScore
  })

  return Object.entries(resultsMap).map(([id, newScore]) => ({
    id,
    newScore,
  }))
}

function Elo(scores: readonly [EloInput, EloInput]): [EloResult, EloResult] {
  const { prevScore: ra, result: sa, id: ida } = scores[0]
  const { prevScore: rb, result: sb, id: idb } = scores[1]

  const ea = expectedResult(ra, rb)
  const eb = expectedResult(rb, ra)

  const ka = calcK(ra)
  const newRa = Math.round(ra + ka * (sa - ea))

  const kb = calcK(rb)
  const newRb = Math.round(rb + kb * (sb - eb))

  return [
    {
      id: ida,
      newScore: newRa,
    },
    {
      id: idb,
      newScore: newRb,
    },
  ]
}

/**
 * returns the expected result for the player
 * @param ra player score
 * @param rb adversary score
 */
function expectedResult(ra: number, rb: number) {
  return 1 / (1 + 10 ** ((rb - ra) / 400.0))
}

function calcK(r: number) {
  if (r < 2100) return 32
  if (r < 2400) return 24
  return 16
}
