using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Oqtane.Services;
using GIBS.Module.FBClients.Services;

namespace GIBS.Module.FBClients.Startup
{
    public class ClientStartup : IClientStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            if (!services.Any(s => s.ServiceType == typeof(IFBClientsService)))
            {
                services.AddScoped<IFBClientsService, ClientFBClientsService>();
            }
        }
    }
}
