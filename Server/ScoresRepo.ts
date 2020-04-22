import * as AWS from 'aws-sdk'

AWS.config.update({
  region: 'us-east-1',
  accessKeyId: 'AKIAVQP6QK3UBHX5JVJ5',
  secretAccessKey: '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',
})

const conf = process.env.DYNAMO_ENDPOINT ? { endpoint: process.env.DYNAMO_ENDPOINT } : undefined
const db = new AWS.DynamoDB(conf)

const TableName = 'scores'

export function putScore(item: { fb_id: string; score: number }) {
  const Item = { fb_id: { S: item.fb_id }, score: { N: item.score.toString() } }
  return new Promise((resolve, reject) => {
    db.putItem({ TableName, Item }, (err) => {
      if (err) return reject(err)
      resolve()
    })
  })
}

export function getScore(fb_id: string) {
  const Key = { fb_id: { S: fb_id } }
  return new Promise((resolve, reject) => {
    db.getItem({ TableName, Key }, (err, data) => {
      if (err) return reject(err)
      const Item = { fb_id: data.Item!.fb_id.S, score: Number.parseInt(data.Item!.score.N!) }
      resolve(Item)
    })
  })
}
