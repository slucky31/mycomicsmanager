#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# A tester : https://gist.github.com/ptupitsyn/1c0f4d6186f63c0f09689745242ebee5

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
ARG TARGETARCH
WORKDIR /app
EXPOSE 5000

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
RUN dotnet restore "./WebAPI/./WebAPI.csproj" -a $TARGETARCH
COPY WebAPI/. .
RUN dotnet publish "./WebAPI.csproj" -c "$BUILD_CONFIGURATION" -a $TARGETARCH -o /app /p:UseAppHost=false

# Rootless user : https://devblogs.microsoft.com/dotnet/securing-containers-with-rootless/
FROM base AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WebAPI.dll"]
