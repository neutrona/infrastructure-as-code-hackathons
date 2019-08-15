#!/usr/bin/python3

import logging
import requests
from requests.auth import HTTPBasicAuth
import time
from os import getenv
import pika
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

if getenv('RMQ_ROUTING_KEY'):
    RPC_ROUTING_KEY = getenv('RPC_ROUTING_KEY')
else:
    RPC_ROUTING_KEY = 'shift_rpm_rpc_replies'

if getenv('NETCONF_BROKER_API_HOST'):
    NETCONF_BROKER_API_HOST = getenv('NETCONF_BROKER_API_HOST')
else:
    NETCONF_BROKER_API_HOST = '127.0.0.1'

if getenv('NETCONF_BROKER_API_USERNAME'):
    NETCONF_BROKER_API_USERNAME = getenv('NETCONF_BROKER_API_USERNAME')
else:
    NETCONF_BROKER_API_USERNAME = 'admin'

if getenv('NETCONF_BROKER_API_PASSWORD'):
    NETCONF_BROKER_API_PASSWORD = getenv('NETCONF_BROKER_API_PASSWORD')
else:
    NETCONF_BROKER_API_PASSWORD = 'admin'


def main():
    api_url = 'http://'+NETCONF_BROKER_API_HOST+':8646/Inventory'
    api_user = NETCONF_BROKER_API_USERNAME
    api_password = NETCONF_BROKER_API_PASSWORD

    try:
        if publisher.is_ready:
            payload = {'limit': '0'}
            response = requests.get(url=api_url, auth=HTTPBasicAuth(username=api_user, password=api_password), params=payload)
            content = response.json()

            rpc = '''
                <rpc>
                    <get-probe-results>
                    </get-probe-results>
                </rpc>
                '''

            for i in content:  # iterates among all of nodes and sends rpc-requests
                if i['Is_Message_Broker_Channel_Open']:
                    logger.info(
                        'Sending RPC -> Node Name: %s. RPC Routing Key: %s.' % (i['Node_Name'], i['RPC_Routing_Key']))
                    properties = pika.BasicProperties(reply_to=RPC_QUEUE, correlation_id=i['Node_Name'])

                    publisher.publish(properties=properties, routing_key=i['RPC_Routing_Key'], body=rpc)

    except requests.exceptions.ConnectionError:     # Exception handling when netconf broker is not working
        logger.info('Received connection refused! Trying again later')
        pass


if __name__ == '__main__':

    threads = []

    publisher = RMQPublisher()
    publisher.host = RMQ_HOST
    publisher.user = RMQ_USERNAME
    publisher.password = RMQ_PASSWORD
    publisher.exchange = RPC_EXCHANGE
    publisher.queue = RPC_QUEUE
    publisher.routing_key = RPC_ROUTING_KEY
    publisher.start()
    publisher.setName(HOSTNAME+'_RPMRPCRequester')
    threads.append(publisher)

    try:
        threads_monitor = ThreadsMonitor(threads)
        threads_monitor.start()

        # Starts exposing prometheus metrics
        start_http_server(60000)

        while True:
            main()
            time.sleep(60)

    except KeyboardInterrupt:
        try:
            publisher.close()
        except AttributeError:
            pass
