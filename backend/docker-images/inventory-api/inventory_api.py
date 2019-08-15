#!/usr/bin/python3

import logging
from flask import Flask
from flask_restful import Resource, Api
import requests
from requests.auth import HTTPBasicAuth as requests_basic_auth
from flask_httpauth import HTTPBasicAuth
from os import getenv
import json


logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Getting environent variables. If None, then a default one is assigned

if getenv('NEO4JDB_HOST'):
    NEO4JDB_HOST = getenv('NEO4JDB_HOST')
else:
    NEO4JDB_HOST = '127.0.0.1'

if getenv('NEO4JDB_USERNAME'):
    NEO4JDB_USERNAME = getenv('NEO4JDB_USERNAME')
else:
    NEO4JDB_USERNAME = 'neo4j'

if getenv('NEO4JDB_PASSWORD'):
    NEO4JDB_PASSWORD = getenv('NEO4JDB_PASSWORD')
else:
    NEO4JDB_PASSWORD = 'neo4j'

if getenv('API_BINDING_ADDRESS'):
    API_BINDING_ADDRESS = getenv('API_BINDING_ADDRESS')
else:
    API_BINDING_ADDRESS = '0.0.0.0'

if getenv('API_BINDING_PORT'):
    API_BINDING_PORT = getenv('API_BINDING_PORT')
else:
    API_BINDING_PORT = 5002

if getenv('API_USERNAME'):
    API_USERNAME = getenv('API_USERNAME')
else:
    API_USERNAME = 'admin'

if getenv('API_PASSWORD'):
    API_PASSWORD = getenv('API_PASSWORD')
else:
    API_PASSWORD = 'admin'


auth = HTTPBasicAuth()
API_CREDENTIALS = {API_USERNAME: API_PASSWORD}


class Nodes(Resource):

    def data(self):
        host = 'http://'+NEO4JDB_HOST+':7474/db/data/cypher'
        logger.info('Querying to: %s' % host)
        query = {"query" : "MATCH (n:Node) RETURN n"}
        headers = {'Content-type': 'application/json'}
        nodes = requests.post(host, json=json.loads(json.dumps(query)), headers=headers,
                              auth=(NEO4JDB_USERNAME, NEO4JDB_PASSWORD))
        logger.info('QUERY SENT: %s ' % query)
        logger.info('RESPONSE: \n %s' % nodes.json()['data'])
        return nodes.json()['data']

    @auth.verify_password
    def verify(username, password):
        if not (username and password):
            return False
        return API_CREDENTIALS.get(username) == password

    @auth.login_required
    def get(self):
        logger.info('GET received')
        nodes = []
        data = self.data()
        keys_to_be_deleted = ['paged_traverse', 'outgoing_relationships', 'outgoing_typed_relationships',
                              'create_relationship', 'labels', 'traverse', 'extensions', 'all_relationships',
                              'all_typed_relationships', 'property', 'self', 'incoming_relationships', 'properties',
                              'incoming_typed_relationships']
        for i in data:
            node = {}
            for j in keys_to_be_deleted:
                del (i[0][j])
            node['Values'] = i[0]
            node['Values']['NODE'] = node['Values'].pop('metadata')
            node['Values']['NODE']['Properties'] = node['Values'].pop('data')
            node['Values']['NODE']['Id'] = node['Values']['NODE'].pop('id')
            node['Values']['NODE']['Labels'] = node['Values']['NODE'].pop('labels')
            nodes.append(node)

        return nodes


if __name__ == '__main__':

    app = Flask(__name__)
    api = Api(app)
    api.add_resource(Nodes, '/nodes')
    app.run(host=API_BINDING_ADDRESS, port=API_BINDING_PORT)
