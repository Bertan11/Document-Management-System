using Xunit;
using Moq;
using DocumentManagementSystem.Services;
using DocumentManagementSystem.Repositories;
using DocumentManagementSystem.Models;

namespace DocumentManagementSystem.DocumentServiceTests.cs
{
    public class DocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _mockRepository;
        private readonly DocumentService _documentService;

        public DocumentServiceTests()
        {
            _mockRepository = new Mock<IDocumentRepository>();
            _documentService = new DocumentService(_mockRepository.Object);
        }

        [Fact]
        public async Task UploadDocumentAsync_ValidInput_ReturnsDocument()
        {
            // Arrange
            var filename = "test.pdf";
            var contentType = "application/pdf";
            var fileSize = 1024L;
            var fileContent = new byte[] { 1, 2, 3, 4 };

            var expectedDocument = new Document
            {
                Id = 1,
                Filename = filename,
                ContentType = contentType,
                FileSize = fileSize,
                UploadDate = DateTime.UtcNow,
                IsProcessed = false
            };

            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Document>()))
                          .ReturnsAsync(expectedDocument);

            // Act
            var result = await _documentService.UploadDocumentAsync(filename, contentType, fileSize, fileContent);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(filename, result.Filename);
            Assert.Equal(contentType, result.ContentType);
            Assert.Equal(fileSize, result.FileSize);
            Assert.False(result.IsProcessed);

            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Document>()), Times.Once);
        }

        [Fact]
        public async Task UploadDocumentAsync_EmptyFilename_ThrowsArgumentException()
        {
            // Arrange
            var filename = "";
            var contentType = "application/pdf";
            var fileSize = 1024L;
            var fileContent = new byte[] { 1, 2, 3, 4 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _documentService.UploadDocumentAsync(filename, contentType, fileSize, fileContent));
        }

        [Fact]
        public async Task GetDocumentAsync_ValidId_ReturnsDocument()
        {
            // Arrange
            var documentId = 1;
            var expectedDocument = new Document
            {
                Id = documentId,
                Filename = "test.pdf",
                ContentType = "application/pdf"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(documentId))
                          .ReturnsAsync(expectedDocument);

            // Act
            var result = await _documentService.GetDocumentAsync(documentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(documentId, result.Id);
            Assert.Equal("test.pdf", result.Filename);

            _mockRepository.Verify(r => r.GetByIdAsync(documentId), Times.Once);
        }

        [Fact]
        public async Task GetDocumentAsync_InvalidId_ThrowsArgumentException()
        {
            // Arrange
            var invalidId = 0;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _documentService.GetDocumentAsync(invalidId));
        }

        [Fact]
        public async Task DeleteDocumentAsync_ValidId_ReturnsTrue()
        {
            // Arrange
            var documentId = 1;
            _mockRepository.Setup(r => r.DeleteAsync(documentId))
                          .ReturnsAsync(true);

            // Act
            var result = await _documentService.DeleteDocumentAsync(documentId);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(documentId), Times.Once);
        }

        [Fact]
        public async Task GetAllDocumentsAsync_ReturnsAllDocuments()
        {
            // Arrange
            var expectedDocuments = new List<Document>
            {
                new Document { Id = 1, Filename = "doc1.pdf" },
                new Document { Id = 2, Filename = "doc2.pdf" }
            };

            _mockRepository.Setup(r => r.GetAllAsync())
                          .ReturnsAsync(expectedDocuments);

            // Act
            var result = await _documentService.GetAllDocumentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }
    }
}