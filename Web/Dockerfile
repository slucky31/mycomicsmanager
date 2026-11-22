# Images pinned by digest: Renovate opens a PR each time Microsoft rebuilds them (#1357).
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:11.0.100-resolute@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea AS build
ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers
COPY MyComicsManager.slnx .
COPY Domain/Domain.csproj ./Domain/
COPY Application/Application.csproj ./Application/
COPY Persistence/Persistence.csproj ./Persistence/
COPY Web/Web.csproj ./Web/
COPY Directory.Packages.props .
# The NuGet packages (~1.2 GB) and their download cache stay in BuildKit cache mounts, out of the
# layer: kept in the CI layer cache, they took longer to download back (~70 s) than to restore.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=nuget-http,target=/root/.local/share/NuGet/http-cache \
    dotnet restore "./Web/Web.csproj" -r linux-musl-$TARGETARCH

# copy everything else and build app
COPY . .
# The analyzers already ran in the CI build and SonarCloud: skipping them here leaves the
# binaries unchanged (source generators still run) and saves about a quarter of the compile.
# No --no-restore: if the restore layer came from the cache while the cache mount is empty,
# this restores the packages again.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=nuget-http,target=/root/.local/share/NuGet/http-cache \
    dotnet publish "./Web/Web.csproj" -r linux-musl-$TARGETARCH -o /app -p:RunAnalyzers=false

# final stage/image
# Alpine (musl) halves the base image compared with Ubuntu (#1284). The -extra variant adds ICU and
# tzdata, so culture-aware formatting and time zones work as on Ubuntu (no invariant globalization).
FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-alpine3.24-extra@sha256:cd3d2caabadeef985cafe1cedd48f80357c2c72887ac3e74d48a1e70011277c9

# apk upgrade applies the Alpine security fixes released since the base image was built (#1357).
# tesseract-ocr reads the ISBN printed on the pages of the imported books.
RUN apk upgrade --no-cache \
    && apk add --no-cache tesseract-ocr tesseract-ocr-data-eng

EXPOSE 8080
WORKDIR /app
COPY --from=build /app .
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD wget -q -O /dev/null http://localhost:8080/health/live || exit 1
USER $APP_UID
ENTRYPOINT ["./Web"]
