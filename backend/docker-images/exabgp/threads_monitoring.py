#!/usr/bin/python3

from prometheus_client import Gauge
import logging
import threading
import time

LOG_FORMAT = ('%(levelname) -10s %(asctime)s %(name) -30s %(funcName) '
              '-35s %(lineno) -5d: %(message)s')
logging.basicConfig(level=logging.INFO, format=LOG_FORMAT)
logger = logging.getLogger(__name__)


class ThreadsMonitor(threading.Thread):
    '''
    Constantly monitors all of running threads
    '''

    def __init__(self, threads):
        threading.Thread.__init__(self)
        self.threads = threads

    def run(self):
        g = Gauge('Threads_monitoring', '1 if UP 0 if DOWN', ['thread_name'])

        while True:
            for i in self.threads:
                if i.is_alive():
                    g.labels({'thread_name': i.getName()}).set(1)
                else:
                    g.labels({'thread_name': i.getName()}).set(0)
            time.sleep(1)