using RabbitMQ.Client;

namespace BLL.Services
{
    public interface IRabbitMQService
    {
        IConnection CreateChannel();
    }
}
