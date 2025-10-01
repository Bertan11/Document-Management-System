using Microsoft.AspNetCore.Mvc;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;

namespace DocumentManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(IDocumentService documentService, ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<Document>> UploadDocument(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileContent = memoryStream.ToArray();

                var document = await _documentService.UploadDocumentAsync(
                    file.FileName,
                    file.ContentType ?? "application/octet-stream",
                    file.Length,
                    fileContent);

                _logger.LogInformation("Document uploaded: {Filename} (ID: {Id})", document.Filename, document.Id);

                return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Document>> GetDocument(int id)
        {
            try
            {
                var document = await _documentService.GetDocumentAsync(id);
                if (document == null)
                    return NotFound($"Document with ID {id} not found");

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document>>> GetAllDocuments()
        {
            try
            {
                var documents = await _documentService.GetAllDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all documents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDocument(int id)
        {
            try
            {
                var result = await _documentService.DeleteDocumentAsync(id);
                if (!result)
                    return NotFound($"Document with ID {id} not found");

                _logger.LogInformation("Document deleted: ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Document>>> SearchDocuments([FromQuery] string query)
        {
            try
            {
                var documents = await _documentService.SearchDocumentsAsync(query);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}