FROM mono:latest
EXPOSE 8646
ADD . /usr/src/app/
#RUN nuget config -Set HTTP_PROXY=http://10.255.45.50:3128/
RUN nuget restore /usr/src/app/packages.config -PackagesDirectory /usr/src/app/packages 

CMD xbuild /property:Configuration=Release /property:OutDir=/usr/src/app/build/ /usr/src/app/shift-netconf-broker.csproj && mono /usr/src/app/build/shift-netconf-broker.exe

