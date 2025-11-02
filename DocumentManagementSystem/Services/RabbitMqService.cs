using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace DocumentManagementSystem.Services
{
    public interface IEventBus
    {
        void EnsureInfra(string exchange, string queue, string routingKey);
        void Publish<T>(string exchange, string routingKey, T payload);
    }

    public sealed class RabbitMqService : IEventBus, IDisposable
    {
        private readonly IConnection _conn;
        private readonly IModel _ch;

        public RabbitMqService(IConfiguration cfg)
        {
            var host = cfg["RabbitMq:Host"] ?? cfg["Rabbit:Host"] ?? "localhost";
            var user = cfg["RabbitMq:User"] ?? cfg["Rabbit:User"] ?? "guest";
            var pass = cfg["RabbitMq:Password"] ?? cfg["Rabbit:Pass"] ?? "guest";

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass
            };

            _conn = factory.CreateConnection("dms-api");
            _ch = _conn.CreateModel();
        }

        public void EnsureInfra(string exchange, string queue, string routingKey)
        {
            // DLX/DLQ Namen
            var dlxExchange = "dms.dlx";
            var dlqName = $"{queue}.dlq";

            // 1) DLX als fanout + DLQ
            _ch.ExchangeDeclare(dlxExchange, ExchangeType.Fanout, durable: true, autoDelete: false);
            _ch.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _ch.QueueBind(dlqName, dlxExchange, routingKey: string.Empty); // fanout ignoriert RoutingKey

            // 2) Haupt-Exchange (topic)
            _ch.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true, autoDelete: false);

            // 3) Haupt-Queue mit DLX-Argument
            var args = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = dlxExchange
            };

            _ch.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: args);
            _ch.QueueBind(queue, exchange, routingKey);

            // Fair dispatch
            _ch.BasicQos(0, prefetchCount: 10, global: false);
        }

        public void Publish<T>(string exchange, string routingKey, T payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

            var props = _ch.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2; // persistent

            _ch.BasicPublish(exchange, routingKey, mandatory: false, basicProperties: props, body: body);
        }

        public void Dispose()
        {
            _ch?.Dispose();
            _conn?.Dispose();
        }
    }

    // API-Contract (mit MinIO-Infos)
    public sealed record UploadMessage(
        Guid DocumentId,
        string Name,
        string Bucket,
        string ObjectName,
        DateTime UploadedAt
    );
}
