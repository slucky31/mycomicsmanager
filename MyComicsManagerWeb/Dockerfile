FROM mcr.microsoft.com/dotnet/runtime:6.0-bullseye-slim-arm32v7 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Besoin de Git pour la lib GitInfo
RUN apt-get update && apt-get --no-install-recommends -y install git && apt-get clean && rm -rf /var/lib/apt/lists/*
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["MyComicsManagerWeb/MyComicsManagerWeb.csproj", "MyComicsManagerWeb/"]
COPY ["MyComicsManagerWeb/NuGet.config", "MyComicsManagerWeb/"]
RUN dotnet restore -r linux-arm "MyComicsManagerWeb/MyComicsManagerWeb.csproj"

# copy and publish app and libraries
COPY . .
WORKDIR /src/MyComicsManagerWeb
RUN dotnet build "MyComicsManagerWeb.csproj" -c Release -o /app/build -r linux-arm -v d

FROM build AS publish
RUN dotnet publish "MyComicsManagerWeb.csproj" -c Release -o /app/publish -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyComicsManagerWeb.dll"]