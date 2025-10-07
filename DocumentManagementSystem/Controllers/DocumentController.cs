using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;
using Microsoft.Extensions.Logging;
using DocumentManagementSystem.Exceptions;

namespace DocumentManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _service;
        private readonly RabbitMqService _rabbitMqService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(IDocumentService service, RabbitMqService rabbitMqService, ILogger<DocumentController> logger)
        {
            _service = service;
            _rabbitMqService = rabbitMqService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var documents = await _service.GetAllAsync();
            return Ok(documents);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var document = await _service.GetByIdAsync(id);
            if (document == null)
            {
                throw new DocumentNotFoundException($"Dokument mit ID {id} nicht gefunden");
            }
            return Ok(document);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Document document)
        {
            var created = await _service.AddAsync(document);

            // Nachricht an RabbitMQ senden
            var message = $"Document uploaded: {created.Id}, Title: {created.Title}";
            _rabbitMqService.SendMessage(message);

            _logger.LogInformation("Dokument {DocId} erstellt und Nachricht an RabbitMQ gesendet.", created.Id);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, Document document)
        {
            if (id != document.Id)
            {
                throw new DocumentValidationException("Update fehlgeschlagen: ID stimmt nicht mit Dokument überein");
            }

            var updated = await _service.UpdateAsync(document);
            if (updated == null)
            {
                throw new DocumentNotFoundException($"Dokument mit ID {id} nicht gefunden");
            }

            _logger.LogInformation("Dokument {DocId} wurde aktualisiert.", id);
            return Ok(updated);
        }
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string title)
        {
            if (file == null || file.Length == 0)
                throw new DocumentValidationException("Keine Datei hochgeladen.");

            // Datei in Bytes konvertieren
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var document = new Document
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = Convert.ToBase64String(ms.ToArray()) // oder direkt speichern
            };

            var created = await _service.AddAsync(document);

            // Nachricht an RabbitMQ senden
            var message = $"Document uploaded: {created.Id}, Title: {created.Title}";
            _rabbitMqService.SendMessage(message);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
            {
                throw new DocumentNotFoundException($"Dokument mit ID {id} nicht gefunden");
            }

            _logger.LogInformation("Dokument {Id} gelöscht.", id);
            return NoContent();
        }
    }
}
