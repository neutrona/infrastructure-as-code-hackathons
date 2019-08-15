# Environment variables
## Exabgp conf
<pre>
BGP_PEER='192.168.0.2'
RID='10.255.45.67'
LOCAL_IP='192.168.0.1'
LOCAL_AS=64512
PEER_AS=64513
</pre>
## Exabgp_to_rabbitmq process
<pre>
RMQ_HOST=rabbitmq
RMQ_PORT=5672
RMQ_USERNAME=guest
RMQ_PASSWORD=guest
RMQ_EXCHANGE=shift_topology_exchange
RMQ_QUEUE=shift_links_topology_queue_1
RMQ_ROUTING_KEY=shift_links_topology_key_1
EXABGP_PROM_PORT=60000
</pre>
## Mastership_handler
<pre>
LOCAL_SERVER_BINDING_ADDRESS=0.0.0.0
LOCAL_SERVER_BINDING_PORT=54321
REMOTE_SERVER_PORT=54321
SPEAKER_PREFERENCE=150
ATTACHED_DATA_QUEUE=shift_links_topology_queue_1
ATTACHED_DATA_KEY=shift_links_topology_key_1
REMOTE_SERVER_ADDRESS=192.168.0.3
BGP_PEER=BGP_PEER
RMQ_HOST=RMQ_HOST
RMQ_PORT=RMQ_PORT
RMQ_USERNAME=RMQ_USERNAME
RMQ_PASSWORD=RMQ_PASSWORD
RMQ_CONTROL_EXCHANGE=shift_control_exchange
RMQ_CONTROL_QUEUE=shift_control_queue
RMQ_CONTROL_ROUTING_KEY=shift_control_key
MASTERSHIP_PROM_PORT=60001
</pre>


```
sudo docker run -ti --rm --name exabgp -p 179:179 --cap-add=NET_ADMIN --net=host -e BGP_PEER=192.168.0.2 -e RID=192.168.0.1 -e LOCAL_IP=192.168.0.1 -e LOCAL_AS=64512  -e PEER_AS=64513 -e RMQ_HOST=rabbitmq -e RMQ_USERNAME=admin -e RMQ_PASSWORD=password -e RMQ_EXCHANGE=shift_topology_exchange -e RMQ_QUEUE=shift_links_topology_queue_1 -e RMQ_ROUTING_KEY=shift_links_topology_key_1 -e EXABGP_PROM_PORT=60000 -e LOCAL_SERVER_BINDING_ADDRESS=0.0.0.0 -e LOCAL_SERVER_BINDING_PORT=54321 -e REMOTE_SERVER_PORT=54321 -e SPEAKER_PREFERENCE=150 -e ATTACHED_DATA_QUEUE=shift_links_topology_queue_1 -e ATTACHED_DATA_KEY=shift_links_topology_key_1 -e REMOTE_SERVER_ADDRESS=exabgp-slave -e RMQ_CONTROL_EXCHANGE=shift_control_exchange -e RMQ_CONTROL_QUEUE=shift_control_queue -e RMQ_CONTROL_ROUTING_KEY=shift_control_key -e MASTERSHIP_PROM_PORT=60001 gitlab:4567/nm2/exabgp
```
