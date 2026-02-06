# Shipment Events & Label Service

Backend servis za upravljanje pošiljkama koji omogućava:
- kreiranje pošiljaka putem REST API-ja
- evidenciju statusnih događaja (events)
- upload label dokumenata u Azure Blob Storage
- asinhronu obradu labela korišćenjem Azure Service Bus queue-a

Rešenje je implementirano u ASP.NET Core (.NET 8) uz clean architecture pristup.

---

## Tehnologije

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- Azure.Storage.Blobs
- Azure.Messaging.ServiceBus
- Swagger / OpenAPI
- ILogger (Serilog)
- MediatR (CQRS)
- Dependency Injection
---

## Arhitektura

Rešenje je organizovano po principima **Clean Architecture** i sastoji se od sledećih slojeva:

- **API layer**
  - Controllers
  - Exception Middleware
- **Application layer**
  - CQRS pattern (Commands/Queries)
  - Repository Interfaces
  - Custom Exceptions
  - Models (DTOs, Result pattern) 
  - Validators (FluentValidation)
  - Poslovna logika i servisi (ShipmentService)
- **Domain layer**
  - Entiteti: Shipment, ShipmentEvent, ShipmentDocument
  - Enum: ShipmentState
- **Infrastructure layer**
  - EF Core DbContext
  - Implementacija Azure servisa
  - Implementacija repozitorijuma

Rešenje sadrži dva projekta:
- **ShipmentService.Api** – REST API
- **ShipmentService.Worker** – Background servis za obradu labela

---

## Model podataka

### Shipment
- Id (GUID)
- ReferenceNumber (string, unique)
- SenderName (string)
- RecipientName (string)
- State (Created, LabelUploaded, LabelProcessed, Failed)
- CreatedAt
- UpdatedAt

### ShipmentEvent
- Id (GUID)
- ShipmentId (GUID)
- EventCode (CREATED, LABEL_UPLOADED, LABEL_PROCESSED, FAILED)
- EventTime
- Payload (opciono)
- CorrelationId

### ShipmentDocument
- Id (GUID)
- ShipmentId (GUID)
- BlobName
- ContentType
- SizeBytes
- UploadedAt

---

## Konfiguracija

Konfiguracija se vrši putem `appsettings.json` fajla u oba projekta.

