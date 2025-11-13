// OcrWorker/Services/OcrService.cs
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio.DataModel.Notification;
using Tesseract;

namespace OcrWorker.Services;

public class OcrService
{
    private readonly ILogger<OcrService> _logger;
    private readonly string _tessdataPath;
    private readonly string _language;

    public OcrService(ILogger<OcrService> logger, IConfiguration configuration)
    {
        _logger = logger;

        _tessdataPath = configuration["Ocr:TessdataPath"]
                        ?? Path.Combine(AppContext.BaseDirectory, "tessdata");

        _language = configuration["Ocr:Language"] ?? "eng";

        _logger.LogInformation(
            $"OcrService initialized with tessdata='{_tessdataPath}', language='{_language}'");
    }

    /// <summary>
    /// Führt OCR auf einem PDF-Stream durch und gibt den gesamten erkannten Text zurück.
    /// </summary>
    public async Task<string> ExtractTextFromPdfAsync(Stream pdfStream, CancellationToken ct = default)
    {
        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid():N}.pdf");

        await using (var fs = File.Create(tempPdfPath))
        {
            await pdfStream.CopyToAsync(fs, ct);
        }

        try
        {
            var sb = new StringBuilder();

            using var engine = new TesseractEngine(_tessdataPath, _language, EngineMode.Default);

            var settings = new MagickReadSettings
            {
                Density = new Density(300, 300)
            };

            using var images = new MagickImageCollection();
            images.Read(tempPdfPath, settings);

            _logger.LogInformation($"PDF loaded with {images.Count} page(s) for OCR.");

            var pageIndex = 0;
            foreach (var image in images)
            {
                ct.ThrowIfCancellationRequested();
                pageIndex++;

                using var pageImage = (MagickImage)image.Clone();
                pageImage.Format = MagickFormat.Png;

                using var ms = new MemoryStream();
                pageImage.Write(ms);
                ms.Position = 0;

                using var pix = Pix.LoadFromMemory(ms.ToArray());
                using var page = engine.Process(pix);

                var text = page.GetText() ?? string.Empty;
                sb.AppendLine(text);

                _logger.LogInformation($"Finished OCR for page {pageIndex}.");
            }

            return sb.ToString();
        }
        finally
        {
            try
            {
                if (File.Exists(tempPdfPath))
                {
                    File.Delete(tempPdfPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not delete temp PDF file '{tempPdfPath}'");
            }
        }
    }
}
