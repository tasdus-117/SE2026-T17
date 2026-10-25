FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ArenaServer.csproj", "."]
RUN dotnet restore "ArenaServer.csproj"

COPY . .
RUN dotnet build "ArenaServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ArenaServer.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "ArenaServer.dll"]