Primer konfiguracije:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<db-connection-string>"
  },
  "Azure": {
    "ServiceBus": {
      "ConnectionString": "<service-bus-connection-string>",
      "QueueName": "labels-to-process"
    },
    "BlobStorage": {
      "ConnectionString": "<blob-storage-connection-string>",
      "ContainerName": "shipment-labels"
    }
  }
}
```
---

## Pokretanje aplikacije

### Pokretanje API-ja

```bash
cd ShipmentService.Api
dotnet restore
dotnet run
```

API je dostupan na:
- http://localhost:5252
- Swagger UI: http://localhost:5252/swagger

### Pokretanje Worker servisa

```bash
cd ShipmentService.Worker
dotnet run
```

Worker servis sluša Service Bus queue `labels-to-process` i obrađuje poruke asinhrono.

---

## API Endpoint-i

### POST /api/shipments
Kreira novu pošiljku.

**Request body:**
```json
{
  "referenceNumber": "REF-001",
  "senderName": "John Doe",
  "recipientName": "Jane Doe"
}
```
**Response**
```json
{
  "isSuccess": true,
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "string",
  "errors": [
    "string"
  ]
}
```

Validacije:
- ReferenceNumber mora biti jedinstven
- Obavezna polja ne smeju biti prazna

---

### GET /api/shipments
Vraća listu pošiljaka.

Podržava:
- paginaciju (`page`, `pageSize`)
- filter po stanju (`state`)

**Response**
```json
{
    "isSuccess": true,
    "value": {
        "items": [
            {
                "referenceNumber": "ref2",
                "senderName": "Deki",
                "recipientName": "Slavica",
                "state": "Created",
                "createdAt": "2026-02-06T15:08:15.5525312"
            },
            {
                "referenceNumber": "ref1",
                "senderName": "Petar",
                "recipientName": "Milan",
                "state": "Created",
                "createdAt": "2026-02-04T12:23:40.8363222"
            },
            {
                "referenceNumber": "ref0",
                "senderName": "Voja",
                "recipientName": "Stefan",
                "state": "Created",
                "createdAt": "2026-02-03T22:31:15.465632"
            }
        ],
        "totalCount": 3,
        "page": 1,
        "pageSize": 10
    },
    "message": "Shipments retrieved successfully.",
    "errors": null
}
```

---

### GET /api/shipments/{id}
Vraća detalje pošiljke, trenutno stanje i listu događaja.

**Response**
```json
{
    "isSuccess": true,
    "value": {
        "id": "a97338e2-8d33-457e-9413-12590b3fb4d6",
        "referenceNumber": "ref1",
        "senderName": "Petar",
        "recipientName": "Milan",
        "state": "Created",
        "createdAt": "2026-02-04T12:23:40.8363222",
        "updatedAt": "2026-02-04T12:23:40.8363571",
        "lastStatus": {
            "eventCode": "CREATED",
            "eventTime": "2026-02-04T12:23:40.8365881",
            "payload": null,
            "correlationId": "a4532443-c243-46cd-abcc-719b955efdd0"
        },
        "shipmentEvents": [
            {
                "eventCode": "CREATED",
                "eventTime": "2026-02-04T12:23:40.8365881",
                "payload": null,
                "correlationId": "a4532443-c243-46cd-abcc-719b955efdd0"
            }
        ]
    },
    "message": "Shipment details retrieved successfully.",
    "errors": null
}
```

---

### GET /api/shipments/{id}/events
Vraća događaje za izabranu pošiljku sortirane(ascending) po vremenu.

**Response**
```json
{
  "isSuccess": true,
  "value": [
      {
          "eventCode": "CREATED",
          "eventTime": "2026-02-04T12:23:40.8365881",
          "payload": null,
          "correlationId": "a4532443-c243-46cd-abcc-719b955efdd0"
      }
  ],
  "message": "Shipment events retrieved successfully.",
  "errors": null
}
```

---

### POST /api/shipments/{id}/label
Upload label dokumenta (PDF ili slika).

Tok obrade:
- Upload fajla u Azure Blob Storage
- Kreiranje ShipmentDocument zapisa
- Upis događaja `LABEL_UPLOADED`
- Ažuriranje stanja pošiljke na `LabelUploaded`
- Slanje poruke na Service Bus queue

---

## Background / Worker servis

Worker servis sluša poruke sa Service Bus queue-a i za svaku poruku:

- učitava ShipmentId, BlobName i CorrelationId
- preuzima fajl iz Blob Storage-a
- idempotency
- vrši simuliranu obradu (provera veličine fajla + `Task.Delay`)
- upisuje događaj `LABEL_PROCESSED`
- ažurira stanje pošiljke na `LabelProcessed`

U slučaju greške:
- upisuje se događaj `FAILED` sa razlogom
- stanje pošiljke se postavlja na `Failed`
- retry pristup
- dead-letter-queue

---

## Error handling i idempotency

- Upravljanje greškama u Api projektu implementira se pomoću exception middleware i četiri vrste custom exception-a(+ default).
- Ukoliko dodje do greške npr u azure servisima, greška će se hvatati kroz try-catch blokove i prosledjivati u nivo iznad sve dok stigne na obradu u odgovarajućem custom exception middleware-u.
- U worker projektu se obradjuju azure tip greške i sve ostale neočekivane greške kroz retry pristup.

- Worker servis je idempotentan:
  - Pre same obrade i skidanja fajla proverava se da li za konkretnu pošiljku postoji event koji ima eventCode LABEL_PROCESSED.
  - Na osnovu toga se donosi odluka da li je poruka već bila obradjena ili je tek treba obraditi.

---

## Logovanje

Kroz ceo tok obrade koristi se `CorrelationId` koji:
- se generiše prilikom upload-a labela
- prosleđuje se kroz Service Bus poruku
- koristi se u API i Worker logovima

---

## Zaključak

Ovaj projekat demonstrira:
- dizajn REST API-ja
- asinhronu obradu korišćenjem message queue-a
- rad sa Azure SDK bibliotekama
- clean architecture pristup

