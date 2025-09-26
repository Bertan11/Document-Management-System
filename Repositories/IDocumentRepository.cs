using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.Repositories
{
    public interface IDocumentRepository
    {
        Task<Document> CreateAsync(Document document);
        Task<Document?> GetByIdAsync(int id);
        Task<IEnumerable<Document>> GetAllAsync();
        Task<Document> UpdateAsync(Document document);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Document>> SearchByFilenameAsync(string filename);
        Task<IEnumerable<Document>> GetUnprocessedAsync();
    }
}