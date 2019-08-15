import pika
import logging
import threading

LOG_FORMAT = ('%(levelname) -10s %(asctime)s %(name) -30s %(funcName) '
              '-35s %(lineno) -5d: %(message)s')
logging.basicConfig(level=logging.INFO, format=LOG_FORMAT)
LOGGER = logging.getLogger(__name__)


class RMQConsumer(threading.Thread):
    host = ''
    user = ''
    password = ''
    exchange = ''
    queue = ''
    routing_key = ''

    def __init__(self, on_message_callback):
        threading.Thread.__init__(self)
        self.on_message_callback = on_message_callback

    def run(self):
        '''
        Establishes connection to rabbitmq and starts consuming
        :return:
        '''
        self.credentials = pika.PlainCredentials(self.user, self.password)
        self.connection = pika.BlockingConnection(pika.ConnectionParameters(host=self.host,
                                                                            credentials=self.credentials,
                                                                            heartbeat_interval=0))
        self.channel = self.connection.channel()
        self.channel.basic_qos(prefetch_count=1)
        self.channel.queue_declare(queue=self.queue, arguments={"x-ha-policy": "all", 'x-message-ttl': 60000})
        self.channel.queue_bind(queue=self.queue, exchange=self.exchange, routing_key=self.routing_key)
        # self.channel.basic_consume(self.on_message_callback, queue=self.queue, no_ack=True)
        self.channel.basic_consume(self.on_message_callback, queue=self.queue)
        self.channel.start_consuming()

