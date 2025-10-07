using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;

namespace DocumentManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _service;

        public DocumentController(IDocumentService service)
        {
            _service = service;
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
                return NotFound();

            return Ok(document);
        }

        // POST: api/document
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Document document)
        {
            // Id und CreatedAt automatisch setzen
            document.Id = Guid.NewGuid();
            document.CreatedAt = DateTime.UtcNow;

            var created = await _service.AddAsync(document);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/document/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Document document)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            // Nur veränderbare Felder aktualisieren
            existing.Title = document.Title;
            existing.Content = document.Content;

            var updated = await _service.UpdateAsync(existing);
            return Ok(updated);
        }

        // DELETE: api/document/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
