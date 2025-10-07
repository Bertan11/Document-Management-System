using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using DocumentManagementSystem.Controllers;
using DocumentManagementSystem.Services;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Exceptions;

namespace DocumentManagementSystem.Tests
{
    public class DocumentControllerTests
    {
        private readonly Mock<IDocumentService> _mockService;
        private readonly Mock<RabbitMqService> _mockRabbit;
        private readonly Mock<ILogger<DocumentController>> _mockLogger;
        private readonly DocumentController _controller;

        public DocumentControllerTests()
        {
            _mockService = new Mock<IDocumentService>();
            _mockRabbit = new Mock<RabbitMqService>() { CallBase = true };
            _mockLogger = new Mock<ILogger<DocumentController>>();
            _controller = new DocumentController(_mockService.Object, _mockRabbit.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var id = Guid.NewGuid();
            var doc = new Document { Id = id, Title = "Doc", Content = "Data" };
            _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(doc);

            var result = await _controller.GetById(id) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task GetById_Throws_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync((Document)null);

            await Assert.ThrowsAsync<DocumentNotFoundException>(() => _controller.GetById(id));
        }

        [Fact]
        public async Task Update_ReturnsOk_WhenValid()
        {
            var doc = new Document { Id = Guid.NewGuid(), Title = "U", Content = "X" };
            _mockService.Setup(s => s.UpdateAsync(doc)).ReturnsAsync(doc);

            var result = await _controller.Update(doc.Id, doc) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task Update_Throws_WhenIdMismatch()
        {
            var doc = new Document { Id = Guid.NewGuid(), Title = "X" };
            await Assert.ThrowsAsync<DocumentValidationException>(() => _controller.Update(Guid.NewGuid(), doc));
        }

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenFound()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

            var result = await _controller.Delete(id) as NoContentResult;

            Assert.NotNull(result);
            Assert.Equal(204, result.StatusCode);
        }

        [Fact]
        public async Task Delete_Throws_WhenNotFound()
        {
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(false);

            await Assert.ThrowsAsync<DocumentNotFoundException>(() => _controller.Delete(id));
        }

        [Fact]
        public async Task Upload_Throws_WhenInvalid()
        {
            var doc = new Document { Title = "X", Content = "" };
            await Assert.ThrowsAsync<DocumentValidationException>(() => _controller.Upload(doc));
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenDocsExist()
        {
            var docs = new List<Document> { new Document { Title = "T1", Content = "C1" } };
            _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(docs);

            var result = await _controller.GetAll() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }


    }
}
