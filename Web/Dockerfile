FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0.305-noble AS build
ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers
COPY MyComicsManager.sln .
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
FROM mcr.microsoft.com/dotnet/aspnet:9.0.9-noble

EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["./Web"]

