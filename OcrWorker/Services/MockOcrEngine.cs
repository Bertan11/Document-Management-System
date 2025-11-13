using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OcrWorker.Services
{
    public sealed class MockOcrEngine : IOcrEngine
    {
        public async Task<string> ExtractTextAsync(Stream input, string contentType, CancellationToken ct)
        {
            // Nur zum Testen: Inhalt als Text zurückgeben (oder Länge, falls binär)
            using var ms = new MemoryStream();
            await input.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();

            // Wenn es nach Text aussieht, als UTF-8 interpretieren, sonst Dummy
            try
            {
                var text = Encoding.UTF8.GetString(bytes);
                // Kleine Heuristik: wenn viele � drin sind, nimm Dummy
                if (text.Split('�').Length > bytes.Length / 16 + 1)
                    return $"[MOCK OCR] contentType={contentType}; length={bytes.Length}";
                return $"[MOCK OCR] contentType={contentType}\n{text}";
            }
            catch
            {
                return $"[MOCK OCR] contentType={contentType}; length={bytes.Length}";
            }
        }
    }
}
