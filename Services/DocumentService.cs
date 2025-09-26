using DocumentManagementSystem.Models;
using DocumentManagementSystem.Repositories;

namespace DocumentManagementSystem.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repository;

        public DocumentService(IDocumentRepository repository)
        {
            _repository = repository;
        }

        public async Task<Document> UploadDocumentAsync(string filename, string contentType, long fileSize, byte[] fileContent)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException("Filename cannot be empty", nameof(filename));

            if (fileSize <= 0)
                throw new ArgumentException("File size must be greater than 0", nameof(fileSize));

            if (fileContent == null || fileContent.Length == 0)
                throw new ArgumentException("File content cannot be empty", nameof(fileContent));

            var document = new Document
            {
                Filename = filename,
                ContentType = contentType,
                FileSize = fileSize,
                FilePath = GenerateFilePath(filename),
                UploadDate = DateTime.UtcNow,
                IsProcessed = false,
                HasOcr = false,
                HasSummary = false
            };

            var savedDocument = await _repository.CreateAsync(document);
            return savedDocument;
        }

        public async Task<Document?> GetDocumentAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Document ID must be greater than 0", nameof(id));

            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Document> UpdateDocumentAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (document.Id <= 0)
                throw new ArgumentException("Document ID must be greater than 0");

            var existingDocument = await _repository.GetByIdAsync(document.Id);
            if (existingDocument == null)
                throw new InvalidOperationException($"Document with ID {document.Id} not found");

            return await _repository.UpdateAsync(document);
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Document ID must be greater than 0", nameof(id));

            return await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<Document>> SearchDocumentsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllDocumentsAsync();

            return await _repository.SearchByFilenameAsync(searchTerm);
        }

        private static string GenerateFilePath(string filename)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            return $"{timestamp}/{uniqueId}_{filename}";
        }
    }
}