FROM ubuntu:xenial

RUN apt-get update && apt-get install -y apt-transport-https ca-certificates

RUN sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ xenial main" > /etc/apt/sources.list.d/dotnetdev.list'

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 417A0893

RUN apt-get update && apt-get -y install wget lib32gcc1 dotnet-dev-1.0.4

RUN useradd -m steam

WORKDIR /home/steam
USER steam

ADD kf2_functions.sh kf2_functions.sh 
ADD containerMain main 
ADD KF2Srv/bin/Debug/netcoreapp1.0/ ConfigEdit

# Steam port
EXPOSE 20560/udp

# Query port - used to communicate with the master server
EXPOSE 27015/udp

# Game port - primary comms with players
EXPOSE 7777/udp

# Web Admin port
EXPOSE 8080/tcp

CMD ["/bin/bash", "main"]

