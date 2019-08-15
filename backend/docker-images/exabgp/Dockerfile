FROM alpine:3.7

WORKDIR /opt/

RUN apk --no-cache add wget curl python python-dev py-setuptools coreutils netcat-openbsd bash \
    && apk --no-cache add --virtual build-dependencies build-base py-pip  \
    && mkdir -p /opt/exabgp \
    && pip install ipaddr exabgp ipy requests \
    && apk del build-dependencies \
    && apk add python3 

ADD exabgp.conf /opt/exabgp/
ADD entrypoint.sh /opt/exabgp/
ADD exabgp_to_rabbitmq.py /opt/exabgp/
ADD mastership_handler.py /opt/exabgp/
ADD rmq_fanout_publisher.py /opt/exabgp/
ADD rmq_publisher.py /opt/exabgp/
ADD threads_monitoring.py /opt/exabgp/
ADD requirements /opt/exabgp/

RUN pip3 install --no-cache-dir -r /opt/exabgp/requirements

ENTRYPOINT ["/opt/exabgp/entrypoint.sh"]

EXPOSE 179
CMD ["exabgp"]
