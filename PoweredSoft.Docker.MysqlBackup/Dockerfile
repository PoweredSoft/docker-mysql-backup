FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine3.11 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /src
COPY PoweredSoft.Docker.MysqlBackup.csproj ./
RUN dotnet restore
COPY . .
WORKDIR /src
RUN dotnet build PoweredSoft.Docker.MysqlBackup.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
RUN apk update
RUN apk add --no-cache bash
RUN apk add --no-cache mysql-client
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PoweredSoft.Docker.MysqlBackup.dll"]