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

            services.AddTransient<Lists.Services.IListsService, Lists.Services.ServerListsService>();
            services.AddTransient<Lists.Repository.IListsRepository, Lists.Repository.ListsRepository>();
            services.AddTransient<Ordering.Services.ServerFoodOrderService>();
            services.AddTransient<Ordering.Services.IFoodOrderService>(provider => provider.GetRequiredService<Ordering.Services.ServerFoodOrderService>());
            services.AddSingleton<Ordering.Services.FoodOrderLinks>();
            services.AddTransient<Inventory.Services.IInventoryService, Inventory.Services.ServerInventoryService>();
            services.AddTransient<Fulfillment.IFulfillmentService, Fulfillment.ServerFulfillmentService>();
            services.AddTransient<ImportData.IImportDataService, ImportData.ServerImportDataService>();
        }
    }
}
