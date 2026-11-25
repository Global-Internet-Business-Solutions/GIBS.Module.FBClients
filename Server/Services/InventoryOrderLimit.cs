using System;
using System.Globalization;

namespace GIBS.Module.FBClients.Ordering.Services
{
    internal static class InventoryOrderLimit
    {
        internal static int Resolve(int? defaultLimit, string overrides, int householdSize)
        {
            var limit = defaultLimit.GetValueOrDefault();
            if (!string.IsNullOrWhiteSpace(overrides))
            {
                foreach (var entry in overrides.Split(','))
                {
                    var assignment = entry.Split('=');
                    if (assignment.Length == 2
                        && int.TryParse(assignment[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var size)
                        && size == householdSize
                        && int.TryParse(assignment[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity)
                        && quantity > 0)
                    {
                        limit = quantity;
                    }
                }
            }
            return Math.Clamp(limit, 0, 100);
        }
    }
}
