Ref.: https://blog.inkubate.io/install-and-configure-a-multi-master-kubernetes-cluster-with-kubeadm/

# Install cfssl (ONLY ON HAPROXY-LB)
## Download binaries, add exec permissions and move binaries to /usr/local/bin
```
wget https://pkg.cfssl.org/R1.2/cfssl_linux-amd64
wget https://pkg.cfssl.org/R1.2/cfssljson_linux-amd64
chmod +x cfssl*
sudo mv cfssl_linux-amd64 /usr/local/bin/cfssl
sudo mv cfssljson_linux-amd64 /usr/local/bin/cfssljson
```
## Verify installation
```
cfssl version
```
# HAProxy (ONLY ON HAPROXY-LB)
```
sudo su
```
## Add repo
```
add-apt-repository ppa:vbernat/haproxy-1.7
```
## Install HAProxy
```
apt update && apt install -y haproxy
```
## Add your configuration
```
cat >> /etc/haproxy/haproxy.cfg << EOF

frontend kubernetes
 	bind 10.255.32.200:6443
 	option tcplog
 	mode tcp
 	default_backend kubernetes-master-nodes


backend kubernetes-master-nodes
	mode tcp
	balance roundrobin
	option tcp-check
	server master-01 10.255.32.204:6443 check fall 3 rise 2
	server master-02 10.255.32.205:6443 check fall 3 rise 2
	server master-03 10.255.32.206:6443 check fall 3 rise 2

EOF
```
## Restart HAProxy
```
sudo systemctl restart haproxy
```
## Create certificate authority
### Create cert authority config file
```
cat > ca-config.json << EOF
{
  "signing": {
    "default": {
      "expiry": "8760h"
    },
    "profiles": {
      "kubernetes": {
        "usages": ["signing", "key encipherment", "server auth", "client auth"],
        "expiry": "8760h"
      }
    }
  }
}

EOF
```
### Create cert authority signing request config file
```
cat > ca-csr.json << EOF
 {
  "CN": "Kubernetes",
  "key": {
    "algo": "rsa",
    "size": 2048
  },
  "names": [
  {
    "C": "IE",
    "L": "Cork",
    "O": "Kubernetes",
    "OU": "CA",
    "ST": "Cork Co."
  }
 ]
}

EOF
```
### Generate the certificate authority certificate and private key
```
cfssl gencert -initca ca-csr.json | cfssljson -bare ca
```
### Verify that the ca-key.pem and the ca.pem were generated
```
ls -lha
```
## Create certificate for the etcd cluster

