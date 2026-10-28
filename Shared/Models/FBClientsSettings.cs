#nullable enable

using System;
using System.Collections.Generic;

namespace GIBS.Module.FBClients.Models
{
    /// <summary>
    /// Strongly-typed module settings and setting key constants ported from the DNN FBClients module
    /// (<c>Components\FBClientsSettings.cs</c> and <c>Settings.ascx.cs</c>).
    /// </summary>
    public class FBClientsSettings
    {
        public static class Keys
        {
            public const string Recaptcha = "recaptcha";

            // Food Bank Settings
            public const string FBName = "fBName";
            public const string FBAddress = "fBAddress";
            public const string FBCity = "fBCity";
            public const string FBState = "fBState";
            public const string FBZipCode = "fBZipCode";
            public const string FBPhoneNumber = "fBPhoneNumber";
            public const string FBFaxNumber = "fBFaxNumber";
            public const string AllowedIPAddress = "allowedIPAddress";
            public const string GoogleAPIKey = "googleAPIKey";
            public const string GoogleGeocodeAPIKey = "googleGeocodeAPIKey";
            public const string EnableGeocodeOnSave = "enableGeocodeOnSave";

            // General Settings
            public const string IDCardImagePath = "iDCardImagePath";
            public const string DaysToValidVisit = "daysToValidVisit";
            public const string ShowRequiredFields = "showRequiredFields";
            public const string ShowPrimaryIncomeSource = "showPrimaryIncomeSource";
            public const string ShowSendText = "showSendText";
            public const string ShowClientServiceLocation = "showClientServiceLocation";
            public const string ShowSuffix = "showSuffix";
            public const string ShowRelationshipToClient = "showRelationshipToClient";
            public const string ShowEthnicity = "showEthnicity";
            public const string ShowPhotoID = "showPhotoID";
            public const string ShowOneBagOnly = "showOneBagOnly";
            public const string ShowVisitStopLight = "showVisitStopLight";
            public const string ShowIncExpSummary = "showIncExpSummary";
            public const string ShowExpense = "showExpense";
            public const string ShowDisabilities = "showDisabilities";
            public const string ShowClientType = "showClientType";
            public const string ShowClientIdCard = "showClientIdCard";
            public const string ShowPrintShoppingLabel = "showPrintShoppingLabel";
            public const string PrintShoppingLabelQuantity = "printShoppingLabelQuantity";
            public const string ShowPrintBarCodeLabel = "showPrintBarCodeLabel";
            public const string IncludeInactiveOnSearch = "includeInactiveOnSearch";
            public const string FlagForReviewNotify = "flagForReviewNotify";
            public const string FocusableControl = "focusableControl";
            public const string BagAllowance = "bagAllowance";
            public const string GroupHomeBagAllowance = "groupHomeBagAllowance";

            // Twilio Fields
            public const string TwilioAccountSid = "twilioAccountSid";
            public const string TwilioAuthToken = "twilioAuthToken";
            public const string TwilioPhoneNumber = "twilioPhoneNumber";
            public const string ClientOrderPage = "clientOrderPage";

            // Christmas Toys
            public const string ShowXmasToys = "showXmasToys";
            public const string XmasToysYear = "xmasToysYear";
            public const string XmasRequireSizeAgeRange = "xmasRequireSizeAgeRange";
            public const string ShowXmasGiftFields = "showXmasGiftFields";
            public const string ToyTicketContent = "toyTicketContent";

            // Instructions
            public const string IncomeEligibilityGuidelines = "incomeEligibilityGuidelines";

            // Required Fields
            public const string ReqPrimaryIncomeSource = "reqPrimaryIncomeSource";
            public const string ReqZipCode = "reqZipCode";
            public const string ReqGender = "reqGender";
            public const string ReqEthnicity = "reqEthnicity";
            public const string ReqAFMVerified = "reqAFMVerified";
            public const string ReqAFMRelationship = "reqAFMRelationship";

            // Permissions
            public const string ClientManagerUserRole = "clientManagerUserRole";
            public const string ClientManagerDeleteRecordRole = "clientManagerDeleteRecordRole";
        }

        // Food Bank Settings
        public string FBName { get; set; } = string.Empty;
        public string FBAddress { get; set; } = string.Empty;
        public string FBCity { get; set; } = string.Empty;
        public string FBState { get; set; } = string.Empty;
        public string FBZipCode { get; set; } = string.Empty;
        public string FBPhoneNumber { get; set; } = string.Empty;
        public string FBFaxNumber { get; set; } = string.Empty;
        public string AllowedIPAddress { get; set; } = string.Empty;
        public string GoogleAPIKey { get; set; } = string.Empty;
        public string GoogleGeocodeAPIKey { get; set; } = string.Empty;
        public bool EnableGeocodeOnSave { get; set; }

