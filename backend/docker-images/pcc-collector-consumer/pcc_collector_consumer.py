#!/usr/bin/python3

import logging
from lxml import etree
from io import StringIO
import time
import json
from os import getenv
import pika
from threads_monitoring import *
from prometheus_client import start_http_server
import os
from rmq_publisher import *
from rmq_consumer import *

HOSTNAME = os.uname()[1]

LOG_FORMAT = ('%(levelname) -10s %(asctime)s %(name) -30s %(funcName) '
              '-35s %(lineno) -5d: %(message)s')
logging.basicConfig(level=logging.INFO, format=LOG_FORMAT)
logger = logging.getLogger(__name__)

# Getting environent variables. If None, then a default one is assigned

if getenv('RMQ_HOST'):
    RMQ_HOST = getenv('RMQ_HOST')
else:
    RMQ_HOST = '127.0.0.1'

if getenv('RMQ_USERNAME'):
    RMQ_USERNAME = getenv('RMQ_USERNAME')
else:
    RMQ_USERNAME = 'guest'

if getenv('RMQ_PASSWORD'):
    RMQ_PASSWORD = getenv('RMQ_PASSWORD')
else:
    RMQ_PASSWORD = 'guest'

if getenv('RPC_EXCHANGE'):
    RPC_EXCHANGE = getenv('RPC_EXCHANGE')
else:
    RPC_EXCHANGE = 'NETCONF_ASYNC_BROKER'

if getenv('RPC_QUEUE'):
    RPC_QUEUE = getenv('RPC_QUEUE')
else:
    RPC_QUEUE = 'shift_pcc_rpc_replies'

if getenv('RPC_ROUTING_KEY'):
    RPC_ROUTING_KEY = getenv('RPC_ROUTING_KEY')
else:
    RPC_ROUTING_KEY = 'shift_pcc_rpc_replies'

if getenv('RMQ_EXCHANGE'):
    RMQ_EXCHANGE = getenv('RMQ_EXCHANGE')
else:
    RMQ_EXCHANGE = 'shift_topology_exchange'

if getenv('RMQ_QUEUE'):
    RMQ_QUEUE = getenv('RMQ_QUEUE')
else:
    RMQ_QUEUE = 'shift_pcc_queue'

if getenv('RMQ_ROUTING_KEY'):
    RMQ_ROUTING_KEY = getenv('RMQ_ROUTING_KEY')
else:
    RMQ_ROUTING_KEY = 'shift_pcc_routing_key'

if getenv('SHIFT_PCE_LISTENER_IP'):
    SHIFT_PCE_LISTENER_IP = getenv('SHIFT_PCE_LISTENER_IP')
else:
    SHIFT_PCE_LISTENER_IP = '127.0.0.1'


def on_message_received(unused_channel, basic_deliver, properties, body):
    '''
    Called when a message (rpc-reply) is received
    :param unused_channel:
    :param basic_deliver:
    :param properties:
    :param body: rpc-reply
    :return:

    '''
    logger.info("RPC reply received")
    rpc_reply = body.decode('utf-8')
    # Parsing rpc-replies
    f = StringIO(rpc_reply.replace('xmlns=', 'asd='))
    root = etree.parse(f)
    pces = root.xpath('result/rpc-reply/path-computation-client-statistics/pce-statistics-response')
    for pce in pces:
        data = {}
        if pce.xpath('pce-statistics-common/pce-ip')[0].text == SHIFT_PCE_LISTENER_IP:
            if pce.xpath('pce-statistics-common/pce-status')[0].text == 'PCE_STATE_UP':
                data['local_ip'] = pce.xpath('pce-statistics-common/local-ip')[0].text
                data['IPv4_Router_Identifier'] = properties.correlation_id
                if publisher.is_ready:
                    publisher.publish(json.dumps(data))
                    logger.info("Publishing %s" % data)
                break
        else:
            continue

if __name__ == '__main__':

    threads = []

    consumer = RMQConsumer(on_message_received)
    consumer.host = RMQ_HOST
    consumer.user = RMQ_USERNAME
    consumer.password = RMQ_PASSWORD
    consumer.exchange = RPC_EXCHANGE
    consumer.queue = RPC_QUEUE
    consumer.routing_key = RPC_ROUTING_KEY
    consumer.start()
    consumer.setName(HOSTNAME+'_PCCRPCConsumer')
    threads.append(consumer)

    publisher = RMQPublisher()
    publisher.host = RMQ_HOST
    publisher.user = RMQ_USERNAME
    publisher.password = RMQ_PASSWORD
    publisher.exchange = RMQ_EXCHANGE
    publisher.queue = RMQ_QUEUE
    publisher.routing_key = RMQ_ROUTING_KEY
    publisher.start()
    publisher.setName(HOSTNAME + '_PCCRPCDataPublisher')
    threads.append(publisher)

    try:
        threads_monitor = ThreadsMonitor(threads)
        threads_monitor.start()

        # Starts exposing prometheus metrics
        start_http_server(60000)

    except KeyboardInterrupt:
        try:
            consumer.close()
        except AttributeError:
            pass
