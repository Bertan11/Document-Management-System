using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OcrWorker.Services
{
    public sealed class TesseractOcr : IOcrEngine
    {
        private static async Task<int> RunAsync(string fileName, string args, StringBuilder? stdout, StringBuilder? stderr, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = stdout is not null,
                RedirectStandardError = stderr is not null,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            p.Start();

            Task readOut = Task.CompletedTask, readErr = Task.CompletedTask;
            if (stdout is not null)
                readOut = p.StandardOutput.ReadToEndAsync().ContinueWith(t => stdout.Append(t.Result), ct);
            if (stderr is not null)
                readErr = p.StandardError.ReadToEndAsync().ContinueWith(t => stderr.Append(t.Result), ct);

            await Task.WhenAll(readOut, readErr);
            await p.WaitForExitAsync(ct);
            return p.ExitCode;
        }

        public async Task<string> ExtractTextAsync(Stream inputStream, string originalFileName, CancellationToken ct)
        {
            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            var tmpRoot = Path.Combine(Path.GetTempPath(), "dmsocr");
            Directory.CreateDirectory(tmpRoot);

            var tmpIn = Path.Combine(tmpRoot, Guid.NewGuid().ToString("N") + ext);
            await using (var f = File.Create(tmpIn))
                await inputStream.CopyToAsync(f, ct);

            try
            {
                string text;

                if (ext is ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" or ".bmp")
                {
                    text = await TesseractImageAsync(tmpIn, ct);
                }
                else if (ext is ".pdf")
                {
                    // 1) PDF -> PNG (erste Seite reicht meist für Proof)
                    var outPrefix = Path.Combine(tmpRoot, Path.GetFileNameWithoutExtension(tmpIn));
                    var png = outPrefix + ".png";
                    var sbErr = new StringBuilder();
                    var rc1 = await RunAsync("pdftoppm", $"-png -singlefile {Quote(tmpIn)} {Quote(outPrefix)}", null, sbErr, ct);
                    if (rc1 != 0 || !File.Exists(png))
                        throw new InvalidOperationException($"pdftoppm failed (rc={rc1}): {sbErr}");

                    text = await TesseractImageAsync(png, ct);
                    File.Delete(png);
                }
                else
                {
                    // Fallback: kein OCR – nur Textdatei/Unbekannt -> lesen
                    text = await File.ReadAllTextAsync(tmpIn, Encoding.UTF8, ct);
                }

                return text;
            }
            finally
            {
                TryDelete(tmpIn);
            }
        }

        private static async Task<string> TesseractImageAsync(string imagePath, CancellationToken ct)
        {
            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();

            // -l deu+eng: deutsche und englische Sprache
            var rc = await RunAsync("tesseract", $"{Quote(imagePath)} stdout -l deu+eng", sbOut, sbErr, ct);
            if (rc != 0)
                throw new InvalidOperationException($"tesseract failed (rc={rc}): {sbErr}");

            return sbOut.ToString();
        }

        private static string Quote(string path)
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"\"{path}\"" : $"'{path}'";

        private static void TryDelete(string p) { try { if (File.Exists(p)) File.Delete(p); } catch { } }
    }
}
