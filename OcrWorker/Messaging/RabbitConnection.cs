using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace OcrWorker.Messaging;

public sealed class RabbitConnection : IDisposable
{
    public IConnection Connection { get; }
    public IModel Channel { get; }

    public RabbitConnection(IConfiguration cfg)
    {
        var host = cfg["Rabbit:Host"] ?? "localhost";
        var user = cfg["Rabbit:User"] ?? "guest";
        var pass = cfg["Rabbit:Pass"] ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = pass,
            DispatchConsumersAsync = true
        };

        Connection = factory.CreateConnection("dms-ocr");
        Channel = Connection.CreateModel();

        var ex = cfg["Rabbit:Exchange"] ?? "dms.events";
        var q = cfg["Rabbit:QueueUpload"] ?? "dms.uploads";
        var rk = cfg["Rabbit:RoutingUpload"] ?? "document.uploaded";

        // DLX einrichten
        var dlx = "dms.dlx";
        var dlq = "dms.uploads.dlq";

        Channel.ExchangeDeclare(ex, ExchangeType.Topic, durable: true, autoDelete: false);
        Channel.ExchangeDeclare(dlx, ExchangeType.Fanout, durable: true, autoDelete: false);
        Channel.QueueDeclare(dlq, durable: true, exclusive: false, autoDelete: false);
        Channel.QueueBind(dlq, dlx, routingKey: "");

        Channel.QueueDeclare(q, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object> { ["x-dead-letter-exchange"] = dlx });
        Channel.QueueBind(q, ex, rk);

        var prefetch = cfg.GetValue<ushort?>("Rabbit:Prefetch") ?? 5;
        Channel.BasicQos(0, prefetch, global: false);
    }

    public void Dispose()
    {
        Channel?.Dispose();
        Connection?.Dispose();
    }
}
