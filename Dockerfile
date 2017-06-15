FROM ubuntu:trusty

RUN dpkg --add-architecture i386 && \
    apt-get -y update && apt-get -y install wine wget xvfb

WORKDIR /

RUN mkdir -p downloads && \
    mkdir -p /kf2/kf2server/ && \ 
    wget http://media.steampowered.com/installer/steamcmd.zip -P downloads && \
    unzip -o downloads/steamcmd.zip && \
    WINEDEBUG=fixme-all wine steamcmd.exe +exit

RUN WINEDEBUG=fixme-all \
    wine steamcmd.exe \
        +login anonymous \
        +force_install_dir /kf2/kf2server \
        +app_update 232130 validate \
        +exit

RUN wget http://www.redorchestra2.fr/downloads/KF2_WineDLL.zip -P downloads && \
    cd $HOME/.wine/drive_c/windows/system32 && \
    unzip -o $OLDPWD/downloads/KF2_WineDLL.zip && \
    winetricks -q vcrun2010 & sleep 30

ADD kf2_functions.sh /kf2_functions.sh 
ADD containerMain /main
ADD FileCache/kf2server /kf2/kf2server/

# Steam port
EXPOSE 20560/udp

# Query port - used to communicate with the master server
EXPOSE 27015/udp

# Game port - primary comms with players
EXPOSE 7777/udp

# Web Admin port
EXPOSE 8080/tcp

CMD ["/bin/bash", "/main"]
