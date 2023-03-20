#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["RestApiNExApplication/RestApiNExApplication.BlazorServerTestApp/RestApiNExApplication.BlazorServerTestApp.csproj", "RestApiNExApplication/RestApiNExApplication.BlazorServerTestApp/"]
RUN dotnet restore "RestApiNExApplication/RestApiNExApplication.BlazorServerTestApp/RestApiNExApplication.BlazorServerTestApp.csproj"
COPY . .
WORKDIR "/src/RestApiNExApplication/RestApiNExApplication.BlazorServerTestApp"
RUN dotnet build "RestApiNExApplication.BlazorServerTestApp.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "RestApiNExApplication.BlazorServerTestApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RestApiNExApplication.BlazorServerTestApp.dll"]