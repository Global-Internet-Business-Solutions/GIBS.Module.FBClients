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

            if (!services.Any(s => s.ServiceType == typeof(Lists.Services.IListsService)))
            {
                services.AddScoped<Lists.Services.IListsService, Lists.Services.ClientListsService>();
            }

            if (!services.Any(s => s.ServiceType == typeof(Ordering.Services.IFoodOrderService)))
            {
                services.AddScoped<Ordering.Services.IFoodOrderService, Ordering.Services.ClientFoodOrderService>();
            }

            if (!services.Any(s => s.ServiceType == typeof(Inventory.Services.IInventoryService)))
            {
                services.AddScoped<Inventory.Services.IInventoryService, Inventory.Services.ClientInventoryService>();
            }

            if (!services.Any(s => s.ServiceType == typeof(Fulfillment.IFulfillmentService)))
            {
                services.AddScoped<Fulfillment.IFulfillmentService, Fulfillment.ClientFulfillmentService>();
            }

            if (!services.Any(s => s.ServiceType == typeof(ImportData.IImportDataService)))
            {
                services.AddScoped<ImportData.IImportDataService, ImportData.ClientImportDataService>();
            }
        }
    }
}
