using Microsoft.EntityFrameworkCore;
using DocumentManagementSystem.Data;
using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DocumentDbContext _context;

        public DocumentRepository(DocumentDbContext context)
        {
            _context = context;
        }

        public async Task<Document> CreateAsync(Document document)
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<Document?> GetByIdAsync(int id)
        {
            return await _context.Documents.FindAsync(id);
        }

        public async Task<IEnumerable<Document>> GetAllAsync()
        {
            return await _context.Documents
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        public async Task<Document> UpdateAsync(Document document)
        {
            _context.Documents.Update(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var document = await GetByIdAsync(id);
            if (document == null) return false;
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Document>> SearchByFilenameAsync(string filename)
        {
            return await _context.Documents
                .Where(d => d.Filename.Contains(filename))
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Document>> GetUnprocessedAsync()
        {
            return await _context.Documents
                .Where(d => !d.IsProcessed)
                .OrderBy(d => d.UploadDate)
                .ToListAsync();
        }
    }
}