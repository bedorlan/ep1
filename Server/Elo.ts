type EloInput = { id: number; prevScore: number; result: number }
type EloResult = { id: number; newScore: number }

export const initialScore = 1700

export function multiElo(scores: EloInput[]): EloResult[] {
  const resultsMap = Object.fromEntries(
    scores.map((it) => [it.id, { prevScore: it.prevScore, expectedResult: 0, actualResult: 0 }]),
  )

  scores = scores.sort((a, b) => a.result - b.result).reverse()
  function* matchesCreator() {
    for (let i = 0; i < scores.length - 1; ++i) {
      for (let j = i + 1; j < scores.length; ++j) {
        yield [
          { id: scores[i].id, prevScore: scores[i].prevScore, result: scores[i].result },
          { id: scores[j].id, prevScore: scores[j].prevScore, result: scores[j].result },
        ]
      }
    }
  }
  const matches = [...matchesCreator()]
  matches.forEach((tuple) => {
    const [a, b] = tuple
    resultsMap[a.id].expectedResult += calcExpectedResult(a.prevScore, b.prevScore)
    resultsMap[b.id].expectedResult += calcExpectedResult(b.prevScore, a.prevScore)
    resultsMap[a.id].actualResult += a.result === b.result ? 0.5 : 1
    resultsMap[b.id].actualResult += a.result === b.result ? 0.5 : 0
  })

  return Object.keys(resultsMap).map((id) => {
    const result = resultsMap[id]
    const newScore = calcNewScore(result.prevScore, result.expectedResult, result.actualResult)
    return { id: Number.parseInt(id), newScore }
  })
}

/**
 * returns the expected result for the player
 * @param ra player score
 * @param rb adversary score
 */
function calcExpectedResult(ra: number, rb: number) {
  return 1 / (1 + 10 ** ((rb - ra) / 400))
}

/**
 * calcs the new score for a player
 * @param ra previous score
 * @param ea expected result
 * @param sa obtained result
 */
function calcNewScore(ra: number, ea: number, sa: number) {
  const ka = calcK(ra)
  return Math.round(ra + ka * (sa - ea))
}

function calcK(r: number) {
  if (r < 2100) return 32
  if (r < 2400) return 24
  return 16
}