### Create cert siging request config file
```
cat > kubernetes-csr.json << EOF
{
  "CN": "kubernetes",
  "key": {
    "algo": "rsa",
    "size": 2048
  },
  "names": [
  {
    "C": "IE",
    "L": "Cork",
    "O": "Kubernetes",
    "OU": "Kubernetes",
    "ST": "Cork Co."
  }
 ]
}

EOF
```
###  Generate the certificate and private key
```
cfssl gencert \
-ca=ca.pem \
-ca-key=ca-key.pem \
-config=ca-config.json \
-hostname=10.255.32.204,10.255.32.205,10.255.32.206,10.255.32.200,127.0.0.1,kubernetes.default \
-profile=kubernetes kubernetes-csr.json | \
cfssljson -bare kubernetes
```
###  Copy the certificate to each nodes
```
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.32.204:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.32.205:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.32.206:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.45.101:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.45.102:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.45.103:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.45.104:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.45.105:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.45.106:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.45.107:~
scp ca.pem kubernetes.pem kubernetes-key.pem ubuntu@10.255.45.108:~
```
# Docker (ALL MASTERS AND NODES)
## Add repository key
```
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | apt-key add -
```
## Add repository
```
add-apt-repository \
"deb https://download.docker.com/linux/$(. /etc/os-release; echo "$ID") \
$(lsb_release -cs) \
stable"
```
## Install docker 17.03
```
apt update && apt-get install -y docker-ce=$(apt-cache madison docker-ce | grep 17.03 | head -1 | awk '{print $3}')
```
# Kubelet, kubeadm and kubectl (ALL MASTERS AND NODES)
## Add repository key
```
curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key add -
```
## Add repository
```
cat >> /etc/apt/sources.list.d/kubernetes.list << EOF
deb http://apt.kubernetes.io kubernetes-xenial main
EOF
```
## Install kubelet, kubeadm and kubectl
```
apt update && apt-get install kubelet kubeadm kubectl -y
```
## Disable swap
```
swapoff -a; sed -i '/ swap / s/^/#/' /etc/fstab
```
# Etcd (ONLY ON MASTER NODES)(Make sure you are in user home directory as non-root user)
## Create a configuration directory for Etcd
```
sudo mkdir /etc/etcd /var/lib/etcd
```
## Move certs to the config directory
```
sudo mv ~/ca.pem ~/kubernetes.pem ~/kubernetes-key.pem /etc/etcd
```
## Etcd binaries
```
wget https://github.com/coreos/etcd/releases/download/v3.3.9/etcd-v3.3.9-linux-amd64.tar.gz 
tar xvzf etcd-v3.3.9-linux-amd64.tar.gz
sudo mv etcd-v3.3.9-linux-amd64/etcd* /usr/local/bin/
sudo rm etcd-v3.3.9-linux-amd64.tar.gz
```
## Create an etcd systemd unit file
### On Master-01
```
sudo su

cat > /etc/systemd/system/etcd.service << EOF
[Unit]
Description=etcd
Documentation=https://github.com/coreos


[Service]
ExecStart=/usr/local/bin/etcd \
  --name 10.255.32.204 \
  --cert-file=/etc/etcd/kubernetes.pem \
  --key-file=/etc/etcd/kubernetes-key.pem \
  --peer-cert-file=/etc/etcd/kubernetes.pem \
  --peer-key-file=/etc/etcd/kubernetes-key.pem \
  --trusted-ca-file=/etc/etcd/ca.pem \
  --peer-trusted-ca-file=/etc/etcd/ca.pem \
  --peer-client-cert-auth \
  --client-cert-auth \
  --initial-advertise-peer-urls https://10.255.32.204:2380 \
  --listen-peer-urls https://10.255.32.204:2380 \
  --listen-client-urls https://10.255.32.204:2379,http://127.0.0.1:2379 \
  --advertise-client-urls https://10.255.32.204:2379 \
  --initial-cluster-token etcd-cluster-0 \
  --initial-cluster 10.255.32.204=https://10.255.32.204:2380,10.255.32.205=https://10.255.32.205:2380,10.255.32.206=https://10.255.32.206:2380 \
  --initial-cluster-state new \
  --data-dir=/var/lib/etcd
Restart=on-failure
RestartSec=5



[Install]
WantedBy=multi-user.target

EOF
```
### On Master-02
```
sudo su

cat > /etc/systemd/system/etcd.service << EOF
[Unit]
Description=etcd
Documentation=https://github.com/coreos


[Service]
ExecStart=/usr/local/bin/etcd \
  --name 10.255.32.205 \
  --cert-file=/etc/etcd/kubernetes.pem \
  --key-file=/etc/etcd/kubernetes-key.pem \
  --peer-cert-file=/etc/etcd/kubernetes.pem \
  --peer-key-file=/etc/etcd/kubernetes-key.pem \
  --trusted-ca-file=/etc/etcd/ca.pem \
  --peer-trusted-ca-file=/etc/etcd/ca.pem \
  --peer-client-cert-auth \
  --client-cert-auth \
  --initial-advertise-peer-urls https://10.255.32.205:2380 \
  --listen-peer-urls https://10.255.32.205:2380 \
  --listen-client-urls https://10.255.32.205:2379,http://127.0.0.1:2379 \
  --advertise-client-urls https://10.255.32.205:2379 \
  --initial-cluster-token etcd-cluster-0 \
  --initial-cluster 10.255.32.204=https://10.255.32.204:2380,10.255.32.205=https://10.255.32.205:2380,10.255.32.206=https://10.255.32.206:2380 \
  --initial-cluster-state new \
  --data-dir=/var/lib/etcd
Restart=on-failure
RestartSec=5



[Install]
WantedBy=multi-user.target

EOF
```
### On Master-03
```
sudo su

cat > /etc/systemd/system/etcd.service << EOF
[Unit]
Description=etcd
Documentation=https://github.com/coreos


[Service]
ExecStart=/usr/local/bin/etcd \
  --name 10.255.32.206 \
  --cert-file=/etc/etcd/kubernetes.pem \
  --key-file=/etc/etcd/kubernetes-key.pem \
  --peer-cert-file=/etc/etcd/kubernetes.pem \
  --peer-key-file=/etc/etcd/kubernetes-key.pem \
  --trusted-ca-file=/etc/etcd/ca.pem \
  --peer-trusted-ca-file=/etc/etcd/ca.pem \
  --peer-client-cert-auth \
  --client-cert-auth \
  --initial-advertise-peer-urls https://10.255.32.206:2380 \
  --listen-peer-urls https://10.255.32.206:2380 \
  --listen-client-urls https://10.255.32.206:2379,http://127.0.0.1:2379 \
  --advertise-client-urls https://10.255.32.206:2379 \
  --initial-cluster-token etcd-cluster-0 \
  --initial-cluster 10.255.32.204=https://10.255.32.204:2380,10.255.32.205=https://10.255.32.205:2380,10.255.32.206=https://10.255.32.206:2380 \
  --initial-cluster-state new \
  --data-dir=/var/lib/etcd
Restart=on-failure
RestartSec=5



[Install]
WantedBy=multi-user.target

EOF
```
## Reload daemon configuration, enable etcd to start at boot time and start etcd
```
systemctl daemon-reload
systemctl enable etcd
systemctl start etcd
```
## Verify that the cluster's up and running
```
ETCDCTL_API=3 etcdctl member list
```
# Initializing master nodes (as non-root user)
##  Create the config file for kubeadm
```
cat > config.yaml << EOF
apiVersion: kubeadm.k8s.io/v1alpha3
kind: ClusterConfiguration
kubernetesVersion: stable
apiServerCertSANs:
- 10.255.32.200
controlPlaneEndpoint: "10.255.32.200:6443"
etcd:
  external:
    endpoints:
    - https://10.255.32.204:2379
    - https://10.255.32.205:2379
    - https://10.255.32.206:2379
    caFile: /etc/etcd/ca.pem
    certFile: /etc/etcd/kubernetes.pem
    keyFile: /etc/etcd/kubernetes-key.pem
networking:
  podSubnet: 10.244.0.0/16
apiServerExtraArgs:
  apiserver-count: "3"

EOF
```
## Initialize machine as master node (ONLY ON MASTER-01)
```
sudo kubeadm init --config=config.yaml
```
## Copy the certs to the two other masters
```
sudo scp -r /etc/kubernetes/pki ubuntu@10.255.32.205:~
sudo scp -r /etc/kubernetes/pki ubuntu@10.255.32.206:~
```
## Remove apiserver.crt and apiserver.key (ONLY ON MASTER-02 and MASTER-03 as non-root user)
```
rm ~/pki/apiserver.*
```
## Move the certificates to the /etc/kubernetes directory
```
sudo mv ~/pki /etc/kubernetes/
```
## Initialize machine as master node (ONLY ON MASTER-02 and MASTER-03)
```
sudo kubeadm init --config=config.yaml
```
# Initializing worker nodes (as non-root user. PARAMETERS WILL CHANGE FOR EACH DEPLOYMENT)
```
sudo kubeadm join 10.255.32.200:6443 --token 8ojra0.f5vjg9sod8114toy --discovery-token-ca-cert-hash sha256:0de3b2069a656d6f29aa7c9c66676580529eb074c679b7265bc0fea9af0b442d
```
# Verifying the workers joined to the cluster
```
sudo kubectl --kubeconfig /etc/kubernetes/admin.conf get nodes
```
The status of the nodes is NotReady as we haven't configured the networking overlay yet.



