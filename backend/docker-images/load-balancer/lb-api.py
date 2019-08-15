#!/usr/bin/python3

import logging
from flask import Flask, jsonify, request
from flask_restful import Resource, Api
from flask_httpauth import HTTPBasicAuth
from os import getenv
from flask_api import status
import os
import json
import time 
import requests

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Getting environent variables. If None, then a default one is assigned

if getenv('NEO4J_HOST'):
    NEO4J_HOST = getenv('NEO4J_HOST')
else:
    NEO4J_HOST = '127.0.0.1'

if getenv('NEO4J_USERNAME'):
    NEO4J_USERNAME = getenv('NEO4J_USERNAME')
else:
    NEO4J_USERNAME = 'neo4j'

if getenv('NEO4J_PASSWORD'):
    NEO4J_PASSWORD = getenv('NEO4J_PASSWORD')
else:
    NEO4J_PASSWORD = 'neo4j'

if getenv('API_BINDING_ADDRESS'):
    API_BINDING_ADDRESS = getenv('API_BINDING_ADDRESS')
else:
    API_BINDING_ADDRESS = '0.0.0.0'

if getenv('API_BINDING_PORT'):
    API_BINDING_PORT = getenv('API_BINDING_PORT')
else:
    API_BINDING_PORT = 8007

if getenv('API_USERNAME'):
    API_USERNAME = getenv('API_USERNAME')
else:
    API_USERNAME = 'admin'

if getenv('API_PASSWORD'):
    API_PASSWORD = getenv('API_PASSWORD')
else:
    API_PASSWORD = 'admin'

if getenv('WEBHOOK_URL'):
    WEBHOOK_URL = getenv('WEBHOOK_URL')
else:
    WEBHOOK_URL = ''

if getenv('SLACK_CHANNEL_NAME'):
    SLACK_CHANNEL_NAME = getenv('SLACK_CHANNEL_NAME')
else:
    SLACK_CHANNEL_NAME = ''

auth = HTTPBasicAuth()
API_CREDENTIALS = {API_USERNAME: API_PASSWORD}


class dbLoadBalancer(Resource):

    @auth.verify_password
    def verify(username, password):
        if not (username and password):
            return False
        return API_CREDENTIALS.get(username) == password

    # @auth.login_required
    def get(self):
        logger.info('GET received')
        with open('/etc/nginx/sites-enabled/db-lb.conf', 'r') as ff:
            response = ff.read()

        return response

    # @auth.login_required
    def post(self):
        if not request.json or not 'attached_db' in request.json:  # Si no es un json o el json no tiene attached_db no continuo
            logging.error('Request does not have attached_db key')
            return status.HTTP_406_NOT_ACCEPTABLE
        else:
            master_id = request.json['attached_db']
            os.system("(cat /etc/nginx/sites-enabled/db-lb.conf | grep {0}) || (sed -i 's/[a-z]*4[a-z]*-[a-z]*/{0}/g' /etc/nginx/sites-enabled/db-lb.conf)".format(master_id))
            os.system("(cat /etc/nginx/nginx.conf | grep {0}) || (sed -i 's/[a-z]*4[a-z]*-[a-z]*/{0}/g' /etc/nginx/nginx.conf && nginx -s reload)".format(master_id))

            # Check nginx status and tries to restart it until it is successfully running
            while os.system("/etc/init.d/nginx status") != 0:
                logger.info("NGINX is down. Restarting it!!!")
                os.system("/etc/init.d/nginx restart")

            logging.info('changed database to ' + master_id)
            if WEBHOOK_URL != '' and SLACK_CHANNEL_NAME != '':
                text = ":bell: LB Changed to: *" + str(master_id) + " *"
                payload = {"text": text,
                           "channel": SLACK_CHANNEL_NAME,
                           "username": "Load Balancer API"}
                requests.post(WEBHOOK_URL, json=json.loads(json.dumps(payload)))
            return status.HTTP_202_ACCEPTED


if __name__ == '__main__':

    while os.system("/etc/init.d/nginx status") != 0:
        logger.info("NGINX is down. Restarting it!!!")
        os.system("/etc/init.d/nginx restart && nginx -s reload")
        time.sleep(5)
    app = Flask(__name__)
    api = Api(app)
    api.add_resource(dbLoadBalancer, '/')
    app.run(host=API_BINDING_ADDRESS, port=API_BINDING_PORT, debug=True)
