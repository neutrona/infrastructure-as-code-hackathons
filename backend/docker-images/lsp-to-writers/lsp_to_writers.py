#!/usr/bin/python3

import logging
import json
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
    RMQ_COLLECTOR_QUEUE = 'shift_lsp_topology_queue'

if getenv('RMQ_COLLECTOR_ROUTING_KEY'):
    RMQ_COLLECTOR_ROUTING_KEY = getenv('RMQ_COLLECTOR_ROUTING_KEY')
else:
    RMQ_COLLECTOR_ROUTING_KEY = 'shift_lsp_topology_key'

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


def updatedb(update_message):
    try:
        a_ipv4 = update_message['lsp']['ipv4_tunnel_sender_address']
        z_ipv4 = update_message['lsp']['ipv4_tunnel_endpoint_address']
        bw = update_message['bandwidth']
        name = update_message['lsp']['symbolic_path_name']
        lsp_id = update_message['lsp']['lsp_id']
        lsp_tunnel_id = update_message['lsp']['tunnel_id']
        lsp_extended_tunnel_id = update_message['lsp']['extended_tunnel_id']
        operational = update_message['lsp']['flags']['operational']
        administrative = update_message['lsp']['flags']['administrative']
        delegate = update_message['lsp']['flags']['delegate']
        remove = update_message['lsp']['flags']['remove']
        sync = update_message['lsp']['flags']['sync']
        pcc = update_message['lsp']['pcc']

        if remove:
            query = '''
            MATCH ()-[l:LSP]-() WHERE l.Symbolic_Path_Name = '%s'
            DELETE l
            ''' % name

        elif operational:
            lsp_rro = []
            if update_message['rro'] != None:
                for i in update_message['rro']:
                    lsp_rro.append(i['address'])

            query = '''
            MERGE (a:Node {IPv4_Router_Identifier:'%s', topology_id:'mpls'})
               ON CREATE SET
                 a.topology_id='mpls',
                 a.IPv4_Router_Identifier='%s',
                 a.First_Seen = timestamp()
            MERGE (z:Node {IPv4_Router_Identifier:'%s', topology_id:'mpls'})
              ON CREATE SET
                z.topology_id='mpls',
                z.IPv4_Router_Identifier='%s',
                z.First_Seen = timestamp()
            MERGE (a)-[lsp:LSP {Symbolic_Path_Name:'%s'}]->(z)
              ON CREATE SET
                lsp.First_Seen = timestamp(),
                lsp.Last_Event = timestamp(),
                lsp.Reserved_Bandwidth = %i,
                lsp.Symbolic_Path_Name = '%s',
                lsp.IPv4_Tunnel_Sender_Address = '%s',
                lsp.IPv4_Tunnel_Endpoint_Address = '%s',
                lsp.LSP_Identifier = '%s',
                lsp.Tunnel_Identifier = '%s',
                lsp.Extended_Tunnel_Identifier = '%s',
                lsp.Operational = %s,
                lsp.Administrative = %s,
                lsp.Delegate = %s,
                lsp.Remove = %s,
                lsp.Sync = %s,
                lsp.Record_Route_Object = %s,
                lsp.PCC = '%s'
              ON MATCH SET
                lsp.Last_Event = timestamp(),
                lsp.Reserved_Bandwidth = %i,
                lsp.Record_Route_Object = %s,
                lsp.LSP_Identifier = '%s',
                lsp.Tunnel_Identifier = '%s',
                lsp.Extended_Tunnel_Identifier = '%s',
                lsp.Operational = %s,
                lsp.Administrative = %s,
                lsp.Delegate = %s,
                lsp.Remove = %s,
                lsp.Sync = %s,
                lsp.PCC = '%s'
            ''' % (a_ipv4, a_ipv4, z_ipv4, z_ipv4, name, int(bw), name, a_ipv4, z_ipv4, lsp_id, lsp_tunnel_id,
                   lsp_extended_tunnel_id, operational, administrative, delegate, remove, sync, lsp_rro, pcc,
                   int(bw), lsp_rro, lsp_id, lsp_tunnel_id, lsp_extended_tunnel_id, operational, administrative,
                   delegate, remove, sync, pcc)

        else:
            query = '''
            MERGE (a:Node {IPv4_Router_Identifier:'%s', topology_id:'mpls'})
              ON CREATE SET
                a.topology_id='mpls',
                a.IPv4_Router_Identifier='%s',
                a.First_Seen = timestamp()
            MERGE (z:Node {IPv4_Router_Identifier: '%s', topology_id:'mpls'})
              ON CREATE SET
                z.topology_id='mpls',
                z.IPv4_Router_Identifier='%s',
                z.First_Seen=timestamp()
            MERGE (a)-[lsp:LSP {Symbolic_Path_Name:'%s'}]->(z)
              ON CREATE SET
                lsp.First_Seen = timestamp(),
                lsp.Last_Event = timestamp(),
                lsp.Reserved_Bandwidth = %i,
                lsp.Symbolic_Path_Name = '%s',
                lsp.IPv4_Tunnel_Sender_Address = '%s',
                lsp.IPv4_Tunnel_Endpoint_Address = '%s',
                lsp.LSP_Identifier = '%s',
                lsp.Tunnel_Identifier = '%s',
                lsp.Extended_Tunnel_Identifier= '%s',
                lsp.Operational = %s,
                lsp.Administrative = %s,
                lsp.Delegate = %s,
                lsp.Remove = %s,
                lsp.Sync = %s,
                lsp.PCC = '%s'
              ON MATCH SET
                lsp.Last_Event = timestamp(),
                lsp.Reserved_Bandwidth = %i,
                lsp.LSP_Identifier = '%s',
                lsp.Tunnel_Identifier = '%s',
                lsp.Extended_Tunnel_Identifier = '%s',
                lsp.Operational = %s,
                lsp.Administrative = %s,
                lsp.Delegate = %s,
                lsp.Remove = %s,
                lsp.Sync = %s,
                lsp.PCC = '%s'
            ''' % (a_ipv4, a_ipv4, z_ipv4, z_ipv4, name, int(bw), name, a_ipv4, z_ipv4, lsp_id, lsp_tunnel_id,
                   lsp_extended_tunnel_id, operational, administrative, delegate, remove, sync, pcc, int(bw),
                   lsp_id, lsp_tunnel_id, lsp_extended_tunnel_id, operational, administrative, delegate,
                   remove, sync, pcc)

        if rmq_publisher_client.is_ready:
            rmq_publisher_client.publish(query)
        if rmq_publisher_server.is_ready:
            rmq_publisher_server.publish(query)

    except KeyError:
        pass


