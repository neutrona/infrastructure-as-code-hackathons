


# Build and push Scheduler image

``` git pull && sudo docker build -t davidneutrona/shift-yggdrasil2-scheduler -f shift-yggdrasil2-scheduler/Dockerfile . && sudo docker push davidneutrona/shift-yggdrasil2-scheduler ```

# Build and push Runner image

``` git pull && sudo docker build -t davidneutrona/shift-yggdrasil2-runner -f shift-yggdrasil2-runner/Dockerfile . && sudo docker push davidneutrona/shift-yggdrasil2-runner ```
