# Build-Phase
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiere nur die csproj-Datei und restore Dependencies
COPY *.csproj ./
RUN dotnet restore

# Kopiere den Rest und baue
COPY . .
RUN dotnet publish -c Release -o /app

# Runtime-Phase
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "OcrWorker.dll"]
