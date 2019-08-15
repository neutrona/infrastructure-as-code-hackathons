#!/usr/bin/python3

import requests
import logging
import json
import re
import os
from os import getenv
from requests.auth import HTTPBasicAuth


LOG_FORMAT = ('%(levelname) -10s %(asctime)s %(name) -30s %(funcName) '
              '-35s %(lineno) -5d: %(message)s')
logging.basicConfig(level=logging.INFO, format=LOG_FORMAT)
logger = logging.getLogger(__name__)


if getenv('NETCONF_BROKER_API_HOST'):
    NETCONF_BROKER_API_HOST = getenv('NETCONF_BROKER_API_HOST')
else:
    NETCONF_BROKER_API_HOST = '127.0.0.1'

if getenv('NETCONF_BROKER_API_USERNAME'):
    NETCONF_BROKER_API_USERNAME = getenv('NETCONF_BROKER_API_USERNAME')
else:
    NETCONF_BROKER_API_USERNAME = 'admin'

if getenv('NETCONF_BROKER_API_PASSWORD'):
    NETCONF_BROKER_API_PASSWORD = getenv('NETCONF_BROKER_API_PASSWORD')
else:
    NETCONF_BROKER_API_PASSWORD = 'admin'


def main(pe, lo_reachable, secs):
    api_url = 'http://'+NETCONF_BROKER_API_HOST+':8646/Inventory'
    api_user = NETCONF_BROKER_API_USERNAME
    api_password = NETCONF_BROKER_API_PASSWORD

    try:
        payload = {'limit': '0'}
        response = requests.get(url=api_url, auth=HTTPBasicAuth(username=api_user, password=api_password), params=payload)
        content = response.json()

        data = {}
        extra_vars = {}

        for i in content:  # iterates among all of nodes and sends rpc-requests
            if i['Node_Name'] == pe:
                logger.info('Node Name: %s' % i['Node_Name'])
                if not lo_reachable:
                    regex = re.search(r'^(\d*).(\d*).\d*.(\d*)$', i['IPv4_Router_Identifier'])
                    data['hosts'] = regex.group(1) + '.' + regex.group(2) + '.' + '50' + '.' + regex.group(3)
                else:
                    data['hosts'] = i['IPv4_Router_Identifier']
                extra_vars['SOURCE_HOST_MGMT'] = data['hosts']
                logger.info('SOURCE_HOST_MGMT: %s' % extra_vars['SOURCE_HOST_MGMT'])
                extra_vars['LABEL_SWITCHED_PATHS'] = []
                source_name = i['Node_Name']
                source_host = i['IPv4_Router_Identifier']

                for j in content:
                    if j['Node_Name'] != 'USAMIATER1JVM1' and i['Node_Name'] != j['Node_Name'] and 'vmx' not in j['Node_Name'].lower():
                        eq = {}
                        eq['SOURCE_NAME'] = source_name
                        eq['SOURCE_HOST'] = source_host
                        eq['TARGET_NAME'] = j['Node_Name']
                        eq['TARGET_HOST'] = j['IPv4_Router_Identifier']
                        eq['PRIMARY_PATH'] = {}
                        eq['PRIMARY_PATH']['EXTENDED_ADMIN_GROUPS'] = []
                        eq['SECONDARY_PATHS'] = []
                        for k in range(secs):
                            eag = {}
                            eag['EXTENDED_ADMIN_GROUPS'] = []
                            eq['SECONDARY_PATHS'].append(eag)

                        extra_vars['LABEL_SWITCHED_PATHS'].append(eq)
        data['extra_vars'] = extra_vars
        with open('extra_vars.json', 'w') as f:
            json.dump(extra_vars, f)

    except requests.exceptions.ConnectionError:     # Exception handling when netconf broker is not working
        logger.info('Received connection refused! Trying again later')
        pass

if __name__ == '__main__':
    pe = input('Please enter the ingres PE name\n')
    lr = input('Will the lo0.100 be reachable from where the playbook will be executed? (Y/N)\n')
    secs = int(input('How many secondary paths do you want to provision for each LSP?\n'))
    lo_reachable = False
    if lr.lower() == 'y':
        lo_reachable = True
    main(pe, lo_reachable, secs)
