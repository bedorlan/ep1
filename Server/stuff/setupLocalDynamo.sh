#!/bin/bash

set -ex

scoresSchema="$(< scores.json)"
db="aws dynamodb --endpoint-url http://localhost:8000"

$db create-table --cli-input-json "$scoresSchema"
$db put-item --table-name scores --item '{"fb_id":{"S":"115251663499389"}, "score":{"N":"800"}, "active": {"N":"1"}}'
$db put-item --table-name scores --item '{"fb_id":{"S":"101861341518834"}, "score":{"N":"900"}, "active": {"N":"1"}}'
$db put-item --table-name scores --item '{"fb_id":{"S":"105333811168098"}, "score":{"N":"1000"}, "active": {"N":"1"}}'
$db put-item --table-name scores --item '{"fb_id":{"S":"105760594449126"}, "score":{"N":"1100"}, "active": {"N":"1"}}'
$db put-item --table-name scores --item '{"fb_id":{"S":"107271457628980"}, "score":{"N":"1200"}, "active": {"N":"1"}}'
$db put-item --table-name scores --item '{"fb_id":{"S":"113851153630011"}, "score":{"N":"1300"}, "active": {"N":"1"}}'
$db put-item --table-name scores --item '{"fb_id":{"S":"107374057627834"}, "score":{"N":"1400"}, "active": {"N":"1"}}'
$db put-item --table-name scores --item '{"fb_id":{"S":"4"}, "score":{"N":"400"}, "active": {"N":"1"}}'
$db put-item --table-name scores --item '{"fb_id":{"S":"5"}, "score":{"N":"500"}, "active": {"N":"1"}}'
