using System.Text;
using RabbitMQ.Client;

namespace DocumentManagementSystem.Services
{
    public class RabbitMqService
    {
        private readonly ConnectionFactory _factory;

        // Standard-Konstruktor für echten Betrieb
        public RabbitMqService()
        {
            _factory = new ConnectionFactory()
            {
                HostName = "rabbitmq",
                UserName = "guest",
                Password = "guest"
            };
        }

        // Test-Konstruktor → erlaubt Dependency Injection
        public RabbitMqService(ConnectionFactory factory)
        {
            _factory = factory;
        }

        public void SendMessage(string message)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare("documents", false, false, false, null);
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish("", "documents", null, body);
            }
        }
    }
}
