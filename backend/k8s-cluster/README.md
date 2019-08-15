### Getting repo with all the scripts for deploying k8s cluster and tools
```
sudo su
git clone https://github.com/wardviaene/on-prem-or-cloud-agnostic-kubernetes.git
cd on-prem-or-cloud-agnostic-kubernetes/scripts
```
# MASTER:
```
swapoff -a
./install-kubernetes.sh
```
### If ubuntu (or another) user not created:
```
./create-user.sh
```
# NODES:
## Disable swap
```
swapoff -a
./install-node.sh
```
#### Make sure ubuntu (or another) is created

## Label all nodes
```
kubectl label nodes node-01 zone=MIA
kubectl label nodes node-03 zone=MIA
kubectl label nodes node-05 zone=MIA
kubectl label nodes node-02 zone=NYC
kubectl label nodes node-04 zone=NYC
kubectl label nodes node-06 zone=NYC
kubectl label nodes node-08 zone=NYC
kubectl label nodes node-07 zone=MIA
kubectl label nodes node-01 role=worker
kubectl label nodes node-02 role=worker
kubectl label nodes node-03 role=worker
kubectl label nodes node-04 role=worker
kubectl label nodes node-05 mq=True
kubectl label nodes node-06 mq=True
kubectl label nodes node-07 mq=True
kubectl label nodes node-08 mq=True
kubectl label nodes node-05 db=True
kubectl label nodes node-07 db=True
kubectl label nodes node-08 db=True
kubectl label nodes node-06 db=True
kubectl label nodes node-05 storage=True
kubectl label nodes node-07 storage=True
kubectl label nodes node-08 storage=True
kubectl label nodes node-06 storage=True
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
# Dashboard UI (optional)
```
kubectl create -f https://raw.githubusercontent.com/kubernetes/dashboard/master/src/deploy/recommended/kubernetes-dashboard.yaml

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

kubectl create -f dashboard-admin.yaml
sudo nohup kubectl proxy --address="10.255.45.111" -p 443 --accept-hosts='^*$' &
http://10.255.45.111:443/api/v1/namespaces/kube-system/services/https:kubernetes-dashboard:/proxy/

# NOTE: 10.255.45.111 is the master's IP address
```

# Helm (k8s package manager)
## Install
```
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

# ISTIO
## Download and install
```
wget https://github.com/istio/istio/releases/download/1.0.4/istio-1.0.4-linux.tar.gz
tar -xzvf istio-1.0.4-linux.tar.gz
cd istio-1.0.4
echo 'export PATH="$PATH:/home/ubuntu/istio-1.0.4/bin"' >> ~/.profile
rm ../istio-1.0.4-linux.tar.gz 
```
## Apply CRDs:
```
kubectl apply -f ~/istio-1.0.4/install/kubernetes/helm/istio/templates/crds.yaml
```
### With no mutual TLS:
```
kubectl apply -f ~/istio-1.0.4/install/kubernetes/istio-demo.yaml
```
------------------------------------------------------------------
### Hello world app 
```
export PATH="$PATH:/home/ubuntu/istio-1.0.2/bin"
kubectl apply -f <(istioctl kube-inject -f helloworld.yaml)
kubectl apply -f helloworld-gw.yaml
```
------------------------------------------------------------------

# ROOK
```
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
## Create file storage class (here you can define how many replicas for each dataset)
```
kubectl create -f rook-storageclass-fs.yaml 
```
## Deploy rook-tools (useful for troubleshooting and getting status of rook cluster)
```
kubectl create -f rook-tools.yaml 
```
### Checking rook cluster status
``` 
kubectl exec -ti rook-tools -n rook -- bash
> rookctl status
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
### On master
```
docker login https://gitlab:4567 -u docker-swarm -p password

cat ~/.docker/config.json
```
## Create namespace
```
kubectl create -f namespace.yaml
```
## Create secret in cluster that holds authorization token
```
kubectl create secret docker-registry regcred --docker-server=gitlab:4567 --docker-username=docker-swarm --docker-password=password --namespace=nm2
```
## Exposing Grafana (change ClusterIP by NodePort (for example))
```
kubectl edit svc grafana -n istio-system
```
# To login into nm2-toolbox
```
kubectl exec -ti toolbox-0 -n nm2 -- bash
```