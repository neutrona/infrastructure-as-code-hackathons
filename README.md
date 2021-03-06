# infrastructure-as-code-hackathons
Infrastructure as Code &amp; Software Defined Networking Hackathons - [Wiki](https://github.com/neutrona/infrastructure-as-code-hackathons/wiki)

The backend collects network information: network topology, traffic engineering attributes, performance, etc.
We used __BGP-LS, PCEP & NETCONF__ to collect such information.

The business application exposes abstracted network information to an IDE (C#), an operator can code custom 
business rules for path computation, testing, and validation. This code is then pushed to a git repository and
a CI/CD pipeline is triggered, three stages are run (build, test, deploy) to finally configuring those paths
in the network through Ansible and maintaining them through an SDN controller.

Some tools and platforms we used:

- Docker
- Kubernetes
- Gitlab CI/CD
- Ansible
- Ansible Tower
- ExaBGP
- Neo4J
- RabbitMQ
- OpenDayLight
- Prometheus
- Grafana
- Juniper JunOS

Some things we didn't do:

- Clean code
- Runners monitoring strategy, ELK maybe?
- O(n) ==> O(1)
- CI/CD (backend)
- Maybe [Dgraph](https://dgraph.io/) instead of Neo4J?
- ODL monitoring
- Use branches for lab/production from the business app
- ...
