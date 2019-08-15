#!/usr/bin/python3

import logging
import json
import threading
from os import getenv
from rmq_consumer import *
from rmq_publisher import *
from rmq_fanout_consumer import *
from threads_monitoring import *
from prometheus_client import start_http_server
import os

HOSTNAME = os.uname()[1]

LOG_FORMAT = ('%(levelname) -10s %(asctime)s %(name) -30s %(funcName) '
              '-35s %(lineno) -5d: %(message)s')
logging.basicConfig(level=logging.INFO, format=LOG_FORMAT)
logger = logging.getLogger(__name__)

EXABGP_PEER = ''

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

if getenv('RMQ_CONTROL_EXCHANGE'):
    RMQ_CONTROL_EXCHANGE = getenv('RMQ_CONTROL_EXCHANGE')
else:
    RMQ_CONTROL_EXCHANGE = 'shift_control_exchange'

if getenv('RMQ_CONTROL_QUEUE'):
    RMQ_CONTROL_QUEUE = getenv('RMQ_CONTROL_QUEUE')
else:
    RMQ_CONTROL_QUEUE = 'shift_control_queue'

if getenv('RMQ_CONTROL_KEY'):
    RMQ_CONTROL_KEY = getenv('RMQ_CONTROL_KEY')
else:
    RMQ_CONTROL_KEY = 'shift_control_key'


class Node(object):
    name = ''
    ipv4_router_id = ''
    igp_router_id = ''


class UpdateNode(Node):
    def __init__(self, node_data):
        self.node_data = node_data

    def run(self):
        query = '''
            MERGE (x:Node {IGP_Router_Identifier:'%s', topology_id:'igp'})
              ON CREATE SET
                x.topology_id='igp',
                x.First_Seen=timestamp(),
                x.Node_Name='%s',
                x.IPv4_Router_Identifier='%s',
                x.IGP_Router_Identifier='%s'
              ON MATCH SET
                x.Last_Event=timestamp(),
                x.Node_Name='%s',
                x.IPv4_Router_Identifier='%s'
        ''' % (self.node_data.igp_router_id, self.node_data.name, self.node_data.ipv4_router_id,
               self.node_data.igp_router_id, self.node_data.name, self.node_data.ipv4_router_id)

        if rmq_publisher_client.is_ready:
            rmq_publisher_client.publish(query)
        if rmq_publisher_server.is_ready:
            rmq_publisher_server.publish(query)


class PseudoNode(object):
    psn = ''
    pn_igp_id = ''


class UpdatePseudonode(PseudoNode):
    def __init__(self, pn_data):
        self.pn_data = pn_data

    def run(self):
        query = '''
            MERGE (x:Pseudonode {IGP_Router_Identifier:'%s'})
              ON CREATE SET
                x.IGP_Router_Identifier='%s',
                x.psn='%s'
              ON MATCH SET
                x.IGP_Router_Identifier='%s',
                x.psn='%s'
        ''' % (self.pn_data.pn_igp_id, self.pn_data.pn_igp_id, self.pn_data.psn, self.pn_data.pn_igp_id,
               self.pn_data.psn)

        if rmq_publisher_client.is_ready:
            rmq_publisher_client.publish(query)
        if rmq_publisher_server.is_ready:
            rmq_publisher_server.publish(query)


class LinkUpstream(object):
    node_router_igp_id = ''
    psn_router_id = ''
    interface = ''
    max_link_bw = 0
    max_rsv_bw = 0
    unreserved_bw = []
    srlg = []
    asn = 0
    status = True


