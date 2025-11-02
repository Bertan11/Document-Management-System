using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OcrWorker.Contracts;
using OcrWorker.Storage;

namespace OcrWorker.Workers
{
    public sealed class OcrConsumer : BackgroundService
    {
        private readonly ILogger<OcrConsumer> _logger;
        private readonly IObjectStorage _storage;
        private readonly IConnection _conn;
        private readonly IModel _ch;
        private readonly string _queue;

        public OcrConsumer(ILogger<OcrConsumer> logger, IConfiguration cfg, IObjectStorage storage)
        {
            _logger = logger;
            _storage = storage;

            // RabbitMQ-Konfiguration lesen
            var host = cfg["Rabbit:Host"] ?? "rabbitmq";
            var user = cfg["Rabbit:User"] ?? "guest";
            var pass = cfg["Rabbit:Pass"] ?? "guest";
            _queue = cfg["Rabbit:QueueUpload"] ?? "dms.uploads";
            var prefetch = ushort.TryParse(cfg["Rabbit:Prefetch"], out var p) ? p : (ushort)5;

            // Verbindung + Channel
            var factory = new ConnectionFactory { HostName = host, UserName = user, Password = pass };
            _conn = factory.CreateConnection("dms-ocr");
            _ch = _conn.CreateModel();

            // WICHTIG: Queue NICHT neu anlegen, nur passiv prüfen.
            // Die API hat die Queue mit DLX-Argumenten erstellt.
            _ch.QueueDeclarePassive(_queue);

            // Fair Dispatch
            _ch.BasicQos(prefetchSize: 0, prefetchCount: prefetch, global: false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_ch);

            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var msg = JsonSerializer.Deserialize<UploadMessage>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (msg is null)
                    {
                        _logger.LogWarning("Konnte Nachricht nicht deserialisieren: {Json}", json);
                        _ch.BasicAck(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    _logger.LogInformation("Nachricht empfangen: {DocId} - {Name}", msg.DocumentId, msg.Name);

                    // Datei aus MinIO holen
                    using var inStream = await _storage.DownloadAsync(msg.Bucket, msg.ObjectName, stoppingToken);
                    using var reader = new StreamReader(inStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

#if NET8_0_OR_GREATER
                    var sourceText = await reader.ReadToEndAsync(stoppingToken);
#else
                    var sourceText = await reader.ReadToEndAsync();
#endif

                    var ocrText = $"[OCR] length={sourceText?.Length ?? 0}\n{sourceText}".TrimEnd();

                    // OCR-Resultat wieder nach MinIO schreiben
                    var ocrObjectName = $"{msg.DocumentId}.ocr.txt";
                    using var outMs = new MemoryStream(Encoding.UTF8.GetBytes(ocrText));
                    await _storage.UploadAsync(msg.Bucket, ocrObjectName, outMs, "text/plain", stoppingToken);

                    _logger.LogInformation("OCR gespeichert: {Bucket}/{Object}", msg.Bucket, ocrObjectName);

                    _ch.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // sauberes Beenden
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Verarbeitung.");
                    _ch.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // → DLQ
                }
            };

            _ch.BasicConsume(queue: _queue, autoAck: false, consumer: consumer);
            _logger.LogInformation("OcrConsumer lauscht auf Queue '{Queue}'.", _queue);
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            try { _ch?.Close(); } catch { }
            try { _conn?.Close(); } catch { }
            _ch?.Dispose();
            _conn?.Dispose();
            base.Dispose();
        }
    }
}