def on_message_callback(ch, method, properties, body):
    update_message = json.loads(body.decode('utf-8'))
    logger.info(update_message)
    try:
        if update_message['lsp']['flags']['remove']:
            updatedb(update_message)
        elif (update_message['lsp']['ipv4_tunnel_sender_address'] != '0.0.0.0') or \
                (update_message['lsp']['ipv4_tunnel_endpoint_address'] != '0.0.0.0'):
            updatedb(update_message)
        ch.basic_ack(delivery_tag=method.delivery_tag)  # Ack the message if it was properly processed
    except Exception as ee:
        # Explicit unack the message in case there's an error while processing it, so that it is put back to the queue
        ch.basic_nack(delivery_tag=method.delivery_tag)
        logger.info("EXCEPTION: %s" %ee)


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
    rmq_collector.setName(HOSTNAME+'_LSPDataConsumer')
    threads.append(rmq_collector)

    rmq_publisher_server = RMQPublisher()
    rmq_publisher_server.host = RMQ_PUBLISHER_HOST
    rmq_publisher_server.user = RMQ_PUBLISHER_USERNAME
    rmq_publisher_server.password = RMQ_PUBLISHER_PASSWORD
    rmq_publisher_server.exchange = RMQ_PUBLISHER_EXCHANGE
    rmq_publisher_server.queue = RMQ_PUBLISHER_SERVER_QUEUE
    rmq_publisher_server.routing_key = RMQ_PUBLISHER_SERVER_ROUTING_KEY
    rmq_publisher_server.start()
    rmq_publisher_server.setName(HOSTNAME+'_LSPsQueryPublisherServerQueue')
    threads.append(rmq_publisher_server)

    rmq_publisher_client = RMQPublisher()
    rmq_publisher_client.host = RMQ_PUBLISHER_HOST
    rmq_publisher_client.user = RMQ_PUBLISHER_USERNAME
    rmq_publisher_client.password = RMQ_PUBLISHER_PASSWORD
    rmq_publisher_client.exchange = RMQ_PUBLISHER_EXCHANGE
    rmq_publisher_client.queue = RMQ_PUBLISHER_CLIENT_QUEUE
    rmq_publisher_client.routing_key = RMQ_PUBLISHER_CLIENT_ROUTING_KEY
    rmq_publisher_client.start()
    rmq_publisher_client.setName(HOSTNAME+'_LSPsQueryPublisherClientQueue')
    threads.append(rmq_publisher_client)

    threads_monitor = ThreadsMonitor(threads)
    threads_monitor.start()

    # Starts exposing prometheus metrics
    start_http_server(60000)