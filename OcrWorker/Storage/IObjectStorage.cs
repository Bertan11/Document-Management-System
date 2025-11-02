using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OcrWorker.Storage
{
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
    }
}
