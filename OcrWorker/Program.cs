using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

Console.WriteLine("📥 OCR Worker gestartet. Warte auf Nachrichten...");

var factory = new ConnectionFactory()
{
    HostName = "rabbitmq",
    UserName = "guest",
    Password = "guest"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare(
    queue: "documents",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"👉 Nachricht erhalten: {message}");

    // Fake-OCR Simulation
    Console.WriteLine($"⚡ Fake OCR Ergebnis: {message.ToUpper()}");
};

channel.BasicConsume(
    queue: "documents",
    autoAck: true,
    consumer: consumer
);

Console.WriteLine("⚡ Worker läuft. Beende mit STRG+C.");

// Worker dauerhaft laufen lassen
await Task.Delay(-1);
