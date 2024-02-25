FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0.201-jammy AS build
ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers
COPY MyComicsManager.sln .
COPY Domain/Domain.csproj ./Domain/
COPY Application/Application.csproj ./Application/
COPY Persistence/Persistence.csproj ./Persistence/
COPY WebAPI/WebAPI.csproj ./WebAPI/
COPY Directory.Packages.props .
RUN dotnet restore "./WebAPI/WebAPI.csproj" -a $TARGETARCH

# copy everything else and build app
COPY . .
RUN dotnet publish "./WebAPI/WebAPI.csproj" -a $TARGETARCH -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0.2-jammy
EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["./WebAPI"]
