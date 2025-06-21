import * as AWS from 'aws-sdk'
import { AttributeMap } from 'aws-sdk/clients/dynamodb'

const awsConf = { region: 'us-east-1' }
const dynamoConf = {}

if (process.env.DYNAMO_ENDPOINT) {
  Object.assign(dynamoConf, { endpoint: process.env.DYNAMO_ENDPOINT })
} else {
  Object.assign(awsConf, {
    accessKeyId: '',
    secretAccessKey: '',
  })
}

AWS.config.update(awsConf)
const db = new AWS.DynamoDB(dynamoConf)

const TableName = 'scores'

export interface IScore {
  fb_id: string
  score: number
}

export function putScore(item: IScore) {
  const Item = {
    fb_id: { S: item.fb_id },
    score: { N: item.score.toString() },
    active: { N: '1' },
  }
  return new Promise((resolve, reject) => {
    db.putItem({ TableName, Item }, (err) => {
      if (err) return reject(err)
      resolve()
    })
  })
}

export function getScore(fb_id: string): Promise<IScore | null> {
  const Key = { fb_id: { S: fb_id } }
  return new Promise((resolve, reject) => {
    db.getItem({ TableName, Key }, (err, data) => {
      if (err) return reject(err)
      if (!data || !data.Item) return resolve(null)
      resolve(itemToScore(data.Item))
    })
  })
}

export function batchGetScore(fb_ids: string[]): Promise<IScore[]> {
  const Keys = fb_ids.map((it) => ({ fb_id: { S: it } }))
  return new Promise((resolve, reject) => {
    db.batchGetItem({ RequestItems: { [TableName]: { Keys } } }, (err, data) => {
      if (err) return reject(err)
      if (!data?.Responses?.scores) return resolve([])
      const scores = data.Responses.scores.map(itemToScore)
      resolve(scores)
    })
  })
}

export function getTop3() {
  return new Promise<IScore[]>((resolve, reject) => {
    db.query(
      {
        TableName,
        IndexName: 'active-score-index',
        ScanIndexForward: false,
        KeyConditions: {
          active: { ComparisonOperator: 'EQ', AttributeValueList: [{ N: '1' }] },
        },
        Limit: 3,
      },
      (err, data) => {
        if (err) return reject(err)
        resolve(data.Items?.map(itemToScore) ?? [])
      },
    )
  })
}

function itemToScore(Item: AttributeMap): IScore {
  return { fb_id: Item.fb_id.S!, score: Number.parseInt(Item.score.N!) }
}
