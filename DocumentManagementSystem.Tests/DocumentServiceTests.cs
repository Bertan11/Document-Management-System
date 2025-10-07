using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Repositories;
using DocumentManagementSystem.Services;

namespace DocumentManagementSystem.Tests
{
    public class DocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly DocumentService _service;

        public DocumentServiceTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _service = new DocumentService(_mockRepo.Object);
        }

        [Fact]
        public async Task AddDocument_Saves_WhenValid()
        {
            var doc = new Document { Id = Guid.NewGuid(), Title = "Test", Content = "Hello" };
            await _service.AddAsync(doc);
            _mockRepo.Verify(r => r.AddAsync(It.Is<Document>(d => d.Title == "Test")), Times.Once);
        }

        [Fact]
        public async Task GetDocument_ReturnsDocument_WhenExists()
        {
            var id = Guid.NewGuid();
            var doc = new Document { Id = id, Title = "Hallo", Content = "Inhalt" };
            _mockRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(doc);

            var result = await _service.GetByIdAsync(id);

            Assert.NotNull(result);
            Assert.Equal("Hallo", result.Title);
        }

        [Fact]
        public async Task GetAllDocuments_ReturnsList()
        {
            var list = new List<Document>
            {
                new Document { Id = Guid.NewGuid(), Title = "Doc1" },
                new Document { Id = Guid.NewGuid(), Title = "Doc2" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

            var result = await _service.GetAllAsync();

            Assert.Equal(2, ((List<Document>)result).Count);
        }

        [Fact]
        public async Task UpdateDocument_ReturnsUpdated()
        {
            var doc = new Document { Id = Guid.NewGuid(), Title = "Old", Content = "X" };
            _mockRepo.Setup(r => r.UpdateAsync(doc)).ReturnsAsync(doc);

            var result = await _service.UpdateAsync(doc);

            Assert.NotNull(result);
            Assert.Equal("Old", result.Title);
        }

        [Fact]
        public async Task DeleteDocument_CallsRepository_WhenExists()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(id);

            Assert.True(result);
            _mockRepo.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteDocument_ReturnsFalse_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.DeleteAsync(id)).ReturnsAsync(false);

            var result = await _service.DeleteAsync(id);

            Assert.False(result);
        }
       

        [Fact]
        public async Task UpdateDocument_ReturnsNull_WhenNotFound()
        {
            var doc = new Document { Id = Guid.NewGuid(), Title = "X" };
            _mockRepo.Setup(r => r.UpdateAsync(doc)).ReturnsAsync((Document)null);

            var result = await _service.UpdateAsync(doc);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetById_ReturnsNull_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Document)null);

            var result = await _service.GetByIdAsync(id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAll_ReturnsEmpty_WhenNoDocs()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Document>());

            var result = await _service.GetAllAsync();

            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateDocument_Saves_WhenValid()
        {
            var doc = new Document { Id = Guid.NewGuid(), Title = "U1", Content = "C" };
            _mockRepo.Setup(r => r.UpdateAsync(doc)).ReturnsAsync(doc);

            var result = await _service.UpdateAsync(doc);

            Assert.NotNull(result);
            Assert.Equal("U1", result.Title);
        }

    }
}
