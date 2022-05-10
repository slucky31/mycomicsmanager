FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# Besoin de Git pour la lib GitInfo
RUN apt-get update && apt-get --no-install-recommends -y install git && apt-get clean && rm -rf /var/lib/apt/lists/*
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["MyComicsManagerApi/MyComicsManagerApi.csproj", "MyComicsManagerApi/"]
COPY ["MyComicsManagerApi/NuGet.config", "MyComicsManagerApi/"]

COPY ["MyComicsManagerWeb/MyComicsManagerWeb.csproj", "MyComicsManagerWeb/"]
COPY ["MyComicsManagerWeb/NuGet.config", "MyComicsManagerWeb/"]

RUN dotnet restore "MyComicsManagerApi/MyComicsManagerApi.csproj" -r linux-arm
RUN dotnet restore "MyComicsManagerWeb/MyComicsManagerWeb.csproj" -r linux-arm

# Copy everything else and build
COPY . .
WORKDIR "/src/MyComicsManagerApi"
RUN dotnet build "MyComicsManagerApi.csproj" -c Release -o /app/build -r linux-arm -v d
WORKDIR "/src/MyComicsManagerWeb"
RUN dotnet build "MyComicsManagerWeb.csproj" -c Release -o /app/build -r linux-arm -v d

FROM build AS publish
WORKDIR "/src/MyComicsManagerApi"
RUN dotnet publish "MyComicsManagerApi.csproj" -c Release -o /app/publish -r linux-arm
WORKDIR "/src/MyComicsManagerWeb"
RUN dotnet publish "MyComicsManagerWeb.csproj" -c Release -o /app/publish -r linux-arm

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0-bullseye-slim-arm32v7
WORKDIR /app
RUN ls -la
EXPOSE 5000
EXPOSE 8080
COPY --from=publish /app/publish .
RUN ls -la
ENTRYPOINT ["dotnet", "MyComicsManagerApi.dll"]
ENTRYPOINT ["dotnet", "MyComicsManagerWeb.dll"]