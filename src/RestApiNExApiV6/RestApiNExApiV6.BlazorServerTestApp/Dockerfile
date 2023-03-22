#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RestApiNExApiV6/RestApiNExApiV6.BlazorServerTestApp/RestApiNExApiV6.BlazorServerTestApp.csproj", "RestApiNExApiV6/RestApiNExApiV6.BlazorServerTestApp/"]
RUN dotnet restore "RestApiNExApiV6/RestApiNExApiV6.BlazorServerTestApp/RestApiNExApiV6.BlazorServerTestApp.csproj"
COPY . .
WORKDIR "/src/RestApiNExApiV6/RestApiNExApiV6.BlazorServerTestApp"
RUN dotnet build "RestApiNExApiV6.BlazorServerTestApp.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "RestApiNExApiV6.BlazorServerTestApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RestApiNExApiV6.BlazorServerTestApp.dll"]