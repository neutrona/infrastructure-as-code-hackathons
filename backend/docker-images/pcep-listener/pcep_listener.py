#!/usr/bin/python3

import threading
import socketserver
from time import sleep
from datetime import datetime
from pcep_handler import PCEP
import json
import logging
from os import getenv
from rmq_publisher import *
from threads_monitoring import *
from prometheus_client import start_http_server
import os

HOSTNAME = os.uname()[1]

LOG_FORMAT = ('%(levelname) -10s %(asctime)s %(name) -30s %(funcName) '
              '-35s %(lineno) -5d: %(message)s')
logging.basicConfig(level=logging.INFO, format=LOG_FORMAT)
logger = logging.getLogger(__name__)

"""
 Ref.:
 
 socketserver — A framework for network servers
 https://docs.python.org/3/library/socketserver.html

"""

# Getting environent variables. If None, then a default one is assigned

if getenv('PCEP_BINDING_ADDRESS'):
    PCEP_BINDING_ADDRESS = getenv('PCEP_BINDING_ADDRESS')
else:
    PCEP_BINDING_ADDRESS = '127.0.0.1'

if getenv('PCEP_BINDING_PORT'):
    PCEP_BINDING_PORT = getenv('PCEP_BINDING_PORT')
else:
    PCEP_BINDING_PORT = 4189

if getenv('RMQ_HOST'):
    RMQ_HOST = getenv('RMQ_HOST')
else:
    RMQ_HOST = '127.0.0.1'

if getenv('RMQ_PORT'):
    RMQ_PORT = getenv('RMQ_PORT')
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
    RMQ_EXCHANGE = 'shift_topology_exchange'

if getenv('RMQ_QUEUE'):
    RMQ_QUEUE = getenv('RMQ_QUEUE')
else:
    RMQ_QUEUE = 'shift_lsp_topology_queue'

if getenv('RMQ_ROUTING_KEY'):
    RMQ_ROUTING_KEY = getenv('RMQ_ROUTING_KEY')
else:
    RMQ_ROUTING_KEY = 'shift_lsp_topology_key'

exiting = False


class ThreadedTCPRequestHandler(socketserver.BaseRequestHandler):

    # PCEP 'On PCRpt' Callback
    def on_pcrpt(self, pcrpt):
        pcrpt_json = json.dumps(pcrpt, indent=3, sort_keys=True)
        # Publish PCRpt to RMQ
        if rmq_publisher.is_ready:
            # publish_thread = threading.Thread(target=rmq_publisher.publish, args=[pcrpt_json])
            # publish_thread.start()
            rmq_publisher.publish(pcrpt_json)

    def handle(self):

        # Instantiate PCEP handler and pass client socket, stop_event and on_pcrpt callback
        stop_event = threading.Event()
        stop_event.clear()

        pcep = PCEP(self.request, stop_event=stop_event, on_pcrpt_callback=self.on_pcrpt)

        # Main thread exiting flag
        while not exiting:
            # Run PCEP handler
            if not pcep.run():
                break

        stop_event.set()

        client_thread = threading.current_thread()
        logger.info('%s. Exiting PCEP client loop running in thread: %s. Client address: %s. ' % (datetime.now(),
                                                                                                  client_thread.name,
                                                                                                  self.client_address))


class ThreadedTCPServer(socketserver.ThreadingMixIn, socketserver.TCPServer):
    pass


if __name__ == '__main__':

    threads = []

    # Instantiate and start RMQThread

    rmq_publisher = RMQPublisher()
    rmq_publisher.host = RMQ_HOST
    rmq_publisher.port = RMQ_PORT
    rmq_publisher.user = RMQ_USERNAME
    rmq_publisher.password = RMQ_PASSWORD
    rmq_publisher.exchange = RMQ_EXCHANGE
    rmq_publisher.queue = RMQ_QUEUE
    rmq_publisher.routing_key = RMQ_ROUTING_KEY
    rmq_publisher.start()
    rmq_publisher.setName(HOSTNAME+'_LSPDataPublisher')
    threads.append(rmq_publisher)

    # Port 0 means to select an arbitrary unused port
    HOST, PORT = PCEP_BINDING_ADDRESS, PCEP_BINDING_PORT

    # Instantiate a ThreadedTCPServer binding the handler
    server = ThreadedTCPServer((HOST, PORT), ThreadedTCPRequestHandler)

    # Allow reuse address for short recovery after server shutdown
    server.allow_reuse_address = True

    # Get the local socket ip and port
    ip, port = server.server_address

    # Start a thread with the server -- that thread will then start one
    # more thread for each request
    server_thread = threading.Thread(target=server.serve_forever)

    try:
        # Exit the server thread when the main thread terminates
        server_thread.daemon = True
        server_thread.start()
        server_thread.setName(HOSTNAME+'_PCEPServerListener')
        threads.append(server_thread)

        logger.info('PCEP Server loop running in thread: %s' % server_thread.name)
        # print('Press [Enter] to exit.')

        # Wait for user input in main thread, exit if any
        #input()

        threads_monitor = ThreadsMonitor(threads)
        threads_monitor.start()

        # Starts exposing prometheus metrics
        start_http_server(60000)

        while True:
            pass

        # Exit the server and main threads
        #logger.info('%s. Exiting PCEP server loop running in thread: %s' % (datetime.now(), server_thread.name))

        #exiting = True
        #sleep(10)
        #server.shutdown()
        #server.server_close()

    except KeyboardInterrupt:
        # Exit the server and main threads
        logger.info('%s. Exiting PCEP server loop running in thread: %s' % (datetime.now(), server_thread.name))

        exiting = True
        sleep(10)
        server.shutdown()
        server.server_close()