# Configuring kubectl on the client machine (it could be one of the master ndoes)
## Add permissions to the admin.conf file
```
sudo chmod +r /etc/kubernetes/admin.conf
```
## From the client machine, copy the configuration file
```
scp ubuntu@10.255.32.204:/etc/kubernetes/admin.conf .
```
## Create kubectl config dir
```
mkdir ~/.kube
```
## Move the config file to the config directory
```
sudo mv admin.conf ~/.kube/config
sudo chmod 600 ~/.kube/config
```
 DO NOT CHANGE THE PERMISSIONS BACK
 ## Change back the permissions of the config file
 ```
 sudo chmod 600 /etc/kubernetes/admin.conf
 ```
## Verify
```
kubectl get nodes
```
# Deploying the overlay network (on client machine/master-01)
## Using weavenet as overlay network
```
kubectl apply -f https://git.io/weave-kube-1.6
```

<pre>
	DO NOT USE CALICO WITH THIS METHODOLOGY
## Deploy CNI calico
```
export KUBECONFIG=/etc/kubernetes/admin.conf
kubectl apply -f https://docs.projectcalico.org/v3.1/getting-started/kubernetes/installation/hosted/canal/rbac.yaml
kubectl apply -f https://docs.projectcalico.org/v3.1/getting-started/kubernetes/installation/hosted/canal/canal.yaml
```
</pre>
## Verify
```
kubectl get nodes
```

