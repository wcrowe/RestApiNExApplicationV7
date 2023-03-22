
*************************
Instructions  to run solution (api, Blazor web and database) in Docker as containers 
*************************

1)
install latest Docker desktop

2)
run Docker desktop app before running Docker compose script and running containers 

3)
create RestApinEx solution and wait all nugest packages get restored 


4) run ps1 to set the right docker project location - fix for Docker relative files issue
right click  ../RestApiNExApiV6\RestApiNExApiV6.Api\fixDockerConfiguration.ps1
and -Run with Powershell-

5)
in Visual Studio PM console run these scripts to get required Docker images:
PM>docker pull mcr.microsoft.com/mssql/server:2022-latest
PM>docker pull mcr.microsoft.com/dotnet/aspnet:6.0

6)
set single startup project docker-compose and run the solution
it will create new database db and seed it in Docker container

Note:
To run unit tests against docker database, update {your-solution}.Test config file appsettings.json "ConnectionStrings" to use docker settings - Server=db,1433

YouTube instructions:
https://youtu.be/SqCyvIghRak


-------------------------
PM console Docker scripts
-------------------------

--get .NET sdk
PM> docker pull mcr.microsoft.com/dotnet/core/sdk

--get version
PM> docker-compose --version
Docker Compose version v2.10.2

--list images 
PM> docker images
--remove image with id
PM>docker image rm e8146cd5633e
--list of images ids
PM> docker image ls -q

--list containers 
PM> docker ps
--list of container ids
PM> docker container ls -q
--remove all containers -- stop andf remove all
PM> docker container rm -f $(docker container ls -aq)

--remove all image -- stop and remove all
PM> docker image rm -f $(docker image ls -aq)


--help commands
PM>docker compose
--help arguments for build
PM>docker compose build --help

--build from scratch
PM>docker compose build --no-cache

--build and start container
PM>docker compose up --build

--stops and removes all containers but images ok 
PM>docker compose down

--network story
PM>docker network ls

PM>docker exec -it -u root cont-id sh
/app # ping api
/app # ifconfig

PM>docker inspect bridge

--remove network(s)
PM>  docker network rm net1 net2 net3

--remove unused
PM>docker network prune
----------------------------------------------
https://docs.docker.com/engine/reference/builder/

----------------------------------------------
https://mcr.microsoft.com/en-us/product/dotnet/aspnet/about
images base
7.0 (Standard Support)
PM>docker pull mcr.microsoft.com/dotnet/aspnet:7.0
6.0 (Long-Term Support)
PM>docker pull mcr.microsoft.com/dotnet/aspnet:6.0


https://mcr.microsoft.com/en-us/product/powershell/about
images
Ubuntu for Linux and Windows Server Core for Windows
PM>docker pull mcr.microsoft.com/powershell or docker pull mcr.microsoft.com/powershell:latest

https://mcr.microsoft.com/en-us/product/windows/about
PM>ltsc2019 (LTSC) docker pull mcr.microsoft.com/windows:ltsc2019
windows/nanoserver: Nano Server base OS container image
windows/servercore: Windows Server Core base OS container image
windows/server: Server base OS container image
windows/insider: Insider version of this base OS image.

https://mcr.microsoft.com/en-us/product/windows-cssc/python3.7.2server/about
Server base image with Python 3.7.2
ltsc2022 (LTSC)
docker pull mcr.microsoft.com/windows-cssc/python3.7.2server:ltsc2022
