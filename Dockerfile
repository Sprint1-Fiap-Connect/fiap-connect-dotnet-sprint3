FROM ://microsoft.com AS build
WORKDIR /src

COPY . .

RUN dotnet publish src/FiapConnect.API/FiapConnect.API.csproj -c Release -o /app/publish

FROM ://microsoft.com AS final
WORKDIR /app

COPY --from=build /app/publish .

# Linha corrigida para o Railway interpretar a porta
CMD ASPNETCORE_URLS=http://*:$PORT dotnet FiapConnect.API.dll
