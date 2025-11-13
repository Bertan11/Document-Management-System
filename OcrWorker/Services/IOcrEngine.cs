// Services/IOcrEngine.cs
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OcrWorker.Services
{
    public interface IOcrEngine
    {
        Task<string> ExtractTextAsync(Stream input, string contentType, CancellationToken ct);
    }
}
