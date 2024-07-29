using RabbitMQ.Client;

namespace BLL.Services
{
    public class RabbitMQService: IRabbitMQService
    {
        public RabbitMQService() { 
        }

        public IConnection CreateChannel()
        {
            ConnectionFactory connection = new ConnectionFactory()
            {
                UserName = "guest",
                Password = "guest",
                HostName = "localhost"
            };
            connection.DispatchConsumersAsync = true;
            var channel = connection.CreateConnection();
            return channel;
        }
    }
}
