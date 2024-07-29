using aeroports.Shared;
using BLL.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace aeroports
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;
        public Startup(ILogger<Startup> logger, IConfiguration configuration)
        {
            Configuration = configuration;
            _logger = logger;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddSingleton<IRabbitMQService, RabbitMQService>();
            services.AddSingleton<IMainService, MainService>();
            services.AddSingleton<IComputingService, ComputingService>();
            services.AddLogging(loggingBuilder => {
                loggingBuilder.AddNLog("nlog.config");
            });
            services.AddHostedService<ThirdAPIWorker>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }            
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
