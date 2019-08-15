#!/usr/bin/python3
# -*- coding: utf-8 -*-

import sys
import threading
import time
import socket
import logging
from rmq_fanout_publisher import *
import json
from json.decoder import JSONDecodeError
import configparser
from threads_monitoring import *
from prometheus_client import start_http_server
import os
from os import getenv
import socket

HOSTNAME = os.uname()[1]

LOG_FORMAT = ('%(levelname) -10s %(asctime)s %(name) -30s %(funcName) '
              '-35s %(lineno) -5d: %(message)s')
logging.basicConfig(level=logging.INFO, format=LOG_FORMAT)
logger = logging.getLogger(__name__)

# Getting environent variables. If None, then a default one is assigned

if getenv('LOCAL_SERVER_BINDING_ADDRESS'):
    LOCAL_SERVER_BINDING_ADDRESS = getenv('LOCAL_SERVER_BINDING_ADDRESS')
else:
    LOCAL_SERVER_BINDING_ADDRESS = '127.0.0.1'

if getenv('LOCAL_SERVER_BINDING_PORT'):
    LOCAL_SERVER_BINDING_PORT = int(getenv('LOCAL_SERVER_BINDING_PORT'))
else:
    LOCAL_SERVER_BINDING_PORT = 54321

if getenv('REMOTE_SERVER_PORT'):
    REMOTE_SERVER_PORT = int(getenv('REMOTE_SERVER_PORT'))
else:
    REMOTE_SERVER_PORT = 54321

if getenv('SPEAKER_PREFERENCE'):
    SPEAKER_PREFERENCE = int(getenv('SPEAKER_PREFERENCE'))
else:
    SPEAKER_PREFERENCE = 150

if getenv('ATTACHED_DATA_QUEUE'):
    ATTACHED_DATA_QUEUE = getenv('ATTACHED_DATA_QUEUE')
else:
    ATTACHED_DATA_QUEUE = 'shift_links_topology_queue_1'

if getenv('ATTACHED_DATA_KEY'):
    ATTACHED_DATA_KEY = getenv('ATTACHED_DATA_KEY')
else:
    ATTACHED_DATA_KEY = 'shift_links_topology_key_1'

if getenv('REMOTE_SERVER_ADDRESS'):
    REMOTE_SERVER_ADDRESS = getenv('REMOTE_SERVER_ADDRESS')
else:
    REMOTE_SERVER_ADDRESS = 'exabgp-slave'

if getenv('BGP_PEER'):
    BGP_PEER = getenv('BGP_PEER')
else:
    BGP_PEER = '127.0.0.1'

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

if getenv('RMQ_CONTROL_EXCHANGE'):
    RMQ_CONTROL_EXCHANGE = getenv('RMQ_CONTROL_EXCHANGE')
else:
    RMQ_CONTROL_EXCHANGE = 'fanout'

if getenv('RMQ_CONTROL_QUEUE'):
    RMQ_CONTROL_QUEUE = getenv('RMQ_CONTROL_QUEUE')
else:
    RMQ_CONTROL_QUEUE = 'fanout'

if getenv('RMQ_CONTROL_ROUTING_KEY'):
    RMQ_CONTROL_ROUTING_KEY = getenv('RMQ_CONTROL_ROUTING_KEY')
else:
    RMQ_CONTROL_ROUTING_KEY = 'fanout'    

if getenv('MASTERSHIP_PROM_PORT'):
    MASTERSHIP_PROM_PORT = int(getenv('MASTERSHIP_PROM_PORT'))
else:
    MASTERSHIP_PROM_PORT = 60001

LOCAL_DATA = {'bgp_state': False, 'bgp_peer': BGP_PEER, 'queue': ATTACHED_DATA_QUEUE, 'routing_key': ATTACHED_DATA_KEY,
              'is_master': False}
REMOTE_DATA = {}


#################################### CLIENT CODE PORTION ####################################

class ClientConnection(threading.Thread):
    '''
     Handles the connection with writer server
    '''
    def __init__(self):
        threading.Thread.__init__(self)

    def run(self):
        self.connected = False
        while True:
            try:
                if not self.connected:   # Tries to connect to writer server
                    # create an INET, STREAMing socket
                    self.conn = socket.socket()
                    # now connect to the web server on port 12345
                    self.conn.connect((REMOTE_SERVER_ADDRESS, REMOTE_SERVER_PORT))
                    self.conn.setsockopt(socket.SOL_SOCKET, socket.SO_KEEPALIVE, 1)
                    self.connected = True
                    logger.info('Connection to speaker server established!')

            except ConnectionResetError:    # Raised when remote writer disconnects
                self.connected = False
                self.conn.close()
                time.sleep(5)
                logger.info('Connection to speaker server lost! Trying to reconnect')

                # When remote speaker disconnects, it assumes mastership
                if rmq_publisher.is_ready:
                    rmq_publisher.publish(json.loads(json.dumps(LOCAL_DATA)))
                pass

            except ConnectionRefusedError:
                logger.info('Connection refused!')
                time.sleep(5)
                pass

            except TimeoutError:
                logger.info('Connection timeout!')
                time.sleep(5)
                pass

            except socket.gaierror:
                logger.info('Could not resolve name of remote server!')
                time.sleep(5)
                pass

    def send_my_data(self):
        '''
        :return:

        '''
        if self.connected:
            try:
                # Sends local data to remote writer
                self.conn.send(json.dumps(LOCAL_DATA).encode())
            except BrokenPipeError:
                logger.info("Pipe to remote server is broken. Trying to reconnect")
                self.run()
                logger.info("Trying to send my data after pipe broken")
                self.send_my_data()
        else:
            logger.info("Not connected to remote server. Trying to reconnect")
            self.run()

