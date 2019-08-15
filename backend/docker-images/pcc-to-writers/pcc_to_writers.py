#!/usr/bin/python3

import logging
import json
from netaddr import *
from os import getenv
from rmq_consumer import *
from rmq_publisher import *
from threads_monitoring import *
from prometheus_client import start_http_server
import os

HOSTNAME = os.uname()[1]

LOG_FORMAT = ('%(levelname) -10s %(asctime)s %(name) -30s %(funcName) '
              '-35s %(lineno) -5d: %(message)s')
logging.basicConfig(level=logging.INFO, format=LOG_FORMAT)
logger = logging.getLogger(__name__)

# Getting environent variables. If None, then a default one is assigned

if getenv('RMQ_COLLECTOR_HOST'):
    RMQ_COLLECTOR_HOST = getenv('RMQ_COLLECTOR_HOST')
else:
    RMQ_COLLECTOR_HOST = '127.0.0.1'

if getenv('RMQ_COLLECTOR_USERNAME'):
    RMQ_COLLECTOR_USERNAME = getenv('RMQ_COLLECTOR_USERNAME')
else:
    RMQ_COLLECTOR_USERNAME = 'guest'

if getenv('RMQ_COLLECTOR_PASSWORD'):
    RMQ_COLLECTOR_PASSWORD = getenv('RMQ_COLLECTOR_PASSWORD')
else:
    RMQ_COLLECTOR_PASSWORD = 'guest'

if getenv('RMQ_COLLECTOR_EXCHANGE'):
    RMQ_COLLECTOR_EXCHANGE = getenv('RMQ_COLLECTOR_EXCHANGE')
else:
    RMQ_COLLECTOR_EXCHANGE = 'shift_topology_exchange'

if getenv('RMQ_COLLECTOR_QUEUE'):
    RMQ_COLLECTOR_QUEUE = getenv('RMQ_COLLECTOR_QUEUE')
else:
    RMQ_COLLECTOR_QUEUE = 'shift_pcc_queue'

if getenv('RMQ_COLLECTOR_ROUTING_KEY'):
    RMQ_COLLECTOR_ROUTING_KEY = getenv('RMQ_COLLECTOR_ROUTING_KEY')
else:
    RMQ_COLLECTOR_ROUTING_KEY = 'shift_pcc_routing_key'

if getenv('RMQ_PUBLISHER_HOST'):
    RMQ_PUBLISHER_HOST = getenv('RMQ_PUBLISHER_HOST')
else:
    RMQ_PUBLISHER_HOST = '127.0.0.1'

if getenv('RMQ_PUBLISHER_USERNAME'):
    RMQ_PUBLISHER_USERNAME = getenv('RMQ_PUBLISHER_USERNAME')
else:
    RMQ_PUBLISHER_USERNAME = 'guest'

if getenv('RMQ_PUBLISHER_PASSWORD'):
    RMQ_PUBLISHER_PASSWORD = getenv('RMQ_PUBLISHER_PASSWORD')
else:
    RMQ_PUBLISHER_PASSWORD = 'guest'

if getenv('RMQ_PUBLISHER_EXCHANGE'):
    RMQ_PUBLISHER_EXCHANGE = getenv('RMQ_PUBLISHER_EXCHANGE')
else:
    RMQ_PUBLISHER_EXCHANGE = 'shift_topology_exchange'

if getenv('RMQ_PUBLISHER_SERVER_QUEUE'):
    RMQ_PUBLISHER_SERVER_QUEUE = getenv('RMQ_PUBLISHER_SERVER_QUEUE')
else:
    RMQ_PUBLISHER_SERVER_QUEUE = 'shift_topology_queue_writer_server'

if getenv('RMQ_PUBLISHER_SERVER_ROUTING_KEY'):
    RMQ_PUBLISHER_SERVER_ROUTING_KEY = getenv('RMQ_PUBLISHER_SERVER_ROUTING_KEY')
else:
    RMQ_PUBLISHER_SERVER_ROUTING_KEY = 'shift_topology_key_writer_server'

