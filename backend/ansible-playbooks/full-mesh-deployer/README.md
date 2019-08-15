### Deploy
```
export NETCONF_BROKER_API_HOST=netconf_broker_host && \
export NETCONF_BROKER_API_USERNAME=shift_ro && \
export NETCONF_BROKER_API_PASSWORD=password && \
../../full-mesh-lsps/inventory_getter.py && \
ansible-playbook --extra-vars "@extra_vars.json" deploy-lsp.yml
```