#####################################################################################


def handler(message):
    '''
    :param message:
    :return:

    '''
    global LOCAL_DATA

    if 'keepalive' in message:
        # Update local BGP status and then send it to client
        LOCAL_DATA['bgp_state'] = True
        if rmq_publisher.is_ready:
            rmq_publisher.publish(json.dumps(LOCAL_DATA))

        if client_connection.connected:
            client_connection.send_my_data()
            logger.info('KEEPALIVE received. Current local state:\n%s' % LOCAL_DATA)

    elif 'notification' in message:
        # Update local BGP status and then send it to client
        LOCAL_DATA['bgp_state'] = False
        LOCAL_DATA['is_master'] = False
        if client_connection.connected:
            client_connection.send_my_data()
            logger.info('NOTIFICATION received. Current local state:\n%s' % LOCAL_DATA)

    else:
        pass

#################################### SERVER CODE PORTION ####################################


class ServerConnection(threading.Thread):
    '''
    In charge fo handling the connection with remote writer
    '''
    def __init__(self, clientsocket):
        threading.Thread.__init__(self)
        self.clientsocket = clientsocket

    def run(self):
        logger.info('Connection established on: %s' % threading.current_thread())

        while True:
            try:
                global REMOTE_DATA
                # Saves data received from remote writer
                REMOTE_DATA = json.loads(self.clientsocket.recv(1024).decode())
                if not REMOTE_DATA: break

                if REMOTE_DATA['bgp_state'] is False:
                    LOCAL_DATA['is_master'] = True
                    if rmq_publisher.is_ready:
                        rmq_publisher.publish(json.dumps(LOCAL_DATA))

                    logger.info('Remote BGP session went down! Assuming the control!')

            except ConnectionResetError:    # Raised when remote writer disconnects
                self.clientsocket.close()
                if rmq_publisher.is_ready:
                    rmq_publisher.publish(json.dumps(LOCAL_DATA))

                client_connection.conn.close()
                client_connection.connected = False
                logger.info('Connection from remote speaker closed in %s' % threading.current_thread())
                break

            except JSONDecodeError:
                self.clientsocket.close()
                if rmq_publisher.is_ready:
                    rmq_publisher.publish(json.dumps(LOCAL_DATA))

                client_connection.conn.close()
                client_connection.connected = False
                logger.info('Connection from remote speaker closed in %s' % threading.current_thread())
                break


class ServerListener(threading.Thread):
    '''
    Listens for any new TCP connection.
    '''
    def __init__(self):
        threading.Thread.__init__(self)

    def run(self):
        try:
            # create an INET, STREAMing socket
            self.serversocket = socket.socket()
            # bind the socket to a public host, and a well-known port
            self.serversocket.bind((LOCAL_SERVER_BINDING_ADDRESS, LOCAL_SERVER_BINDING_PORT))
            # become a server socket
            self.serversocket.listen(1)
        except OSError as ee:
            if ee.errno == 98:
                logger.info(ee)
                time.sleep(3)
                self.run()
                pass

        # Keeps continuously listening for new connections
        while True:
            # accept connections
            (clientsocket, address) = self.serversocket.accept()
            # Once a new connection is accepted, handle it on a new thread
            client = ServerConnection(clientsocket)
            # client.daemon = True
            client.start()
            # client.setName(HOSTNAME+'_ServerNewConnectionListener')
            # threads_monitor.threads.append(client)

#############################################################################################


class STDINThread(threading.Thread):
    '''
    Reads every message received from exabgp

    '''
    def __init__(self):
        threading.Thread.__init__(self)
        time.sleep(5)

    def run(self):
        line = "start"
        while line != "":
            line = sys.stdin.readline().strip()
            handler(line)


if __name__ == '__main__':

    threads = []

    rmq_publisher = RMQPublisher()
    rmq_publisher.host = RMQ_HOST
    rmq_publisher.port = RMQ_PORT
    rmq_publisher.user = RMQ_USERNAME
    rmq_publisher.password = RMQ_PASSWORD
    rmq_publisher.exchange = RMQ_CONTROL_EXCHANGE
    rmq_publisher.queue = RMQ_CONTROL_QUEUE
    rmq_publisher.routing_key = RMQ_CONTROL_ROUTING_KEY
    rmq_publisher.start()
    rmq_publisher.setName(HOSTNAME+'_ControlPublisher')
    threads.append(rmq_publisher)

    # Listener for new connection from client speaker
    server_listener_thread = ServerListener()
    # server_listener_thread.daemon = True
    server_listener_thread.start()
    server_listener_thread.setName(HOSTNAME+'_ServerListener')
    threads.append(server_listener_thread)

    client_connection = ClientConnection()
    # client_connection.daemon = True
    client_connection.start()
    client_connection.setName(HOSTNAME+'_BGPSpeakerClientConnection')
    threads.append(client_connection)

    stdinT = STDINThread()
    stdinT.start()
    stdinT.setName(HOSTNAME+'_BGPNotificationListener')
    threads.append(stdinT)

    global threads_monitor

    threads_monitor = ThreadsMonitor(threads)
    threads_monitor.start()

    # Starts exposing prometheus metrics
    start_http_server(MASTERSHIP_PROM_PORT)

    if SPEAKER_PREFERENCE == 150:
        while True:
            if rmq_publisher.is_ready and LOCAL_DATA['bgp_state']:
                LOCAL_DATA['is_master'] = True
                rmq_publisher.publish(json.dumps(LOCAL_DATA))
                break
            else:
                time.sleep(1)