        // General Settings
        public string IDCardImagePath { get; set; } = string.Empty;
        public string DaysToValidVisit { get; set; } = "1";
        public bool ShowRequiredFields { get; set; }
        public bool ShowPrimaryIncomeSource { get; set; }
        public bool ShowSendText { get; set; }
        public bool ShowClientServiceLocation { get; set; }
        public bool ShowSuffix { get; set; }
        public bool ShowRelationshipToClient { get; set; }
        public bool ShowEthnicity { get; set; }
        public bool ShowPhotoID { get; set; }
        public bool ShowOneBagOnly { get; set; }
        public bool ShowVisitStopLight { get; set; }
        public bool ShowIncExpSummary { get; set; }
        public bool ShowExpense { get; set; }
        public bool ShowDisabilities { get; set; }
        public bool ShowClientType { get; set; }
        public bool ShowClientIdCard { get; set; }
        public bool ShowPrintShoppingLabel { get; set; }
        public string PrintShoppingLabelQuantity { get; set; } = "1";
        public bool ShowPrintBarCodeLabel { get; set; }
        public bool IncludeInactiveOnSearch { get; set; }
        public string FlagForReviewNotify { get; set; } = string.Empty;
        public string FocusableControl { get; set; } = "txtClientId";
        public string BagAllowance { get; set; } = string.Empty;
        public string GroupHomeBagAllowance { get; set; } = string.Empty;

        // Twilio Fields
        public string TwilioAccountSid { get; set; } = string.Empty;
        public string TwilioAuthToken { get; set; } = string.Empty;
        public string TwilioPhoneNumber { get; set; } = string.Empty;
        public string ClientOrderPage { get; set; } = string.Empty;

        // Christmas Toys
        public bool ShowXmasToys { get; set; }
        public string XmasToysYear { get; set; } = string.Empty;
        public string XmasRequireSizeAgeRange { get; set; } = string.Empty;
        public bool ShowXmasGiftFields { get; set; }
        public string ToyTicketContent { get; set; } = string.Empty;

        // Instructions
        public string IncomeEligibilityGuidelines { get; set; } = string.Empty;

        // Required Fields
        public bool ReqPrimaryIncomeSource { get; set; }
        public bool ReqZipCode { get; set; } = true;
        public bool ReqGender { get; set; }
        public bool ReqEthnicity { get; set; }
        public bool ReqAFMVerified { get; set; }
        public bool ReqAFMRelationship { get; set; }

        // Permissions
        public string ClientManagerUserRole { get; set; } = string.Empty;
        public string ClientManagerDeleteRecordRole { get; set; } = string.Empty;

        // Other legacy settings
        public string Recaptcha { get; set; } = string.Empty;

