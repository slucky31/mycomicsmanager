FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.401-noble AS build
ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers
COPY MyComicsManager.slnx .
COPY Domain/Domain.csproj ./Domain/
COPY Application/Application.csproj ./Application/
COPY Persistence/Persistence.csproj ./Persistence/
COPY Web/Web.csproj ./Web/
COPY Directory.Packages.props .
RUN dotnet restore "./Web/Web.csproj" -a $TARGETARCH

# copy everything else and build app
COPY . .
RUN dotnet publish "./Web/Web.csproj" -a $TARGETARCH -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1
USER $APP_UID
ENTRYPOINT ["./Web"]

