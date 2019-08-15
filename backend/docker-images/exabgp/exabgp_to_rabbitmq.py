#!/usr/bin/python3
# -*- coding: utf-8 -*-

import sys
import threading
import logging
import time
from rmq_publisher import *
from threads_monitoring import *
from prometheus_client import start_http_server
from os import getenv
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

if getenv('RMQ_PORT'):
    RMQ_PORT = int(getenv('RMQ_PORT'))
else:
    RMQ_PORT = 5672    

if getenv('RMQ_USERNAME'):
    RMQ_USERNAME = getenv('RMQ_USERNAME')
else:
    RMQ_USERNAME = 'guest'

if getenv('RMQ_PASSWORD'):
    RMQ_PASSWORD = getenv('RMQ_PASSWORD')
else:
    RMQ_PASSWORD = 'guest'

if getenv('RMQ_EXCHANGE'):
    RMQ_EXCHANGE = getenv('RMQ_EXCHANGE')
else:
    RMQ_EXCHANGE = 'direct'

if getenv('RMQ_QUEUE'):
    RMQ_QUEUE = getenv('RMQ_QUEUE')
else:
    RMQ_QUEUE = 'default'

if getenv('RMQ_ROUTING_KEY'):
    RMQ_ROUTING_KEY = getenv('RMQ_ROUTING_KEY')
else:
    RMQ_ROUTING_KEY = 'default'

if getenv('EXABGP_PROM_PORT'):
    EXABGP_PROM_PORT = int(getenv('EXABGP_PROM_PORT'))
else:
    EXABGP_PROM_PORT = 60000


class STDINThread(threading.Thread):
    def __init__(self):
        threading.Thread.__init__(self)
        time.sleep(5)

    def run(self):
        line = "start"
        while line != "":
            line = sys.stdin.readline().strip()
            if rmq_publisher.is_ready:
                rmq_publisher.publish(line)


if __name__ == "__main__":
    threads = []

    rmq_publisher = RMQPublisher()
    rmq_publisher.host = RMQ_HOST
    rmq_publisher.port = RMQ_PORT
    rmq_publisher.user = RMQ_USERNAME
    rmq_publisher.password = RMQ_PASSWORD
    rmq_publisher.exchange = RMQ_EXCHANGE
    rmq_publisher.queue = RMQ_QUEUE
    rmq_publisher.routing_key = RMQ_ROUTING_KEY
    rmq_publisher.start()
    rmq_publisher.setName(HOSTNAME+'_LinkDataPublisher')
    threads.append(rmq_publisher)

    stdinT = STDINThread()
    stdinT.start()
    stdinT.setName(HOSTNAME+'_BGPUpdateListener')
    threads.append(stdinT)

    threads_monitor = ThreadsMonitor(threads)
    threads_monitor.start()

    # Starts exposing prometheus metrics
    start_http_server(EXABGP_PROM_PORT)
