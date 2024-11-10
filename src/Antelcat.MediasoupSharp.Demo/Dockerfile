FROM ubuntu:22.04 as build

RUN sed -i s@/archive.ubuntu.com/@/mirrors.aliyun.com/@g /etc/apt/sources.list
RUN apt-get clean
RUN apt-get update

RUN apt-get install -y build-essential
RUN apt-get install -y python3
RUN apt-get install -y python3-pip
RUN apt-get install -y curl
RUN apt-get install -y git
RUN curl -fsSL https://deb.nodesource.com/setup_18.x -o nodesource_setup.sh
RUN bash nodesource_setup.sh
RUN apt-get install -y nodejs
RUN apt-get clean

WORKDIR /build

COPY package.json package.json

RUN npm install

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY . .
COPY --from=build /build/node_modules/mediasoup/worker/out/Release/mediasoup-worker ./runtimes/linux-x64/native/

EXPOSE 80

ENTRYPOINT ["dotnet", "Antelcat.MediasoupSharp.Demo.dll"]
