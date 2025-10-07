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
            {
                throw new DocumentNotFoundException($"Dokument mit ID {id} nicht gefunden");
            }
            return Ok(document);
        }

        // POST: api/document
        [HttpPost]
        public async Task<IActionResult> Create(Document document)
        {
            var created = await _service.AddAsync(document);

            // Nachricht an RabbitMQ
            var message = $"Document uploaded: {created.Id}, Title: {created.Title}";
            _rabbitMqService.SendMessage(message);

            _logger.LogInformation("Dokument {DocId} erstellt und Nachricht an RabbitMQ gesendet.", created.Id);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/document/{id}
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

        // POST: api/document/upload
        /// <summary>
        /// Lädt eine Datei hoch (z. B. PDF oder Bild).
        /// </summary>
        /// <param name="file">Die Datei, die hochgeladen wird</param>
        /// <param name="title">Ein optionaler Titel</param>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromBody] Document document)
        {
            if (document == null || string.IsNullOrEmpty(document.Content))
            {
                throw new DocumentValidationException("Ungültige Datei oder leerer Inhalt.");
            }

            document.Id = Guid.NewGuid();
            document.CreatedAt = DateTime.UtcNow;

            await _service.AddAsync(document);

            _rabbitMqService.SendMessage($"Neue Datei hochgeladen: {document.Title} ({document.Id})");

            return Ok(document);
        }


        // DELETE: api/document/{id}
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
