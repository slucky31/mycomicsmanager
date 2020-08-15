FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app
EXPOSE 80

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore -r linux-arm

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out -r linux-arm

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-arm32v7
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "MyComicsManagerApi.dll"]