        public static FBClientsSettings FromDictionary(IDictionary<string, string>? settings)
        {
            var result = new FBClientsSettings();
            if (settings == null)
            {
                return result;
            }

            result.FBName = GetString(settings, Keys.FBName, string.Empty);
            result.FBAddress = GetString(settings, Keys.FBAddress, string.Empty);
            result.FBCity = GetString(settings, Keys.FBCity, string.Empty);
            result.FBState = GetString(settings, Keys.FBState, string.Empty);
            result.FBZipCode = GetString(settings, Keys.FBZipCode, string.Empty);
            result.FBPhoneNumber = GetString(settings, Keys.FBPhoneNumber, string.Empty);
            result.FBFaxNumber = GetString(settings, Keys.FBFaxNumber, string.Empty);
            result.AllowedIPAddress = GetString(settings, Keys.AllowedIPAddress, string.Empty);
            result.GoogleAPIKey = GetString(settings, Keys.GoogleAPIKey, string.Empty);
            result.GoogleGeocodeAPIKey = GetString(settings, Keys.GoogleGeocodeAPIKey, string.Empty);
            result.EnableGeocodeOnSave = GetBool(settings, Keys.EnableGeocodeOnSave, false);

            result.IDCardImagePath = GetString(settings, Keys.IDCardImagePath, string.Empty);
            result.DaysToValidVisit = GetString(settings, Keys.DaysToValidVisit, "1");
            if (string.IsNullOrWhiteSpace(result.DaysToValidVisit))
            {
                result.DaysToValidVisit = "1";
            }

            result.ShowRequiredFields = GetBool(settings, Keys.ShowRequiredFields, false);
            result.ShowPrimaryIncomeSource = GetBool(settings, Keys.ShowPrimaryIncomeSource, false);
            result.ShowSendText = GetBool(settings, Keys.ShowSendText, false);
            result.ShowClientServiceLocation = GetBool(settings, Keys.ShowClientServiceLocation, false);
            result.ShowSuffix = GetBool(settings, Keys.ShowSuffix, false);
            result.ReqAFMRelationship = GetBool(settings, Keys.ReqAFMRelationship, false);
            result.ShowRelationshipToClient = GetBool(settings, Keys.ShowRelationshipToClient, result.ReqAFMRelationship);
            result.ShowEthnicity = GetBool(settings, Keys.ShowEthnicity, false);
            result.ShowPhotoID = GetBool(settings, Keys.ShowPhotoID, false);
            result.ShowOneBagOnly = GetBool(settings, Keys.ShowOneBagOnly, false);
            result.ShowVisitStopLight = GetBool(settings, Keys.ShowVisitStopLight, false);
            result.ShowIncExpSummary = GetBool(settings, Keys.ShowIncExpSummary, false);
            result.ShowExpense = GetBool(settings, Keys.ShowExpense, false);
            result.ShowDisabilities = GetBool(settings, Keys.ShowDisabilities, false);
            result.ShowClientType = GetBool(settings, Keys.ShowClientType, false);
            result.ShowClientIdCard = GetBool(settings, Keys.ShowClientIdCard, false);
            result.ShowPrintShoppingLabel = GetBool(settings, Keys.ShowPrintShoppingLabel, false);
            result.PrintShoppingLabelQuantity = GetString(settings, Keys.PrintShoppingLabelQuantity, "1");
            if (string.IsNullOrWhiteSpace(result.PrintShoppingLabelQuantity))
            {
                result.PrintShoppingLabelQuantity = "1";
            }

            result.ShowPrintBarCodeLabel = GetBool(settings, Keys.ShowPrintBarCodeLabel, false);
            result.IncludeInactiveOnSearch = GetBool(settings, Keys.IncludeInactiveOnSearch, false);
            result.FlagForReviewNotify = GetString(settings, Keys.FlagForReviewNotify, string.Empty);
            result.FocusableControl = GetString(settings, Keys.FocusableControl, "txtClientId");
            result.BagAllowance = GetString(settings, Keys.BagAllowance, string.Empty);
            result.GroupHomeBagAllowance = GetString(settings, Keys.GroupHomeBagAllowance, string.Empty);

            result.TwilioAccountSid = GetString(settings, Keys.TwilioAccountSid, string.Empty);
            result.TwilioAuthToken = GetString(settings, Keys.TwilioAuthToken, string.Empty);
            result.TwilioPhoneNumber = GetString(settings, Keys.TwilioPhoneNumber, string.Empty);
            result.ClientOrderPage = GetString(settings, Keys.ClientOrderPage, string.Empty);

            result.ShowXmasToys = GetBool(settings, Keys.ShowXmasToys, false);
            result.XmasToysYear = GetString(settings, Keys.XmasToysYear, string.Empty);
            result.XmasRequireSizeAgeRange = GetString(settings, Keys.XmasRequireSizeAgeRange, string.Empty);
            result.ShowXmasGiftFields = GetBool(settings, Keys.ShowXmasGiftFields, false);
            result.ToyTicketContent = GetString(settings, Keys.ToyTicketContent, string.Empty);

            result.IncomeEligibilityGuidelines = GetString(settings, Keys.IncomeEligibilityGuidelines, string.Empty);

            result.ReqPrimaryIncomeSource = GetBool(settings, Keys.ReqPrimaryIncomeSource, false);
            result.ReqZipCode = GetBool(settings, Keys.ReqZipCode, true);
            result.ReqGender = GetBool(settings, Keys.ReqGender, false);
            result.ReqEthnicity = GetBool(settings, Keys.ReqEthnicity, false);
            result.ReqAFMVerified = GetBool(settings, Keys.ReqAFMVerified, false);

            result.ClientManagerUserRole = GetString(settings, Keys.ClientManagerUserRole, string.Empty);
            result.ClientManagerDeleteRecordRole = GetString(settings, Keys.ClientManagerDeleteRecordRole, string.Empty);
            result.Recaptcha = GetString(settings, Keys.Recaptcha, string.Empty);

            return result;
        }

