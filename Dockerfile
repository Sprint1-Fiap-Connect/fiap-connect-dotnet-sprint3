FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish src/FiapConnect.API/FiapConnect.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

CMD ASPNETCORE_URLS=http://*:$PORT dotnet FiapConnect.API.dll
