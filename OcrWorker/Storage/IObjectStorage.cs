// OcrWorker/Services/IObjectStorage.cs
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OcrWorker.Services;

public interface IObjectStorage
{
    Task UploadAsync(
        string bucket,
        string objectName,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    Task<Stream> DownloadAsync(
        string bucket,
        string objectName,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        string bucket,
        string objectName,
        CancellationToken ct = default);
}