class UpdateRelationshipUpstream(LinkUpstream):
    def __init__(self, link_data):
        self.link_data = link_data

    def run(self):
        query = '''
            MERGE (n:Node {IGP_Router_Identifier:'%s', topology_id:'igp'})
              ON CREATE SET
                n.topology_id='igp',
                n.First_Seen = timestamp(),
                n.IGP_Router_Identifier='%s'
            MERGE (pn:Pseudonode {IGP_Router_Identifier:'%s'})
              ON CREATE SET
                pn.IGP_Router_Identifier='%s'
            MERGE (n)-[link:Link]->(pn)
              ON CREATE SET
                link.First_Seen = timestamp(),
                link.IPv4_Interface_Address = '%s',
                link.Maximum_Link_Bandwidth = %i,
                link.Maximum_Reservable_Bandwidth = %i,
                link.Unreserved_Bandwidth = %s,
                link.Shared_Risk_Link_Groups = %s,
                link.asn = %i,
                link.Operational_Status = %s,
                link.Last_Event = timestamp()
              ON MATCH SET
                link.IPv4_Interface_Address = '%s',
                link.Maximum_Link_Bandwidth = %i,
                link.Maximum_Reservable_Bandwidth = %i,
                link.Unreserved_Bandwidth = %s,
                link.Shared_Risk_Link_Groups = %s,
                link.asn = %i,
                link.Operational_Status = %s,
                link.Last_Event = timestamp()
        ''' % (self.link_data.node_router_igp_id, self.link_data.node_router_igp_id, self.link_data.psn_router_id,
               self.link_data.psn_router_id, self.link_data.interface, self.link_data.max_link_bw,
               self.link_data.max_rsv_bw, self.link_data.unreserved_bw, self.link_data.srlg, self.link_data.asn,
               self.link_data.status, self.link_data.interface, self.link_data.max_link_bw,
               self.link_data.max_rsv_bw, self.link_data.unreserved_bw, self.link_data.srlg, self.link_data.asn,
               self.link_data.status)

        if rmq_publisher_client.is_ready:
            rmq_publisher_client.publish(query)
        if rmq_publisher_server.is_ready:
            rmq_publisher_server.publish(query)


class LinkDownstream(object):
    node_router_igp_id = ''
    psn_router_id = ''
    asn = 0
    status = True


class UpdateRelationshipDownstream(LinkDownstream):
    def __init__(self, link_data):
        self.link_data = link_data

    def run(self):
        query = '''
            MERGE (n:Node {IGP_Router_Identifier:'%s', topology_id:'igp'})
              ON CREATE SET
                n.topology_id='igp',
                n.First_Seen = timestamp(),
                n.IGP_Router_Identifier='%s'
            MERGE (pn:Pseudonode {IGP_Router_Identifier:'%s'})
              ON CREATE SET pn.IGP_Router_Identifier='%s'
            MERGE (n)<-[link:Link]-(pn)
              ON CREATE SET
                link.First_Seen = timestamp(),
                link.asn = %i,
                link.Operational_Status = %s,
                link.Last_Event = timestamp()
              ON MATCH SET
                link.Operational_Status = %s,
                link.Last_Event = timestamp()
        ''' % (self.link_data.node_router_igp_id, self.link_data.node_router_igp_id, self.link_data.psn_router_id,
               self.link_data.psn_router_id, self.link_data.asn, self.link_data.status, self.link_data.status)

        if rmq_publisher_client.is_ready:
            rmq_publisher_client.publish(query)
        if rmq_publisher_server.is_ready:
            rmq_publisher_server.publish(query)


class DeletePseudonode(PseudoNode):
    def __init__(self, pn_data):
        self.pn_data = pn_data

    def run(self):
        query = '''
            MATCH (pn:Pseudonode {IGP_Router_Identifier:'%s'})
            DETACH DELETE pn ''' % self.pn_data.pn_igp_id

        if rmq_publisher_client.is_ready:
            rmq_publisher_client.publish(query)
        if rmq_publisher_server.is_ready:
            rmq_publisher_server.publish(query)


class DeleteNode(Node):
    def __init__(self, node_data):
        self.node_data = node_data

    def run(self):
        query = '''
            MATCH (n:Node {IGP_Router_Identifier:'%s', topology_id:'igp'})
            DETACH DELETE n ''' % self.node_data.igp_router_id

        if rmq_publisher_client.is_ready:
            rmq_publisher_client.publish(query)
        if rmq_publisher_server.is_ready:
            rmq_publisher_server.publish(query)


class DeleteUplink(LinkUpstream):
    def __init__(self, link_data):
        self.link_data = link_data

    def run(self):
        query = '''
            MATCH (n:Node {IGP_Router_Identifier:'%s', topology_id:'igp'}),
                  (pn:Pseudonode {IGP_Router_Identifier:'%s'}),
                  (n)-[link:Link]->(pn)
            DELETE link ''' % (self.link_data.node_router_igp_id, self.link_data.psn_router_id)

        if rmq_publisher_client.is_ready:
            rmq_publisher_client.publish(query)
        if rmq_publisher_server.is_ready:
            rmq_publisher_server.publish(query)


class DeleteDownlink(LinkDownstream):
    def __init__(self, link_data):
        self.link_data = link_data

    def run(self):
        query = '''
            MATCH (n:Node {IGP_Router_Identifier:'%s', topology_id:'igp'}),
                  (pn:Pseudonode {IGP_Router_Identifier:'%s'}),
                  (n)<-[link:Link]-(pn)
            DELETE link ''' % (self.link_data.node_router_igp_id, self.link_data.psn_router_id)

        if rmq_publisher_client.is_ready:
            rmq_publisher_client.publish(query)
        if rmq_publisher_server.is_ready:
            rmq_publisher_server.publish(query)