# Logrotate
## Add desired config
```
sudo su
cat >> /etc/logrotate.conf << EOF

/var/lib/docker/containers/**/*-json.log {
    size 10M
    rotate 5
}

EOF
```
## Run logrotate
```
logrotate /etc/logrotate.conf
```
# Dashboard UI (optional)
## Deploy dashboard
```
kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v1.10.0/src/deploy/recommended/kubernetes-dashboard.yaml
```
## Create dashboard rbac
```
cat > dashboard-admin.yaml <<EOF

apiVersion: rbac.authorization.k8s.io/v1beta1
kind: ClusterRoleBinding
metadata:
 name: kubernetes-dashboard
 labels:
   k8s-app: kubernetes-dashboard
roleRef:
 apiGroup: rbac.authorization.k8s.io
 kind: ClusterRole
 name: cluster-admin
subjects:
- kind: ServiceAccount
  name: kubernetes-dashboard
  namespace: kube-system

EOF
```
## Deploy dashboard rbac
```
kubectl create -f dashboard-admin.yaml
```
## Run proxy with nohup
```
sudo nohup kubectl proxy --address="10.255.32.204" -p 443 --accept-hosts='^*$' &
```
## Access the K8s Dashboard at:
http://10.255.32.204:443/api/v1/namespaces/kube-system/services/https:kubernetes-dashboard:/proxy/

NOTE: 10.255.32.204 is the master's IP address




# Helm (k8s package manager)(ON ONE MASTER NODE ONLY)
## Install
```
git clone https://github.com/wardviaene/kubernetes-course.git
cd kubernetes-course/helm/
```
```
wget https://storage.googleapis.com/kubernetes-helm/helm-v2.11.0-linux-amd64.tar.gz
tar -xzvf helm-v2.11.0-linux-amd64.tar.gz
sudo mv linux-amd64/helm /usr/local/bin/helm
```
## Initialize helm
```
kubectl create -f helm-rbac.yaml
helm init --service-account tiller
```


# ROOK
```
git clone https://github.com/wardviaene/on-prem-or-cloud-agnostic-kubernetes.git
cd on-prem-or-cloud-agnostic-kubernetes/rook
```
## Create operator
```
kubectl create -f rook-operator.yaml
```
## Create cluster (within this file you can define affinity for those storage nodes)
```
kubectl create -f rook-cluster.yaml
```
## Create block storage class (here you can define how many replicas for each dataset)
```
kubectl create -f rook-storageclass.yaml
```
<pre>
## Create file storage class (here you can define how many replicas for each dataset)
```
kubectl create -f rook-storageclass-fs.yaml 
```
</pre>
## Deploy rook-tools (useful for troubleshooting and getting status of rook cluster)
```
kubectl create -f rook-tools.yaml 
```
### Checking rook cluster status
``` 
kubectl exec -ti rook-tools -n rook -- rookctl status
```


# Private registry
## Login
### On every master/node
```
sudo su
echo '{"insecure-registries" : [ "gitlab:4567" ], "log-driver": "json-file", "log-opts": {"max-size": "10m","max-file": "10" }}' >> /etc/docker/daemon.json
systemctl daemon-reload
systemctl restart docker
```
### Login into gitlab (ONLY ON ONE MASTER NODE)
```
docker login https://gitlab:4567 -u docker-swarm -p password
cat ~/.docker/config.json
exit
```
## Create namespace
```
kubectl create namespace nm2
```
## Create secret in cluster that holds authorization token
```
kubectl create secret docker-registry regcred --docker-server=gitlab:4567 --docker-username=docker-swarm --docker-password=password --namespace=nm2
```
# To login into nm2-toolbox
```
kubectl exec -ti toolbox-0 -n nm2 -- bash
```

# To force delete
```
kubectl delete pod NAME --grace-period=0 --force
```