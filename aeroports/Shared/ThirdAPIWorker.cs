using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BLL.Services;

namespace aeroports.Shared
{
    public class ThirdAPIWorker : BackgroundService
    {
        private IConnection _connection;
        private readonly ILogger<ThirdAPIWorker> _logger;
        private readonly IMainService _mainService;

        public ThirdAPIWorker(ILogger<ThirdAPIWorker> logger, IMainService mainService) { 
            _logger = logger;
            _mainService = mainService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            stoppingToken.ThrowIfCancellationRequested();

            _mainService.TaskProcessing();
            _mainService.RepeatedCallProcessing();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);            
            _connection.Close();
            _logger.LogInformation("RabbitMQ connection is closed.");
        }
    }
}
