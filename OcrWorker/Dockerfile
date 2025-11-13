FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0


RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-eng \
        tesseract-ocr-deu \
        poppler-utils && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /out ./


ENV OCR__Lang="eng+deu" \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "OcrWorker.dll"]
