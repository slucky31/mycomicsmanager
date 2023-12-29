#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
# https://github.com/dotnet/dotnet-docker/blob/main/samples/aspnetapp/Dockerfile.chiseled

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy-arm64v8 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY WebAPI/*.csproj .
RUN dotnet restore

# copy everything else and build app
COPY WebAPI/. .
RUN dotnet publish --no-restore -o /app


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled-arm64v8
EXPOSE 5000
WORKDIR /app
USER app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WebAPI.dll"]
