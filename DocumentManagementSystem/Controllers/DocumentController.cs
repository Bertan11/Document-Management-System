using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services; // IEventBus, IObjectStorage, UploadMessage
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocumentManagementSystem.Exceptions;

namespace DocumentManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _service;
        private readonly IEventBus _eventBus;
        private readonly IObjectStorage _storage;
        private readonly IConfiguration _cfg;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentService service,
            IEventBus eventBus,
            IConfiguration cfg,
            ILogger<DocumentController> logger,
            IObjectStorage storage)
        {
            _service = service;
            _eventBus = eventBus;
            _cfg = cfg;
            _logger = logger;
            _storage = storage;
        }

        // GET: api/document
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var documents = await _service.GetAllAsync();
            return Ok(documents);
        }

        // GET: api/document/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var document = await _service.GetByIdAsync(id);
            if (document == null)
                throw new DocumentNotFoundException($"Dokument mit ID {id} nicht gefunden");

            return Ok(document);
        }

        // POST: api/document  (JSON)
        // Speichert in DB, lädt Content als .txt nach MinIO und published Event
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Document document)
        {
            if (document == null)
                throw new DocumentValidationException("Dokument ist null.");

            // 1) in DB speichern
            var created = await _service.AddAsync(document);

            // 2) nach MinIO hochladen (als .txt)
            var bucket = _cfg["Minio:Bucket"] ?? "documents";
            var objectName = $"{created.Id}.txt";
            var contentType = "text/plain";

            await using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(created.Content ?? string.Empty)))
            {
                await _storage.UploadAsync(bucket, objectName, ms, contentType);
            }

            // 3) Event veröffentlichen
            var msg = new UploadMessage(created.Id, created.Title, bucket, objectName, DateTime.UtcNow);
            _eventBus.Publish(
                _cfg["RabbitMq:Exchange"] ?? _cfg["Rabbit:Exchange"] ?? "dms.events",
                _cfg["RabbitMq:RoutingUpload"] ?? _cfg["Rabbit:RoutingUpload"] ?? "document.uploaded",
                msg
            );

            _logger.LogInformation("Dokument {DocId} erstellt, nach MinIO hochgeladen und Event veröffentlicht.", created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/document/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Document document)
        {
            if (id != document.Id)
                throw new DocumentValidationException("Update fehlgeschlagen: ID stimmt nicht mit Dokument überein");

            var updated = await _service.UpdateAsync(document);
            if (updated == null)
                throw new DocumentNotFoundException($"Dokument mit ID {id} nicht gefunden");

            _logger.LogInformation("Dokument {DocId} wurde aktualisiert.", id);
            return Ok(updated);
        }

        // POST: api/document/upload  (MULTIPART)
        // Nimmt title + file an, speichert Datei 1:1 in MinIO (z.B. PDF), published Event
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] string title, [FromForm] IFormFile file, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DocumentValidationException("Titel darf nicht leer sein.");
            if (file == null || file.Length == 0)
                throw new DocumentValidationException("Keine Datei übergeben.");

            // 1) Metadaten in DB (Content optional leer)
            var doc = new Document
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = null,
                CreatedAt = DateTime.UtcNow
            };
            var saved = await _service.AddAsync(doc);

            // 2) Datei nach MinIO laden (Id + Originalextension)
            var bucket = _cfg["Minio:Bucket"] ?? "documents";
            var ext = Path.GetExtension(file.FileName);
            var objectName = string.IsNullOrWhiteSpace(ext)
                ? $"{saved.Id}"
                : $"{saved.Id}{ext}";
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

            await using (var stream = file.OpenReadStream())
            {
                await _storage.UploadAsync(bucket, objectName, stream, contentType, ct);
            }

            // 3) Event für den OCR-Worker
            var msg = new UploadMessage(saved.Id, file.FileName, bucket, objectName, DateTime.UtcNow);
            _eventBus.Publish(
                _cfg["RabbitMq:Exchange"] ?? _cfg["Rabbit:Exchange"] ?? "dms.events",
                _cfg["RabbitMq:RoutingUpload"] ?? _cfg["Rabbit:RoutingUpload"] ?? "document.uploaded",
                msg
            );

            _logger.LogInformation("Upload {DocId} ({File}) nach MinIO gespeichert und Event veröffentlicht.", saved.Id, file.FileName);
            return Ok(new { saved.Id, saved.Title, Bucket = bucket, ObjectName = objectName });
        }

        // DELETE: api/document/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                throw new DocumentNotFoundException($"Dokument mit ID {id} nicht gefunden");

            _logger.LogInformation("Dokument {Id} gelöscht.", id);
            return NoContent();
        }

        // GET: api/document/{id}/ocr
        [HttpGet("{id}/ocr")]
        public async Task<IActionResult> GetOcr(Guid id, CancellationToken ct)
        {
            var bucket = _cfg["Minio:Bucket"] ?? "documents";
            var objectName = $"{id}.ocr.txt";

            if (!await _storage.ExistsAsync(bucket, objectName, ct))
                return NotFound($"Kein OCR-Ergebnis für {id} vorhanden.");

            var stream = await _storage.DownloadAsync(bucket, objectName, ct);
            if (stream == null)
                return NotFound($"Kein OCR-Ergebnis für {id} vorhanden.");

            return File(stream, "text/plain", objectName);
        }
    }
}
