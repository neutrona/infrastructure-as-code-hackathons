# shift-netconf-broker
NETCONF Async Broker

![NETCONF Broker Architecture](https://github.com/neutrona/infrastructure-as-code-hackathons/blob/master/backend/docker-images/shift-netconf-broker/NETCONF_Broker_Architecture.png)

# Deploy

Check config file first!!!

```
$ cd shift-netconf-broker
$ sudo docker build -t shift-netconf-broker .
$ sudo docker run -ti -p 8646:8646 --name SHIFT-NETCONF-BROKER -d shift-netconf-broker
```

# Stop

```
$ sudo docker attach SHIFT-NETCONF-BROKER
[hit enter key]
```
