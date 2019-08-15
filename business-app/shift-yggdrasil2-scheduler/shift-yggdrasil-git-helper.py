#!/usr/bin/python3

import logging
from flask import Flask, request
from os import getenv
from flask_api import status
import os
from git import Repo
import git
import re


logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Getting environent variables. If None, then a default one is assigned

if getenv('API_BINDING_ADDRESS'):
    API_BINDING_ADDRESS = getenv('API_BINDING_ADDRESS')
else:
    API_BINDING_ADDRESS = '0.0.0.0'

if getenv('API_BINDING_PORT'):
    API_BINDING_PORT = getenv('API_BINDING_PORT')
else:
    API_BINDING_PORT = 6666

if getenv('REPO_ROOT_DIRECTORY'):
    REPO_ROOT_DIRECTORY = getenv('REPO_ROOT_DIRECTORY')
else:
    REPO_ROOT_DIRECTORY = '/var/shift/scheduler/'


app = Flask(__name__)


@app.route("/repository/update", methods=['POST'])
def update():
    if not request.json or not 'repository_url' in request.json:  # Si no es un json o el json no tiene ci_repository_url no continuo
        logging.error('Request does not have repository_url key or invalid payload')
        return status.HTTP_406_NOT_ACCEPTABLE
    else:
        try:
            ci_repository_url = request.json['repository_url']
            if '.git' not in ci_repository_url:
                return '', status.HTTP_400_BAD_REQUEST
            else:
                regex = re.search(r'(.*@)(.*)', ci_repository_url)
                ci_repository_path = (REPO_ROOT_DIRECTORY + regex.group(2)).strip('.git')
                try:
                    os.makedirs(ci_repository_path)
                except FileExistsError:
                    logger.info("Directory already exists")
                    pass
                try:
                    logger.info('\n%s -- %s', ci_repository_url, ci_repository_path)
                    Repo.clone_from(ci_repository_url, ci_repository_path, env={'GIT_SSL_NO_VERIFY': '1'})
                    return '', status.HTTP_200_OK

                except git.exc.GitCommandError as ee:
                    if 'already exists and is not an empty directory' in str(ee):
                        logger.info("%s\nRepo already exists. Pulling..."%str(ee))

                        try:
                            g = git.Git(ci_repository_path)
                            g.pull('origin', 'master', env={'GIT_SSL_NO_VERIFY': '1'})
                            return '', status.HTTP_200_OK

                        except git.exc.GitCommandError as ee:
                            logger.info('SOMETHING WENT WRONG!!!\n%s' %str(ee))
                            return '', status.HTTP_500_INTERNAL_SERVER_ERROR
                    else:
                        logger.info('SOMETHING WENT WRONG!!!\n%s' %str(ee))
                        return '', status.HTTP_500_INTERNAL_SERVER_ERROR

        except Exception as ee:
            logger.info('SOMETHING WENT WRONG!!!\n%s' %str(ee))
            return '', status.HTTP_500_INTERNAL_SERVER_ERROR
            pass


if __name__ == '__main__':
    app.run(host=API_BINDING_ADDRESS, port=API_BINDING_PORT, debug=True)
