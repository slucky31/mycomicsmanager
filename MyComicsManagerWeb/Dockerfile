FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim-arm32v7 AS base
WORKDIR /app
EXPOSE 7000

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["MyComicsManagerWeb.csproj", "./"]
RUN dotnet restore -r linux-arm "./MyComicsManagerWeb.csproj"

# copy and publish app and libraries
COPY . .
RUN dotnet build "MyComicsManagerWeb.csproj" -c Release -o /app/build -r linux-arm

FROM build AS publish
RUN dotnet publish "MyComicsManagerWeb.csproj" -c Release -o /app/publish -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyComicsManagerWeb.dll"]