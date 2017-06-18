FROM ubuntu:xenial

RUN apt-get -y update && apt-get -y install wget lib32gcc1 xfonts-scalable

RUN useradd -m steam

WORKDIR /home/steam
USER steam

ADD https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz downloads

RUN downloads/steamcmd.sh +exit || true

RUN mkdir -p kf2server && \
    downloads/steamcmd.sh \
        +login anonymous \
        +force_install_dir ./kf2server \
        +app_update 232130 validate \
        +exit

RUN ls Steam
RUN ls downloads


#ADD kf2_functions.sh kf2_functions.sh 
ADD containerMain main

#ADD FileCache/DefaultConfig/ ./kf2server/KFGame/Config/

# Steam port
#EXPOSE 20560/udp

# Query port - used to communicate with the master server
#EXPOSE 27015/udp

# Game port - primary comms with players
#EXPOSE 7777/udp

# Web Admin port
#EXPOSE 8080/tcp

CMD ["/bin/bash", "main"]
