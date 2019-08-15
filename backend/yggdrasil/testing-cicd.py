#!/usr/bin/python3

import logging
from os import getenv
import json
import requests

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


# Getting environent variables. If None, then a default one is assigned

if getenv('WEBHOOK_URL'):
    WEBHOOK_URL = getenv('WEBHOOK_URL')
else:
    WEBHOOK_URL = 'https://hooks.slack.com/services/XXXXX/XXXXX/XXXXX'

if getenv('SLACK_CHANNEL_NAME'):
    SLACK_CHANNEL_NAME = getenv('SLACK_CHANNEL_NAME')
else:
    SLACK_CHANNEL_NAME = 'networkmodel2dot0'

if getenv('CI_COMMIT_SHA'):
    CI_COMMIT_SHA = getenv('CI_COMMIT_SHA')
else:
    CI_COMMIT_SHA = 'Couldn\'t get commit sha!'


def main():
    logging.info('New commit')
    if WEBHOOK_URL != '' and SLACK_CHANNEL_NAME != '':
        text = ":bell: New Commit => %s" % CI_COMMIT_SHA
        payload = {"text": text,
                   "channel": SLACK_CHANNEL_NAME,
                   "username": "CI/CD Master of the universe"}
        requests.post(WEBHOOK_URL, json=json.loads(json.dumps(payload)))
    return



if __name__ == '__main__':
    main()
