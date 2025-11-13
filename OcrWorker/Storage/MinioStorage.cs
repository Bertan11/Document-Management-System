// OcrWorker/Services/MinioStorage.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace OcrWorker.Services;

public class MinioStorage : IObjectStorage
{
    private readonly IMinioClient _client;
    private readonly string _defaultBucket;

    public MinioStorage(IConfiguration cfg)
    {
        var endpoint = cfg["Minio:Endpoint"] ?? "http://localhost:9000";
        var accessKey = cfg["Minio:AccessKey"] ?? "minio";
        var secretKey = cfg["Minio:SecretKey"] ?? "minio12345";

        _defaultBucket = cfg["Minio:Bucket"] ?? "documents";

        var uri = new Uri(endpoint);
        var secure = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
        var port = uri.IsDefaultPort ? (secure ? 443 : 80) : uri.Port;

        _client = new MinioClient()
            .WithEndpoint(uri.Host, port)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(secure)
            .Build();
    }

    public async Task UploadAsync(
        string bucket,
        string objectName,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        bucket ??= _defaultBucket;

        // Bucket sicherstellen
        var be = new BucketExistsArgs().WithBucket(bucket);
        var bucketExists = await _client.BucketExistsAsync(be, ct);
        if (!bucketExists)
        {
            var mb = new MakeBucketArgs().WithBucket(bucket);
            await _client.MakeBucketAsync(mb, ct);
        }

        var put = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(put, ct);
    }

    public async Task<Stream> DownloadAsync(
        string bucket,
        string objectName,
        CancellationToken ct = default)
    {
        bucket ??= _defaultBucket;

        var ms = new MemoryStream();

        var get = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithCallbackStream(s => s.CopyTo(ms));

        await _client.GetObjectAsync(get, ct);

        ms.Position = 0;
        return ms;
    }

    public async Task<bool> ExistsAsync(
        string bucket,
        string objectName,
        CancellationToken ct = default)
    {
        bucket ??= _defaultBucket;

        try
        {
            var stat = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName);

            await _client.StatObjectAsync(stat, ct);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (MinioException)
        {
            // hier könntest du später noch loggen
            return false;
        }
    }
}
