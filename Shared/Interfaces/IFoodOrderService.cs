using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GIBS.Module.FBClients.Ordering.Services
{
    public interface IFoodOrderService
    {
        Task<FoodOrderSheet> OpenAsync(int moduleId, string key);
        Task<FoodOrderSubmitResult> SubmitAsync(int moduleId, string key, Dictionary<int, int> quantities);
    }

    public class FoodOrderSheet
    {
        public DateOnly VisitDate { get; set; }
        public string ClientName { get; set; }
        public int? BagsAllowed { get; set; }
        public int HouseholdSize { get; set; }
        public List<FoodOrderSection> Sections { get; set; }
    }

    public record FoodOrderSection(string Name, string Instructions, List<FoodOrderChoice> Products);
    public record FoodOrderChoice(int Id, string Name, int Limit);

    public enum FoodOrderSubmitResult
    {
        Unavailable,
        InvalidQuantities,
        Submitted
    }

    public class FoodOrderRequest
    {
        public string Key { get; set; }
        public Dictionary<int, int> Quantities { get; set; }
    }
}