class Handler(object):
    update_message = ''

    def create(self, update, message):
        '''
        Called when receiving an update for creating or updating a node/link.
        :param update:
        :param message:
        :return:

        '''
        for i in update:
            nlri_type = i['ls-nlri-type']
            if nlri_type == 1:
                try:    # Assuming that the update processed corresponds to a node
                    mx = Node()
                    mx.name = message['neighbor']['message']['update']['attribute']['bgp-ls']['node-name']
                    mx.ipv4_router_id = message['neighbor']['message']['update']['attribute']['bgp-ls']['local-te-router-id']
                    mx.igp_router_id = i['node-descriptors']['router-id']
                    UpdateNode(mx).run()
                except KeyError:    # Raised when 'node-name' index is not in the update, meaning that it corresponds to a pseudonode update
                    pn = PseudoNode()
                    pn.psn = i['node-descriptors']['psn']
                    pn.pn_rid = i['node-descriptors']['router-id']
                    pn.pn_igp_id = pn.pn_rid + pn.psn
                    UpdatePseudonode(pn).run()

            elif nlri_type == 2:
                try:    # Assuming that the update corresponds to a upstream (seen from the node) link
                    link = LinkUpstream()
                    link.node_router_igp_id = i['local-node-descriptors']['router-id']
                    link.psn_router_id = i['remote-node-descriptors']['router-id'] + i['remote-node-descriptors']['psn']
                    link.interface = i['interface-address']['interface-address']
                    link.max_link_bw = message['neighbor']['message']['update']['attribute']['bgp-ls']['maximum-link-bandwidth']
                    link.unreserved_bw = message['neighbor']['message']['update']['attribute']['bgp-ls']['unreserved-bandwidth']
                    link.max_rsv_bw = message['neighbor']['message']['update']['attribute']['bgp-ls']['maximum-reservable-link-bandwidth']
                    try:
                      link.srlg = message['neighbor']['message']['update']['attribute']['bgp-ls']['shared-risk-link-groups']
                    except:
                      pass
                    link.asn = i['local-node-descriptors']['autonomous-system']
                    UpdateRelationshipUpstream(link).run()
                except KeyError:    # Raised when 'psn' is not in 'remote node descriptor', meaning that the link update corresponds to a downstream (seen from the pseudonode) link
                    link = LinkDownstream()
                    link.node_router_igp_id = i['remote-node-descriptors']['router-id']
                    link.psn_router_id = i['local-node-descriptors']['router-id'] + i['local-node-descriptors']['psn']
                    link.asn = i['local-node-descriptors']['autonomous-system']
                    UpdateRelationshipDownstream(link).run()

    def link_down(self, update):
        '''
        Called when a link goes down. It deletes corresponding pseudo node and all of the relationships it has.
        :param psn_router_id: IGP router ID of the pseudo node corresponding to the link that went down.
        :return:

        '''
        for i in update:
            if i['ls-nlri-type'] == 1:
                try:
                    pn = PseudoNode()
                    pn.pn_igp_id = i['node-descriptors']['router-id']+i['node-descriptors']['psn']
                    DeletePseudonode(pn).run()
                except KeyError:
                    mx = Node()
                    mx.igp_router_id = i['node-descriptors']['router-id']
                    DeleteNode(mx).run()
            elif i['ls-nlri-type'] == 2:
                try:
                    link = LinkUpstream()
                    link.node_router_igp_id = i['local-node-descriptors']['router-id']
                    link.psn_router_id = i['remote-node-descriptors']['router-id'] + i['remote-node-descriptors']['psn']
                    DeleteUplink(link).run()
                except KeyError:
                    link = LinkDownstream()
                    link.psn_router_id = i['local-node-descriptors']['router-id'] + i['local-node-descriptors']['psn']
                    link.node_router_igp_id = i['remote-node-descriptors']['router-id']
                    DeleteDownlink(link).run()

    def run(self):
        '''
        Called to evaluate each message consumed from broker. When an update for creating or updating the topology arrives,
        it contains 'announce' in it so the function 'create' is called. On the other hand, when an update indicating a
        link that went down arrives, it contains 'withdraw' in it, so function 'link_down' is called.
        :return:

        '''
        if 'announce' in self.update_message:
            message = json.loads(self.update_message)
            update = message['neighbor']['message']['update']['announce']['bgp-ls bgp-ls'][EXABGP_PEER]
            self.create(update, message)
        elif 'withdraw' in self.update_message:
            logger.info(self.update_message)
            message = json.loads(self.update_message)
            update = message['neighbor']['message']['update']['withdraw']['bgp-ls bgp-ls']
            self.link_down(update)


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
    try:
        handler.update_message = body.decode('utf-8')
        handler.run()
        ch.basic_ack(delivery_tag=method.delivery_tag)
    except:
        ch.basic_nack(delivery_tag=method.delivery_tag)


