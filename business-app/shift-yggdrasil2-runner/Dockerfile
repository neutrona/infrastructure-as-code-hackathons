FROM mono:latest

ADD ./yggdrasil2 /usr/src/app/yggdrasil2
ADD ./shift-yggdrasil2-runner /usr/src/app/shift-yggdrasil2-runner

RUN nuget restore /usr/src/app/yggdrasil2/packages.config -PackagesDirectory /usr/src/app/packages && \
    nuget restore /usr/src/app/shift-yggdrasil2-runner/packages.config -PackagesDirectory /usr/src/app/packages && \
    xbuild /property:Configuration=Release /property:OutDir=/usr/src/app/build/ /usr/src/app/shift-yggdrasil2-runner/shift-yggdrasil2-runner.csproj

CMD mono /usr/src/app/build/shift-yggdrasil2-runner.exe

