#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 44341
#EXPOSE 57875
ENV ASPNETCORE_URLS=http://+:44341

#debug mode
#FROM base as debug
#RUN tdnf install procps-ng -y
#

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["RestApiNExApplication/RestApiNExApplication.Api/RestApiNExApplication.Api.csproj", "RestApiNExApplication/RestApiNExApplication.Api/"]
COPY ["RestApiNExApplication/RestApiNExApplication.Entity/RestApiNExApplication.Entity.csproj", "RestApiNExApplication/RestApiNExApplication.Entity/"]
COPY ["RestApiNExApplication/RestApiNExApplication.Domain/RestApiNExApplication.Domain.csproj", "RestApiNExApplication/RestApiNExApplication.Domain/"]
RUN dotnet restore "RestApiNExApplication/RestApiNExApplication.Api/RestApiNExApplication.Api.csproj"
COPY . .
WORKDIR "/src/RestApiNExApplication/RestApiNExApplication.Api"
RUN dotnet build "RestApiNExApplication.Api.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "RestApiNExApplication.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RestApiNExApplication.Api.dll"]