        public void ApplyToDictionary(Dictionary<string, string> settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings[Keys.FBName] = FBName ?? string.Empty;
            settings[Keys.FBAddress] = FBAddress ?? string.Empty;
            settings[Keys.FBCity] = FBCity ?? string.Empty;
            settings[Keys.FBState] = FBState ?? string.Empty;
            settings[Keys.FBZipCode] = FBZipCode ?? string.Empty;
            settings[Keys.FBPhoneNumber] = FBPhoneNumber ?? string.Empty;
            settings[Keys.FBFaxNumber] = FBFaxNumber ?? string.Empty;
            settings[Keys.AllowedIPAddress] = AllowedIPAddress ?? string.Empty;
            settings[Keys.GoogleAPIKey] = GoogleAPIKey ?? string.Empty;
            settings[Keys.GoogleGeocodeAPIKey] = GoogleGeocodeAPIKey ?? string.Empty;
            settings[Keys.EnableGeocodeOnSave] = EnableGeocodeOnSave.ToString();

            settings[Keys.IDCardImagePath] = IDCardImagePath ?? string.Empty;
            settings[Keys.DaysToValidVisit] = string.IsNullOrWhiteSpace(DaysToValidVisit) ? "1" : DaysToValidVisit;
            settings[Keys.ShowRequiredFields] = ShowRequiredFields.ToString();
            settings[Keys.ShowPrimaryIncomeSource] = ShowPrimaryIncomeSource.ToString();
            settings[Keys.ShowSendText] = ShowSendText.ToString();
            settings[Keys.ShowClientServiceLocation] = ShowClientServiceLocation.ToString();
            settings[Keys.ShowSuffix] = ShowSuffix.ToString();
            settings[Keys.ShowRelationshipToClient] = ShowRelationshipToClient.ToString();
            settings[Keys.ReqAFMRelationship] = ShowRelationshipToClient.ToString();
            settings[Keys.ShowEthnicity] = ShowEthnicity.ToString();
            settings[Keys.ShowPhotoID] = ShowPhotoID.ToString();
            settings[Keys.ShowOneBagOnly] = ShowOneBagOnly.ToString();
            settings[Keys.ShowVisitStopLight] = ShowVisitStopLight.ToString();
            settings[Keys.ShowIncExpSummary] = ShowIncExpSummary.ToString();
            settings[Keys.ShowExpense] = ShowExpense.ToString();
            settings[Keys.ShowDisabilities] = ShowDisabilities.ToString();
            settings[Keys.ShowClientType] = ShowClientType.ToString();
            settings[Keys.ShowClientIdCard] = ShowClientIdCard.ToString();
            settings[Keys.ShowPrintShoppingLabel] = ShowPrintShoppingLabel.ToString();
            settings[Keys.PrintShoppingLabelQuantity] = PrintShoppingLabelQuantity ?? string.Empty;
            settings[Keys.ShowPrintBarCodeLabel] = ShowPrintBarCodeLabel.ToString();
            settings[Keys.IncludeInactiveOnSearch] = IncludeInactiveOnSearch.ToString();
            settings[Keys.FlagForReviewNotify] = FlagForReviewNotify ?? string.Empty;
            settings[Keys.FocusableControl] = FocusableControl ?? string.Empty;
            settings[Keys.BagAllowance] = BagAllowance ?? string.Empty;
            settings[Keys.GroupHomeBagAllowance] = GroupHomeBagAllowance ?? string.Empty;

            settings[Keys.TwilioAccountSid] = TwilioAccountSid ?? string.Empty;
            settings[Keys.TwilioAuthToken] = TwilioAuthToken ?? string.Empty;
            settings[Keys.TwilioPhoneNumber] = TwilioPhoneNumber ?? string.Empty;
            settings[Keys.ClientOrderPage] = ClientOrderPage ?? string.Empty;

            settings[Keys.ShowXmasToys] = ShowXmasToys.ToString();
            settings[Keys.XmasToysYear] = XmasToysYear ?? string.Empty;
            settings[Keys.XmasRequireSizeAgeRange] = XmasRequireSizeAgeRange ?? string.Empty;
            settings[Keys.ShowXmasGiftFields] = ShowXmasGiftFields.ToString();
            settings[Keys.ToyTicketContent] = ToyTicketContent ?? string.Empty;

            settings[Keys.IncomeEligibilityGuidelines] = IncomeEligibilityGuidelines ?? string.Empty;

            settings[Keys.ReqPrimaryIncomeSource] = ReqPrimaryIncomeSource.ToString();
            settings[Keys.ReqZipCode] = ReqZipCode.ToString();
            settings[Keys.ReqGender] = ReqGender.ToString();
            settings[Keys.ReqEthnicity] = ReqEthnicity.ToString();
            settings[Keys.ReqAFMVerified] = ReqAFMVerified.ToString();

            settings[Keys.ClientManagerUserRole] = ClientManagerUserRole ?? string.Empty;
            settings[Keys.ClientManagerDeleteRecordRole] = ClientManagerDeleteRecordRole ?? string.Empty;
        }

        private static string GetString(IDictionary<string, string> settings, string key, string defaultValue)
        {
            if (settings.TryGetValue(key, out var value) && value != null)
            {
                return value;
            }

            foreach (var kvp in settings)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                {
                    return kvp.Value;
                }
            }

            return defaultValue;
        }

        private static bool GetBool(IDictionary<string, string> settings, string key, bool defaultValue)
        {
            var raw = GetString(settings, key, string.Empty);
            if (bool.TryParse(raw, out var parsed))
            {
                return parsed;
            }

            return defaultValue;
        }
    }
}
