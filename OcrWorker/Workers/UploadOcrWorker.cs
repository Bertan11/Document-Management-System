// OcrWorker/Workers/UploadOcrWorker.cs
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcrWorker.Messaging;
using OcrWorker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OcrWorker.Workers;

public sealed class UploadOcrWorker : BackgroundService
{
    private readonly ILogger<UploadOcrWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IObjectStorage _storage;
    private readonly OcrService _ocrService;

    private IConnection? _connection;
    private IModel? _channel;

    public UploadOcrWorker(
        ILogger<UploadOcrWorker> logger,
        IConfiguration configuration,
        IObjectStorage storage,
        OcrService ocrService)
    {
        _logger = logger;
        _configuration = configuration;
        _storage = storage;
        _ocrService = ocrService;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting UploadOcrWorker...");

        var hostName = _configuration["RabbitMq:HostName"] ?? "localhost";
        var userName = _configuration["RabbitMq:UserName"] ?? "guest";
        var password = _configuration["RabbitMq:Password"] ?? "guest";
        var portValue = _configuration["RabbitMq:Port"];
        var port = string.IsNullOrWhiteSpace(portValue) ? 5672 : int.Parse(portValue);

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            Port = port,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection("ocr-worker-connection");
        _channel = _connection.CreateModel();

        var exchangeName = _configuration["RabbitMq:Exchange"] ?? "dms.events";
        var queueName = _configuration["RabbitMq:QueueUpload"] ?? "dms.uploads";
        var routingKey = _configuration["RabbitMq:RoutingUpload"] ?? "document.uploaded";

        _channel.ExchangeDeclare(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueBind(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation(
            "Connected to RabbitMQ at {Host}:{Port}, listening on queue '{QueueName}' (exchange '{Exchange}', routing key '{RoutingKey}')",
            hostName, port, queueName, exchangeName, routingKey);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            _logger.LogError("RabbitMQ channel is not initialized. Worker will not consume messages.");
            return Task.CompletedTask;
        }

        var queueName = _configuration["RabbitMq:QueueUpload"] ?? "dms.uploads";

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received raw message: {Json}", json);

                UploadMessage? message = null;

                try
                {
                    message = JsonSerializer.Deserialize<UploadMessage>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize UploadMessage from JSON.");
                }

                if (message is null)
                {
                    _logger.LogWarning("Message could not be deserialized into UploadMessage. Nacking message.");
                    _channel!.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                var exists = await _storage.ExistsAsync(
                    message.Bucket,
                    message.ObjectName,
                    stoppingToken);

                if (!exists)
                {
                    _logger.LogWarning(
                        "Object '{ObjectName}' in bucket '{Bucket}' does not exist. Ack message and skip.",
                        message.ObjectName,
                        message.Bucket);

                    _channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }

                await using var pdfStream = await _storage.DownloadAsync(
                    message.Bucket,
                    message.ObjectName,
                    stoppingToken);

                _logger.LogInformation(
                    "Successfully downloaded object '{ObjectName}' from bucket '{Bucket}'. Size={Length} bytes",
                    message.ObjectName,
                    message.Bucket,
                    pdfStream.Length);

                // OCR ausführen
                var ocrText = await _ocrService.ExtractTextFromPdfAsync(pdfStream, stoppingToken);

                _logger.LogInformation(
                    "OCR completed for DocumentId={DocumentId}. Text length={Length} characters.",
                    message.DocumentId,
                    ocrText.Length);

                // OCR-Ergebnis als {DocumentId}.ocr.txt speichern
                var ocrObjectName = $"{message.DocumentId}.ocr.txt";
                var ocrBytes = Encoding.UTF8.GetBytes(ocrText);
                await using var ocrStream = new MemoryStream(ocrBytes);

                await _storage.UploadAsync(
                    message.Bucket,
                    ocrObjectName,
                    ocrStream,
                    "text/plain",
                    stoppingToken);

                _logger.LogInformation(
                    "Uploaded OCR result as '{OcrObjectName}' to bucket '{Bucket}'.",
                    ocrObjectName,
                    message.Bucket);

                _channel!.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing message.");
                _channel!.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }

            await Task.Yield();
        };

        _channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "UploadOcrWorker is now consuming messages from queue '{QueueName}'.",
            queueName);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping UploadOcrWorker...");

        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();

        return base.StopAsync(cancellationToken);
    }
}
