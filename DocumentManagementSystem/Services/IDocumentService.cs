using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.Services
{
    public interface IDocumentService
    {
        Task<IEnumerable<Document>> GetAllAsync();
        Task<Document> GetByIdAsync(Guid id);
        Task<Document> AddAsync(Document document);
        Task<Document> UpdateAsync(Document document);
        Task<bool> DeleteAsync(Guid id);
    }
}
