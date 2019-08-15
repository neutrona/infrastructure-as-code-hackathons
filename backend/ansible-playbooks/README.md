#### Before running any playbook:
```
1) Create an ssh key pair and copy the public one to all of the k8s member nodes. 
2) Modify file vars.yaml accordingly
```

### Deploy k8s cluster
```
ansible-playbook -i inventory -e sshkey=[SSH_KEY_DIR_PATH] k8s-cluster-deployment/main.yaml 
```

#### Before deploying nm2 services, edit ../monitoring/manifest-all.yaml (line 3136) by setting the haproxy vIP address accordingly


### Deploy k8s cluster
```
ansible-playbook -i inventory -e sshkey=[SSH_KEY_DIR_PATH] nm2-deployment/main.yaml 
```