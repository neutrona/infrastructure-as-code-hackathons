#!/usr/bin/python3

import socket
import logging
import threading
import time
from rmq_consumer import *
from rmq_publisher import *
from neo4j_writer import *
import json
import requests
from requests.auth import HTTPBasicAuth as requests_basic_auth
from os import getenv
from neo4j.exceptions import CypherSyntaxError
from threads_monitoring import *
from prometheus_client import start_http_server
from json.decoder import JSONDecodeError
import os

HOSTNAME = os.uname()[1]

LOG_FORMAT = ('%(levelname) -10s %(asctime)s %(name) -30s %(funcName) '
              '-35s %(lineno) -5d: %(message)s')
logging.basicConfig(level=logging.INFO, format=LOG_FORMAT)
logger = logging.getLogger(__name__)

WRITER_SERVER_PORT = 12345
WRITER_PREFERENCE = 100
DB_OPERATION_COUNT = 0

# Getting environent variables. If None, then a default one is assigned

if getenv('WRITER_SERVER_ADDRESS'):
    WRITER_SERVER_ADDRESS = getenv('WRITER_SERVER_ADDRESS')
else:
    WRITER_SERVER_ADDRESS = '127.0.0.1'

if getenv('LOCAL_COUNTER_FILE'):
    LOCAL_COUNTER_FILE = getenv('LOCAL_COUNTER_FILE')
else:
    LOCAL_COUNTER_FILE = '/opt/db_ops_counter.txt'

if getenv('NEO4JDB_HOST_IP'):
    NEO4JDB_HOST_IP = getenv('NEO4JDB_HOST_IP')
else:
    NEO4JDB_HOST_IP = '127.0.0.1'

if getenv('NEO4JDB_USERNAME'):
    NEO4JDB_USERNAME = getenv('NEO4JDB_USERNAME')
else:
    NEO4JDB_USERNAME = 'neo4j'

if getenv('NEO4JDB_PASSWORD'):
    NEO4JDB_PASSWORD = getenv('NEO4JDB_PASSWORD')
else:
    NEO4JDB_PASSWORD = 'neo4j'

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

if getenv('RMQ_EXCHANGE'):
    RMQ_EXCHANGE = getenv('RMQ_EXCHANGE')
else:
    RMQ_EXCHANGE = 'shift_topology_exchange'

if getenv('RMQ_QUEUE'):
    RMQ_QUEUE = getenv('RMQ_QUEUE')
else:
    RMQ_QUEUE = 'shift_topology_queue_writer_client'

if getenv('RMQ_ROUTING_KEY'):
    RMQ_ROUTING_KEY = getenv('RMQ_ROUTING_KEY')
else:
    RMQ_ROUTING_KEY = 'shift_topology_key_writer_client'

if getenv('LB_HOST'):
    LB_HOST = getenv('LB_HOST')
else:
    LB_HOST = 'load-balancer'

if getenv('LB_PORT'):
    LB_PORT = getenv('LB_PORT')
else:
    LB_PORT = '8007'


class ConnToServerThread(threading.Thread):
    '''
     Handles the connection with writer server
    '''
    def __init__(self):
        threading.Thread.__init__(self)

    def run(self):
        connected = False
        while True:
            try:
                if not connected:   # Tries to connect to writer server
                    # create an INET, STREAMing socket
                    self.conn = socket.socket()
                    # now connect to the web server on port 12345
                    self.conn.connect((WRITER_SERVER_ADDRESS, WRITER_SERVER_PORT))
                    self.conn.setsockopt(socket.SOL_SOCKET, socket.SO_KEEPALIVE, 1)
                    connected = True
                    logger.info('Connection to writer server established!')
                else:   # When connection is established, starts sharing writers data
                    # Sends local data to remote writer
                    self.conn.send(json.dumps(local_writer_data).encode())

                    global remote_writer_data
                    # Saves data received from remote writer
                    remote_writer_data = json.loads(self.conn.recv(1024))
                    time.sleep(1)   # Sends its own info each 1 seconds

            except JSONDecodeError:
                connected = False
                self.conn.close()
                logger.info('Connection to writer server lost! Trying to reconnect')
                time.sleep(5)
                pass

            except ConnectionResetError:    # Raised when remote writer disconnects
                connected = False
                self.conn.close()
                time.sleep(5)
                logger.info('Connection to writer server lost! Trying to reconnect')

                # When remote writer disconnects, it assumes mastership
                master_writer_tracker.take_mastership()
                pass

            except ConnectionRefusedError:
                logger.info('Connection refused!')
                time.sleep(5)
                pass

            except TimeoutError:
                logger.info('Connection timeout!')
                time.sleep(5)
                pass


class PreferredTracker(threading.Thread):
    '''
    Used for tracking what's the preferred writer/DB
    '''
    become_master = False

    def __init__(self):
        threading.Thread.__init__(self)

    def take_mastership(self):
        '''
         Called when it determines it should be the master
        '''
        logger.info('I am the master')
        local_writer_data['is_master'] = True
        # Let the load balancers know the attached DB is the master
        requests.post('http://' + LB_HOST + ':' + LB_PORT, json=json.loads(json.dumps(local_writer_data)))

    def run(self):
        '''
         As soon as thread is started, it starts to monitor exchanged messages in order to determine whether or not
         current writer is the master.
        '''
        first_iteration = True

        while True:
            # At runtime, it indicates it's master
            if first_iteration and local_writer_data['db_down'] is False:
                self.take_mastership()
                first_iteration = False
                continue

            # Matches when remote DB is down and this is not master
            if remote_writer_data['db_down'] is True:
                if local_writer_data['is_master'] is False:
                    self.take_mastership()

            # When local DB is UP:
            elif local_writer_data['db_down'] is False:
                # Looks for the writer with more operations
                ops_diff = local_writer_data['db_operations'] - remote_writer_data['db_operations']

                # Matches if local writer is the one with more operations and it's not master already
                if ops_diff > 10:
                    if local_writer_data['is_master'] is False:
                        self.take_mastership()

                # When current writer is not the one with more operations
                elif ops_diff < 10:
                    local_writer_data['is_master'] = False
            time.sleep(1)


