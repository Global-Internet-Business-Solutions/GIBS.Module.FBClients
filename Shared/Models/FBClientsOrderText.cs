using System;
using System.Linq;

namespace GIBS.Module.FBClients.Models
{
    public class FBClientsOrderTextResult
    {
        public bool Accepted { get; set; }
        public bool StatusUpdated { get; set; }
        public bool AcceptanceUnconfirmed { get; set; }
        public string MessageSid { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public static class FBClientsOrderText
    {
        public static string NormalizePhoneNumber(string phone)
        {
            var value = (phone ?? string.Empty).Trim();
            if (value.Any(c => !char.IsAsciiDigit(c) && c != '+' && c != '(' && c != ')' && c != '-' && c != '.' && c != ' '))
            {
                throw new ArgumentException("Enter a valid phone number without an extension.");
            }
            value = new string(value.Where(c => char.IsAsciiDigit(c) || c == '+').ToArray());
            if (!value.StartsWith('+'))
            {
                if (value.Length == 10)
                {
                    value = "+1" + value;
                }
                else if (value.Length == 11 && value[0] == '1')
                {
                    value = "+" + value;
                }
            }
            if (value.Length < 9 || value.Length > 16 || value[0] != '+' || value[1] == '0'
                || value.Skip(1).Any(c => !char.IsAsciiDigit(c)))
            {
                throw new ArgumentException("Enter a 10-digit US/Canadian phone number or an international number beginning with + and its country code.");
            }
            return value;
        }

        public static string CreateMessage(string foodBankName, string orderPage, int visitId, int clientId)
        {
            if (visitId <= 0 || clientId <= 0)
            {
                throw new ArgumentException("A saved client and visit are required to send an order link.");
            }
            var prefix = orderPage?.Trim();
            var link = FormattableString.Invariant($"{prefix}{visitId}-{clientId}");
            if (!Uri.TryCreate(prefix, UriKind.Absolute, out var page) || page.Scheme != Uri.UriSchemeHttps
                || !Uri.TryCreate(link, UriKind.Absolute, out var uri) || uri.Authority != page.Authority
                || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new ArgumentException("Configure Client Order Page as an absolute HTTPS URL prefix, including the query parameter or path before the visit-client key.");
            }
            if (string.IsNullOrWhiteSpace(foodBankName))
            {
                throw new ArgumentException("Configure Food Bank Name before sending order links.");
            }
            return $"{foodBankName.Trim()} Ordering: {link}";
        }
    }
}
