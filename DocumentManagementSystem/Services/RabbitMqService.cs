using System.Text;
using RabbitMQ.Client;

namespace DocumentManagementSystem.Services
{
    public class RabbitMqService
    {
        private readonly ConnectionFactory _factory;

        public RabbitMqService()
        {
            _factory = new ConnectionFactory()
            {
                HostName = "rabbitmq", // aus docker-compose
                UserName = "guest",
                Password = "guest"
            };
        }

        public void SendMessage(string message)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(
                    queue: "documents",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(
                    exchange: "",
                    routingKey: "documents",
                    basicProperties: null,
                    body: body
                );
            }
        }
    }
}
