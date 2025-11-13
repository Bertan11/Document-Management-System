using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocumentManagementSystem.Controllers;
using DocumentManagementSystem.Services;
using DocumentManagementSystem.Models;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Tests.Controllers
{
    public class DocumentControllerTests
    {
        private readonly Mock<IDocumentService> _mockService;
        private readonly Mock<IEventBus> _mockEventBus;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<DocumentController>> _mockLogger;
        private readonly Mock<IObjectStorage> _mockStorage;

        private readonly DocumentController _controller;

        public DocumentControllerTests()
        {
            _mockService = new Mock<IDocumentService>();
            _mockEventBus = new Mock<IEventBus>();
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<DocumentController>>();
            _mockStorage = new Mock<IObjectStorage>();

            // Optional: Standardwerte für RabbitMQ Keys
            _mockConfig.Setup(c => c["RabbitMq:Exchange"]).Returns("dms.events");
            _mockConfig.Setup(c => c["RabbitMq:RoutingUpload"]).Returns("document.uploaded");
            _mockConfig.Setup(c => c["Minio:Bucket"]).Returns("documents");

            _controller = new DocumentController(
                _mockService.Object,
                _mockEventBus.Object,
                _mockConfig.Object,
                _mockLogger.Object,
                _mockStorage.Object // <- neu erforderlich
            );
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithDocuments()
        {
            // Arrange
            var documents = new[] { new Document { Id = Guid.NewGuid(), Title = "Test" } };
            _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(documents);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(documents, okResult.Value);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenDocumentExists()
        {
            var id = Guid.NewGuid();
            var doc = new Document { Id = id, Title = "TestDoc" };
            _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(doc);

            var result = await _controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(doc, okResult.Value);
        }

        [Fact]
        public async Task Create_CallsStorageAndPublishesEvent()
        {
            // Arrange
            var newDoc = new Document { Title = "Hello", Content = "World" };
            var createdDoc = new Document { Id = Guid.NewGuid(), Title = "Hello", Content = "World" };

            _mockService.Setup(s => s.AddAsync(It.IsAny<Document>())).ReturnsAsync(createdDoc);

            // Act
            var result = await _controller.Create(newDoc);

            // Assert
            _mockStorage.Verify(s => s.UploadAsync(
                    It.IsAny<string>(),                // bucket
                    It.IsAny<string>(),                // objectName
                    It.IsAny<System.IO.Stream>(),      // content stream
                    "text/plain",                      // contentType
                    It.IsAny<CancellationToken>()      // <- optionales Arg MUSS explizit gematcht werden
                ),
                Times.Once);

            _mockEventBus.Verify(e => e.Publish(
                    It.IsAny<string>(),                // exchange
                    It.IsAny<string>(),                // routingKey
                    It.IsAny<UploadMessage>()          // payload-Typ konkret matchen
                ),
                Times.Once);

            Assert.IsType<CreatedAtActionResult>(result);

        }
    }
}