if getenv('RMQ_PUBLISHER_CLIENT_QUEUE'):
    RMQ_PUBLISHER_CLIENT_QUEUE = getenv('RMQ_PUBLISHER_CLIENT_QUEUE')
else:
    RMQ_PUBLISHER_CLIENT_QUEUE = 'shift_topology_queue_writer_client'

if getenv('RMQ_PUBLISHER_CLIENT_ROUTING_KEY'):
    RMQ_PUBLISHER_CLIENT_ROUTING_KEY = getenv('RMQ_PUBLISHER_CLIENT_ROUTING_KEY')
else:
    RMQ_PUBLISHER_CLIENT_ROUTING_KEY = 'shift_topology_key_writer_client'


def handler(update_message):
    query = '''
        MATCH (n:Node {IPv4_Router_Identifier: '%s', topology_id:'mpls'})
          SET n.PCC = '%s' 
    ''' % (update_message['IPv4_Router_Identifier'], update_message['local_ip'])

    if rmq_publisher_client.is_ready:
        rmq_publisher_client.publish(query)
    if rmq_publisher_server.is_ready:
        rmq_publisher_server.publish(query)


def on_message_callback(ch, method, properties, body):
    '''
    Function called for each message consumed. It defines the message as value of the object neo (Neo4j connection)
    variable, update_message.
    :param ch:
    :param method:
    :param properties:
    :param body:
    :return:

    '''
    logger.info(body.decode('utf-8'))
    pcc_data = json.loads(body.decode('utf-8'))
    handler(pcc_data)
    ch.basic_ack(delivery_tag=method.delivery_tag)


if __name__ == '__main__':

    threads = []

    rmq_collector = RMQConsumer(on_message_callback)
    rmq_collector.host = RMQ_COLLECTOR_HOST
    rmq_collector.user = RMQ_COLLECTOR_USERNAME
    rmq_collector.password = RMQ_COLLECTOR_PASSWORD
    rmq_collector.exchange = RMQ_COLLECTOR_EXCHANGE
    rmq_collector.queue = RMQ_COLLECTOR_QUEUE
    rmq_collector.routing_key = RMQ_COLLECTOR_ROUTING_KEY
    rmq_collector.start()
    rmq_collector.setName(HOSTNAME+'_PCCDataConsumer')
    threads.append(rmq_collector)

    rmq_publisher_server = RMQPublisher()
    rmq_publisher_server.host = RMQ_PUBLISHER_HOST
    rmq_publisher_server.user = RMQ_PUBLISHER_USERNAME
    rmq_publisher_server.password = RMQ_PUBLISHER_PASSWORD
    rmq_publisher_server.exchange = RMQ_PUBLISHER_EXCHANGE
    rmq_publisher_server.queue = RMQ_PUBLISHER_SERVER_QUEUE
    rmq_publisher_server.routing_key = RMQ_PUBLISHER_SERVER_ROUTING_KEY
    rmq_publisher_server.start()
    rmq_publisher_server.setName(HOSTNAME+'_PCCQueryPublisherServerQueue')
    threads.append(rmq_publisher_server)

    rmq_publisher_client = RMQPublisher()
    rmq_publisher_client.host = RMQ_PUBLISHER_HOST
    rmq_publisher_client.user = RMQ_PUBLISHER_USERNAME
    rmq_publisher_client.password = RMQ_PUBLISHER_PASSWORD
    rmq_publisher_client.exchange = RMQ_PUBLISHER_EXCHANGE
    rmq_publisher_client.queue = RMQ_PUBLISHER_CLIENT_QUEUE
    rmq_publisher_client.routing_key = RMQ_PUBLISHER_CLIENT_ROUTING_KEY
    rmq_publisher_client.start()
    rmq_publisher_client.setName(HOSTNAME+'_PCCQueryPublisherClientQueue')
    threads.append(rmq_publisher_client)

    threads_monitor = ThreadsMonitor(threads)
    threads_monitor.start()

    # Starts exposing prometheus metrics
    start_http_server(60000)