using BLL.Shared.RabbitMessages;

namespace BLL.Services
{
    public interface IComputingService
    {
        double? CountDistance(AeroportsJob job);        
    }
}
