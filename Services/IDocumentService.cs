using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.Services
{
    public interface IDocumentService
    {
        Task<Document> UploadDocumentAsync(string filename, string contentType, long fileSize, byte[] fileContent);
        Task<Document?> GetDocumentAsync(int id);
        Task<IEnumerable<Document>> GetAllDocumentsAsync();
        Task<Document> UpdateDocumentAsync(Document document);
        Task<bool> DeleteDocumentAsync(int id);
        Task<IEnumerable<Document>> SearchDocumentsAsync(string searchTerm);
    }
}