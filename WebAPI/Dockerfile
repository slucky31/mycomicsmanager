#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# A tester : https://gist.github.com/ptupitsyn/1c0f4d6186f63c0f09689745242ebee5

FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy-arm64v8 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
RUN dotnet restore -r linux-arm "WebAPI/WebAPI.csproj"

# copy and publish app and libraries
COPY . .
WORKDIR /src/WebAPI
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build -r linux-arm -v d

FROM build AS publish
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAPI.dll"]
