FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim-arm32v7 AS base
WORKDIR /app
EXPOSE 7000

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
RUN apt-get update
RUN apt-get -y install git
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ["MyComicsManagerWeb.csproj", "./"]
RUN ls -la
RUN dotnet restore -r linux-arm "./MyComicsManagerWeb.csproj"

# copy and publish app and libraries
COPY . .
RUN ls -la
RUN dotnet build "MyComicsManagerWeb.csproj" -c Release -o /app/build -r linux-arm -v d
RUN ls -la obj/Debug/net5.0
RUN more obj/Debug/net5.0/ThisAssembly.GitInfo.g.cs

FROM build AS publish
RUN dotnet publish "MyComicsManagerWeb.csproj" -c Release -o /app/publish -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyComicsManagerWeb.dll"]