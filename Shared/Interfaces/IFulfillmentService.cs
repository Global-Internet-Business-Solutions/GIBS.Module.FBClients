using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Models;
using GIBS.Module.FBClients.Ordering.Services;

namespace GIBS.Module.FBClients.Fulfillment
{
    public static class FulfillmentModule
    {
        public const string DefinitionName = "GIBS.Module.FBClients.Fulfillment, GIBS.Module.FBClients.Client.Oqtane";
        public const string ClientsDefinitionName = "GIBS.Module.FBClients, GIBS.Module.FBClients.Client.Oqtane";
        public const string ClientsModuleId = "ClientsModuleId";
        public const string DefaultLocation = "DefaultLocation";
        public const string ShowCategories = "ShowCategories";
        public const string TwoColumns = "TwoColumns";
        public const string PickupTexts = "PickupTexts";
        public const int PageSize = 50;
    }

    public interface IFulfillmentService
    {
        Task<FulfillmentQueue> GetQueueAsync(int moduleId, DateOnly date, int? status, string location, int page);
        Task<FulfillmentOrder> GetOrderAsync(int moduleId, int visitId);
        Task SubmitPaperOrderAsync(int moduleId, int visitId, Dictionary<int, int> quantities);
        Task MarkFilledAsync(int moduleId, int visitId, int[] checkedItems);
        Task<FBClientsOrderTextResult> SendPickupTextAsync(int moduleId, int visitId);
    }

    public record FulfillmentVisit(int VisitId, int ClientId, string ClientName, DateOnly Date, DateTime EnteredOn,
        int? Bags, string Location, int Status, bool Locked, bool CanText);
    public record FulfillmentQueue(List<FulfillmentVisit> Visits, int Total, List<string> Locations, int ClientsModuleId, string ClientPagePath);
    public record FulfillmentLine(int Id, string Category, string Product, int Quantity);
    public record FulfillmentOrder(FulfillmentVisit Visit, int HouseholdSize, string Notes, List<FulfillmentLine> Lines, List<FoodOrderSection> Sections,
        FulfillmentCatalogAccess CatalogAccess);
    public enum FulfillmentCatalogAccess { NotConfigured, Denied, Available }
    public sealed class FulfillmentConflictException(string message) : Exception(message);
}