def control_queue_callback(ch, method, properties, body):
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
    if properties.correlation_id == 'links_data':
        global EXABGP_PEER

        data = json.loads(body.decode('utf-8'))
        if data['is_master']:
            if rmq_collector.queue == '':   # For the first iteration
                rmq_collector.queue = data['queue']
                rmq_collector.routing_key = rmq_collector.queue.replace('queue', 'key')
                EXABGP_PEER = data['bgp_peer']
                rmq_collector.start()
                rmq_collector.setName(HOSTNAME+'_LinkDataConsumer')
                threads_monitor.threads.append(rmq_collector)

            elif rmq_collector.queue != data['queue']:   # When it has to change the queue for consuming
                rmq_collector.queue = data['queue']
                rmq_collector.routing_key = rmq_collector.queue.replace('queue', 'key')
                EXABGP_PEER = data['bgp_peer']
                rmq_collector.stop_consuming()
            else:   # Does nothing if the queue name received is the one already applied
                logger.info('Queue indicated is the current one being consumed')
                pass
    ch.basic_ack(delivery_tag=method.delivery_tag)


if __name__ == '__main__':

    threads = []

    global handler

    handler = Handler()

    rmq_publisher_server = RMQPublisher()
    rmq_publisher_server.host = RMQ_PUBLISHER_HOST
    rmq_publisher_server.user = RMQ_PUBLISHER_USERNAME
    rmq_publisher_server.password = RMQ_PUBLISHER_PASSWORD
    rmq_publisher_server.exchange = RMQ_PUBLISHER_EXCHANGE
    rmq_publisher_server.queue = RMQ_PUBLISHER_SERVER_QUEUE
    rmq_publisher_server.routing_key = RMQ_PUBLISHER_SERVER_ROUTING_KEY
    rmq_publisher_server.start()
    rmq_publisher_server.setName(HOSTNAME+'_LinksQueryPublisherServerQueue')
    threads.append(rmq_publisher_server)

    rmq_publisher_client = RMQPublisher()
    rmq_publisher_client.host = RMQ_PUBLISHER_HOST
    rmq_publisher_client.user = RMQ_PUBLISHER_USERNAME
    rmq_publisher_client.password = RMQ_PUBLISHER_PASSWORD
    rmq_publisher_client.exchange = RMQ_PUBLISHER_EXCHANGE
    rmq_publisher_client.queue = RMQ_PUBLISHER_CLIENT_QUEUE
    rmq_publisher_client.routing_key = RMQ_PUBLISHER_CLIENT_ROUTING_KEY
    rmq_publisher_client.start()
    rmq_publisher_client.setName(HOSTNAME+'_LinksQueryPublisherClientQueue')
    threads.append(rmq_publisher_client)

    rmq_queue_getter = RMQControlConsumer(control_queue_callback)
    rmq_queue_getter.host = RMQ_PUBLISHER_HOST
    rmq_queue_getter.user = RMQ_PUBLISHER_USERNAME
    rmq_queue_getter.password = RMQ_PUBLISHER_PASSWORD
    rmq_queue_getter.exchange = RMQ_CONTROL_EXCHANGE
    rmq_queue_getter.queue = RMQ_CONTROL_QUEUE
    rmq_queue_getter.routing_key = RMQ_CONTROL_KEY
    rmq_queue_getter.start()
    rmq_queue_getter.setName(HOSTNAME+'_ControlConsumer')
    threads.append(rmq_queue_getter)

    rmq_collector = RMQConsumer(on_message_callback)
    rmq_collector.host = RMQ_COLLECTOR_HOST
    rmq_collector.user = RMQ_COLLECTOR_USERNAME
    rmq_collector.password = RMQ_COLLECTOR_PASSWORD
    rmq_collector.exchange = RMQ_COLLECTOR_EXCHANGE

    threads_monitor = ThreadsMonitor(threads)
    threads_monitor.start()

    # Starts exposing prometheus metrics
    start_http_server(60000)
