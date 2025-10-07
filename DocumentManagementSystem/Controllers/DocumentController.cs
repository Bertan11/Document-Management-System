using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;
using Microsoft.Extensions.Logging;

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
                _logger.LogWarning("Dokument mit ID {Id} nicht gefunden", id);
                return NotFound();
            }
            return Ok(document);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Document document)
        {
            try
            {
                var created = await _service.AddAsync(document);

                // Nachricht an RabbitMQ senden
                var message = $"Document uploaded: {created.Id}, Title: {created.Title}";
                _rabbitMqService.SendMessage(message);

                _logger.LogInformation("Dokument {DocId} erstellt und Nachricht an RabbitMQ gesendet.", created.Id);

                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Erstellen des Dokuments");
                return StatusCode(500, "Fehler beim Erstellen des Dokuments");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, Document document)
        {
            if (id != document.Id)
            {
                _logger.LogWarning("Update fehlgeschlagen: ID {Id} stimmt nicht mit Dokument überein", id);
                return BadRequest();
            }

            var updated = await _service.UpdateAsync(document);
            _logger.LogInformation("Dokument {DocId} wurde aktualisiert.", id);

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
            {
                _logger.LogWarning("Löschen fehlgeschlagen: Dokument {Id} nicht gefunden.", id);
                return NotFound();
            }

            _logger.LogInformation("Dokument {Id} gelöscht.", id);
            return NoContent();
        }
    }
}
