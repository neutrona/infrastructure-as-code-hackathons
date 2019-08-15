FROM mono:latest

RUN apt-get update && apt-get install -y python3 python3-pip supervisor git

ADD ./yggdrasil2 /usr/src/app/yggdrasil2
ADD ./shift-yggdrasil2-scheduler /usr/src/app/shift-yggdrasil2-scheduler
ADD ./shift-yggdrasil2-scheduler/supervisord.conf /etc/supervisor/conf.d/supervisord.conf

RUN nuget restore /usr/src/app/yggdrasil2/packages.config -PackagesDirectory /usr/src/app/packages && \
    nuget restore /usr/src/app/shift-yggdrasil2-scheduler/packages.config -PackagesDirectory /usr/src/app/packages && \
    msbuild /property:Configuration=Release /property:OutDir=/usr/src/app/build/ /usr/src/app/shift-yggdrasil2-scheduler/shift-yggdrasil2-scheduler.csproj

RUN pip3 install -r /usr/src/app/shift-yggdrasil2-scheduler/requirements.txt

ENTRYPOINT ["/usr/bin/supervisord"]
