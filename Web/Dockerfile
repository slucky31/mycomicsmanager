FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0.401-jammy AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:8.0.8-jammy
EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
USER $APP_UID

# Install the agent
RUN apt-get update && apt-get install -y wget ca-certificates gnupg \
&& echo 'deb http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
&& wget https://download.newrelic.com/548C16BF.gpg \
&& apt-key add 548C16BF.gpg \
&& apt-get update \
&& apt-get install -y 'newrelic-dotnet-agent' \
&& rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["./Web"]
