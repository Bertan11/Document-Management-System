using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public Task<IEnumerable<Document>> GetAllAsync()
        {
            return _repository.GetAllAsync();
        }

        public Task<Document> GetByIdAsync(Guid id)
        {
            return _repository.GetByIdAsync(id);
        }

        public Task<Document> AddAsync(Document document)
        {
            return _repository.AddAsync(document);
        }

        public Task<Document> UpdateAsync(Document document)
        {
            return _repository.UpdateAsync(document);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            return _repository.DeleteAsync(id);
        }
    }
}
