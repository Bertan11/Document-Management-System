using System.ComponentModel.DataAnnotations;

namespace DocumentManagementSystem.Models
{
    public class Document
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Filename { get; set; } = string.Empty;

        [MaxLength(50)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public string? OcrText { get; set; }

        public string? Summary { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedDate { get; set; }

        [MaxLength(100)]
        public string? Tags { get; set; }

        public bool IsProcessed { get; set; } = false;
        public bool HasOcr { get; set; } = false;
        public bool HasSummary { get; set; } = false;
    }
}