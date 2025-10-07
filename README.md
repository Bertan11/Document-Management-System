# Document Management System

Dieses Projekt ist ein einfaches Dokumentenverwaltungssystem, das mit **.NET 8** umgesetzt wurde.  
Es bietet eine REST API zum Anlegen, Abrufen, Aktualisieren und Löschen von Dokumenten. Zusätzlich wird RabbitMQ verwendet, um beim Hochladen von Dokumenten Nachrichten zu versenden.

## Projektstruktur

- **Controllers/**  
  Enthält den `DocumentController`, welcher die REST API-Endpunkte bereitstellt.
  
- **Models/**  
  Beinhaltet die Datenmodelle, z. B. `Document`.

- **Repositories/**  
  Schnittstelle und Implementierung für den Zugriff auf die Daten (z. B. `DocumentRepository`).

- **Services/**  
  Geschäftslogik (`DocumentService`) und RabbitMQ-Anbindung (`RabbitMqService`).

- **Exceptions/**  
  Enthält benutzerdefinierte Exception-Klassen wie `DocumentNotFoundException` oder `DocumentValidationException`.

- **Tests/**  
  Projekt mit Unit Tests (xUnit + Moq). Getestet wurden Controller, Services und zentrale Logik.

## Voraussetzungen

- .NET 8 SDK  
- Docker (für RabbitMQ und Datenbank)  
- Visual Studio oder Visual Studio Code  

## Installation & Start

1. Repository klonen  
   ```bash
   git clone <repo-url>
   cd DocumentManagementSystem
   ```

2. Abhängigkeiten wiederherstellen  
   ```bash
   dotnet restore
   ```

3. Docker-Container starten (RabbitMQ und ggf. Datenbank)  
   ```bash
   docker-compose up -d
   ```

4. Projekt starten  
   ```bash
   dotnet run --project DocumentManagementSystem
   ```

Die API ist anschließend unter `http://localhost:5000/api/document` erreichbar.

## API Endpunkte

- `GET /api/document` – alle Dokumente abrufen  
- `GET /api/document/{id}` – Dokument nach ID abrufen  
- `POST /api/document` – neues Dokument erstellen  
- `PUT /api/document/{id}` – Dokument aktualisieren  
- `DELETE /api/document/{id}` – Dokument löschen  
- `POST /api/document/upload` – Datei hochladen  

## Tests & Coverage

Tests wurden mit **xUnit** und **Moq** geschrieben.  
Ausgeführt werden können sie mit:  

```bash
dotnet test
```

Für die Coverage-Auswertung wurde coverlet und reportgenerator genutzt.  
Der Coverage Report befindet sich unter:  

```
DocumentManagementSystem.Tests/coveragereport/index.html
```

## Stand der Umsetzung

- REST API vollständig implementiert  
- Services und Repositories vorhanden  
- RabbitMQ integriert  
- Unit Tests geschrieben (ca. 18 Stück, Abdeckung ~24 %)  
- Coverage Report generiert  
