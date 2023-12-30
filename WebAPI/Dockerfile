FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers
COPY Domain/Domain.csproj .
COPY Application/Application.csproj .
COPY Persistence/Persistence.csproj .
COPY WebAPI/WebAPI.csproj .
COPY Directory.Packages.props .
RUN dotnet restore -a $TARGETARCH "WebAPI.csproj"

# copy everything else and build app
COPY . .
RUN dotnet publish -a $TARGETARCH -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy
EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["./WebAPI"]
