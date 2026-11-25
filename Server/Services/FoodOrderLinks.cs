using System;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace GIBS.Module.FBClients.Ordering.Services
{
    public record FoodOrderAccess(int TenantId, int SiteId, int OrderingModuleId, int InventoryModuleId, int ClientModuleId, int VisitId, int ClientId);

    public class FoodOrderLinks(IDataProtectionProvider dataProtection)
    {
        private readonly ITimeLimitedDataProtector _protector = dataProtection
            .CreateProtector("GIBS.Module.FBClients.Ordering.Link.v2").ToTimeLimitedDataProtector();

        public string CreateMessage(string foodBankName, string orderPage, FoodOrderAccess access)
        {
            if (!Uri.TryCreate(orderPage?.Trim(), UriKind.Absolute, out var page) || page.Scheme != Uri.UriSchemeHttps
                || page.UserInfo.Length != 0 || page.Fragment.Length != 0 || QueryHelpers.ParseQuery(page.Query).ContainsKey("key"))
                throw new ArgumentException("Configure Client Order Page as an absolute HTTPS order page URL without a key parameter or fragment.");
            if (string.IsNullOrWhiteSpace(foodBankName))
                throw new ArgumentException("Configure Food Bank Name before sending order links.");

            var key = _protector.Protect(JsonSerializer.Serialize(access), TimeSpan.FromHours(24));
            return $"{foodBankName.Trim()} Ordering: {QueryHelpers.AddQueryString(page.AbsoluteUri, "key", key)}";
        }

        public FoodOrderAccess Read(string key, int tenantId, int siteId, int orderingModuleId)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 1024) return null;
            try
            {
                var access = JsonSerializer.Deserialize<FoodOrderAccess>(_protector.Unprotect(key));
                return access != null && access.TenantId == tenantId && access.SiteId == siteId && access.OrderingModuleId == orderingModuleId
                    && access.InventoryModuleId > 0 && access.ClientModuleId > 0 && access.VisitId > 0 && access.ClientId > 0 ? access : null;
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
