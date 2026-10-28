using Microsoft.AspNetCore.Builder; 
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Oqtane.Infrastructure;
using GIBS.Module.FBClients.Repository;
using GIBS.Module.FBClients.Services;

namespace GIBS.Module.FBClients.Startup
{
    public class ServerStartup : IServerStartup
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // not implemented
        }

        public void ConfigureMvc(IMvcBuilder mvcBuilder)
        {
            // not implemented
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IFBClientsService, ServerFBClientsService>();
            services.AddTransient<IFBClientsOrderTextSender, TwilioOrderTextSender>();
            services.AddDbContextFactory<FBClientsContext>(opt => { }, ServiceLifetime.Transient);
        }
    }
}
