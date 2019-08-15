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
    RPC_QUEUE = 'shift_rpm_rpc_replies'

if getenv('RPC_ROUTING_KEY'):
    RPC_ROUTING_KEY = getenv('RPC_ROUTING_KEY')
else:
    RPC_ROUTING_KEY = 'shift_rpm_rpc_replies'

if getenv('RMQ_EXCHANGE'):
    RMQ_EXCHANGE = getenv('RMQ_EXCHANGE')
else:
    RMQ_EXCHANGE = 'shift_topology_exchange'

if getenv('RMQ_QUEUE'):
    RMQ_QUEUE = getenv('RMQ_QUEUE')
else:
    RMQ_QUEUE = 'shift_rpm_queue'

if getenv('RMQ_ROUTING_KEY'):
    RMQ_ROUTING_KEY = getenv('RMQ_ROUTING_KEY')
else:
    RMQ_ROUTING_KEY = 'shift_rpm_routing_key'


def on_message_received(channel, method, properties, body):
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
    probes = root.xpath('result/rpc-reply/probe-results/probe-test-results')
    for test in probes:
        if test.xpath('probe-test-current-results/probe-test-generic-results/loss-percentage')[0].text != '100.000000':
            try:
                results = {}
                results['originator'] = properties.correlation_id
                results['source_address'] = test.xpath('source-address')[0].text
                results['target_address'] = test.xpath('target-address')[0].text
                results['rtt'] = test.xpath('probe-single-results/rtt')[0].text

                # results['jitter'] = test.xpath('probe-single-results/round-trip-jitter')[0].text
            except Exception as ee: # On eve lab, there's no measurement for jitter. Also, sometimes there's a random issue with the syntax
                logger.info('EXCEPTION CAUGHT: %s', ee)
                logger.info(results)
                pass
                continue
            if publisher.is_ready:
                publisher.publish(json.dumps(results))
        else:
            logger.info('Frame loss seen between %s and %s', test.xpath('source-address')[0].text,
                        test.xpath('target-address')[0].text)
            continue
    channel.basic_ack(delivery_tag=method.delivery_tag)


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
    consumer.setName(HOSTNAME+'_RPMRPCConsumer')
    threads.append(consumer)

    publisher = RMQPublisher()
    publisher.host = RMQ_HOST
    publisher.user = RMQ_USERNAME
    publisher.password = RMQ_PASSWORD
    publisher.exchange = RMQ_EXCHANGE
    publisher.queue = RMQ_QUEUE
    publisher.routing_key = RMQ_ROUTING_KEY
    publisher.start()
    publisher.setName(HOSTNAME + '_RPMRPCDataPublisher')
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
