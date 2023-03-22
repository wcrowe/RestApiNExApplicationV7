#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 44341
#EXPOSE 57875
ENV ASPNETCORE_URLS=http://+:44341

#debug mode
#FROM base as debug
#RUN tdnf install procps-ng -y
#

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RestApiNExApiV6/RestApiNExApiV6.Api/RestApiNExApiV6.Api.csproj", "RestApiNExApiV6/RestApiNExApiV6.Api/"]
COPY ["RestApiNExApiV6/RestApiNExApiV6.Entity/RestApiNExApiV6.Entity.csproj", "RestApiNExApiV6/RestApiNExApiV6.Entity/"]
COPY ["RestApiNExApiV6/RestApiNExApiV6.Domain/RestApiNExApiV6.Domain.csproj", "RestApiNExApiV6/RestApiNExApiV6.Domain/"]
RUN dotnet restore "RestApiNExApiV6/RestApiNExApiV6.Api/RestApiNExApiV6.Api.csproj"
COPY . .
WORKDIR "/src/RestApiNExApiV6/RestApiNExApiV6.Api"
RUN dotnet build "RestApiNExApiV6.Api.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "RestApiNExApiV6.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RestApiNExApiV6.Api.dll"]