class DBStatusTracker(threading.Thread):
    '''
    Run to monitor the DB status. Marks it as down when there's no response.
    '''

    def __init__(self):
        threading.Thread.__init__(self)

    def run(self):
        while True:
            try:
                response = requests.get('http://'+NEO4JDB_HOST_IP+':7474',
                                        auth=requests_basic_auth(NEO4JDB_USERNAME, NEO4JDB_PASSWORD), timeout=2)
                if response.status_code == 200:
                    local_writer_data['db_down'] = False
            except:     # When exception is raised, it means there's no response from the DB.
                local_writer_data['db_down'] = True
                local_writer_data['is_master'] = False
                logger.info('Attached DataBase is DOWN!')
                pass
            time.sleep(2)


def on_message_callback(ch, method, properties, body):
    '''
    Function called for each message consumed.
    :param ch:
    :param method:
    :param properties:
    :param body:
    :return:

    '''
    if not local_writer_data['db_down']:    # Matched when neo4jDB is UP
        logger.info('Message consumed')
        logger.info(body.decode('utf-8'))
        query = body.decode('utf-8')
        try:
            neo.update(query)
            local_writer_data['db_operations'] += 1
            with open(LOCAL_COUNTER_FILE, 'w') as file:
                file.write(str(local_writer_data['db_operations']))
            ch.basic_ack(delivery_tag=method.delivery_tag)
        except CypherSyntaxError:
            if rmq_qpc.is_ready:
                rmq_qpc.publish(query)
            ch.basic_ack(delivery_tag=method.delivery_tag)
            logger.info('Wrong query received: \n \t Message properties: \n %s \n Query: \n %s'  % (properties, query))
            pass
        except AttributeError: # Raised when connection to DB is not already open
            ch.basic_nack(delivery_tag=method.delivery_tag) # Put the message back to the queue
            pass

    else:   # When DB is down, it sends a NACK to rabbitmq in order to put the message back to the queue
        logger.info('Explicit NACK sent')
        ch.basic_nack(delivery_tag=method.delivery_tag)


if __name__ == '__main__':

    # Sleeps just for waiting writer server is running and known within domain
    time.sleep(10)

    threads = []

    # Look for local file containing the database operations counter
    try:
        with open(LOCAL_COUNTER_FILE, 'r+') as file:
            counter = file.read()
            DB_OPERATION_COUNT = int(counter)

    except:
        with open(LOCAL_COUNTER_FILE, 'w') as file:
            file.write(str(DB_OPERATION_COUNT))

    global local_writer_data, remote_writer_data, neo, master_writer_tracker

    # Data to be shared between both writers
    local_writer_data = {'preference': WRITER_PREFERENCE, 'db_operations': DB_OPERATION_COUNT,
                         'attached_db': NEO4JDB_HOST_IP, 'db_down': False, 'is_master': False}
    remote_writer_data = {'preference': 150, 'db_operations': DB_OPERATION_COUNT,
                          'attached_db': NEO4JDB_HOST_IP, 'db_down': False, 'is_master': False}

    # Thread where a new connection to writer server is handled
    conn = ConnToServerThread()
    conn.start()
    conn.setName(HOSTNAME+'_ConnToWriterServer')
    threads.append(conn)

    # Neo4j DB connection
    neo = Neo4jDB()
    neo.neo4j_host = 'bolt://' + NEO4JDB_HOST_IP + ':7687'
    neo.neo4j_username = NEO4JDB_USERNAME
    neo.neo4j_password = NEO4JDB_PASSWORD
    neo.start()

    # RabbitMQ connection
    rmq = RMQConsumer(on_message_callback)
    rmq.host = RMQ_HOST
    rmq.user = RMQ_USERNAME
    rmq.password = RMQ_PASSWORD
    rmq.exchange = RMQ_EXCHANGE
    rmq.queue = RMQ_QUEUE
    rmq.routing_key = RMQ_ROUTING_KEY
    rmq.start()
    rmq.setName(HOSTNAME+'_QueryConsumer')
    threads.append(rmq)

    # Rabbitmq publisher (Only used for wrong queries)
    rmq_qpc = RMQPublisher()
    rmq_qpc.host = RMQ_HOST
    rmq_qpc.user = RMQ_USERNAME
    rmq_qpc.password = RMQ_PASSWORD
    rmq_qpc.exchange = RMQ_EXCHANGE
    rmq_qpc.queue = 'qpc_queue'
    rmq_qpc.routing_key = 'qpc_key'
    rmq_qpc.start()
    rmq_qpc.setName(HOSTNAME+'_QPCPublisher')
    threads.append(rmq_qpc)

    # Writers preference tracker
    master_writer_tracker = PreferredTracker()
    master_writer_tracker.start()
    master_writer_tracker.setName(HOSTNAME+'_WriterPreferenceTracker')
    threads.append(master_writer_tracker)

    # Tracker for the DB status
    db_status = DBStatusTracker()
    db_status.start()
    db_status.setName(HOSTNAME+'_DBStatusTracker')
    threads.append(db_status)

    threads_monitor = ThreadsMonitor(threads)
    threads_monitor.start()

    # Starts exposing prometheus metrics
    start_http_server(60000)