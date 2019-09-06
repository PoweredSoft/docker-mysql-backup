FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY PoweredSoft.Docker.MysqlBackup.csproj ./
RUN dotnet restore
COPY . .
WORKDIR /src
RUN dotnet build PoweredSoft.Docker.MysqlBackup.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish PoweredSoft.Docker.MysqlBackup.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "PoweredSoft.Docker.MysqlBackup.dll"]
