FROM mcr.microsoft.com/dotnet/runtime:6.0-bullseye-slim-arm32v7 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# Besoin de Git pour la lib GitInfo
RUN apt-get update && apt-get --no-install-recommends -y install git && apt-get clean && rm -rf /var/lib/apt/lists/*
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["MyComicsManagerApi/MyComicsManagerApi.csproj", "MyComicsManagerApi/"]
COPY ["MyComicsManagerApi/NuGet.config", "MyComicsManagerApi/"]
RUN dotnet restore -r linux-arm "MyComicsManagerApi/MyComicsManagerApi.csproj"

# copy and publish app and libraries
COPY . .
WORKDIR "/src/MyComicsManagerApi"
RUN dotnet build "MyComicsManagerApi.csproj" -c Release -o /app/build -r linux-arm -v d

FROM build AS publish
RUN dotnet publish "MyComicsManagerApi.csproj" -c Release -o /app/publish -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyComicsManagerApi.dll"]