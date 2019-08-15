FROM ubuntu:16.04

WORKDIR /opt/
RUN apt update \
    && apt install -y python3 nginx=1.10.3-0ubuntu0.16.04.3 nano vim net-tools curl python3-pip

ADD requirements .
ADD lb-api.py .
#ADD db-lb.conf /etc/nginx/sites-enabled/
#ADD bolt.conf .
ADD entrypoint.sh .

#RUN cat /opt/bolt.conf >> /etc/nginx/nginx.conf
RUN rm /etc/nginx/sites-enabled/default
RUN pip3 install --no-cache-dir -r requirements

ENTRYPOINT ["/opt/entrypoint.sh"]

CMD ["python3", "lb-api.py"]


