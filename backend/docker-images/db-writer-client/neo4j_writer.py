#!/usr/bin/python3

import logging
from neo4j.v1 import GraphDatabase
import threading
from neo4j.exceptions import ServiceUnavailable
import time

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class Neo4jDB(threading.Thread):
    neo4j_host = ''
    neo4j_username = ''
    neo4j_password = ''

    def __init__(self):
        threading.Thread.__init__(self)

    def run(self):
        try:
            self.neo4jDB = GraphDatabase.driver(self.neo4j_host, auth=(self.neo4j_username, self.neo4j_password))
            # Create index for topology_id
            with self.neo4jDB.session() as session:
                with session.begin_transaction() as tx:
                    tx.run("CREATE INDEX ON :Node(topology_id)")
                    tx.run("CREATE INDEX ON :Node(IPv4_Router_Identifier)")
                    tx.run("CREATE INDEX ON :LSP(Symbolic_Path_Name)")
        except ServiceUnavailable:  # Raised when DB is down. It waits 5 seconds and try to connect again
            logger.info("Connection to DB is down. Trying again in 5 seconds")
            time.sleep(5)
            self.run()

    def update(self, query):
        try:
            with self.neo4jDB.session() as session:
                with session.begin_transaction() as tx:
                    tx.run(query)
            logger.info('Sent to DB: \n %s  \n Thread: %s' % (query, threading.current_thread()))
        except:
            logger.info("Connection to DB is down. Trying again in 5 seconds")
            time.sleep(5)
            self.update(query)
            pass
