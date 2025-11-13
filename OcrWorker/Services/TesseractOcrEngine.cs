// Services/TesseractOcrEngine.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OcrWorker.Services
{
    public sealed class TesseractOcrEngine : IOcrEngine
    {
        public async Task<string> ExtractTextAsync(Stream input, string contentType, CancellationToken ct)
        {
            // Fallback: TXT direkt lesen
            if (string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase))
            {
                using var sr = new StreamReader(input, Encoding.UTF8, true, 1024, leaveOpen: true);
#if NET8_0_OR_GREATER
                return await sr.ReadToEndAsync(ct);
#else
                return await sr.ReadToEndAsync();
#endif
            }

            // Input in temp-Datei
            var tmpDir = Path.Combine(Path.GetTempPath(), "ocr");
            Directory.CreateDirectory(tmpDir);
            var inPath = Path.Combine(tmpDir, $"in_{Guid.NewGuid():N}{GuessExt(contentType)}");
            var outBase = Path.Combine(tmpDir, $"out_{Guid.NewGuid():N}");
            await using (var fs = File.Create(inPath))
            {
                await input.CopyToAsync(fs, ct);
            }

            // tesseract inPath outBase --psm 1 -l eng
            var psi = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{inPath}\" \"{outBase}\" -l eng --psm 1",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var proc = Process.Start(psi);
            if (proc == null) throw new InvalidOperationException("Konnte tesseract nicht starten.");
            await proc.WaitForExitAsync(ct);

            // Ergebnis-Datei (outBase.txt) lesen
            var txtPath = outBase + ".txt";
            var text = File.Exists(txtPath) ? await File.ReadAllTextAsync(txtPath, ct) : "";

            // Cleanup
            TryDelete(inPath);
            TryDelete(txtPath);

            return text;
        }

        private static string GuessExt(string contentType) => contentType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "application/pdf" => ".pdf",
            _ => ".bin"
        };

        private static void TryDelete(string p)
        {
            try { if (File.Exists(p)) File.Delete(p); } catch { /* ignore */ }
        }
    }
}
