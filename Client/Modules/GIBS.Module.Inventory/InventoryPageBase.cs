using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GIBS.Module.FBClients.Inventory.Services;
using GIBS.Module.FBClients.Lists.Services;
using Microsoft.AspNetCore.Components;
using Oqtane.Enums;
using Oqtane.Modules;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Inventory
{
    public abstract class InventoryPageBase : ModuleBase
    {
        [Inject] protected IInventoryService Inventory { get; set; }
        [Inject] protected IListsService Lists { get; set; }
        protected bool Busy { get; private set; }
        protected bool CanEdit => SignedIn && IsAuthorizedPermission(PermissionNames.Edit);
        protected bool SignedIn => PageState.User?.IsAuthenticated == true;
        protected int InventoryId => ModuleState.ModuleId;
        public override bool? Prerender => false;

        protected async Task RunAsync(Func<Task> action)
        {
            if (Busy) return;
            if (!SignedIn)
            {
                AddModuleMessage("Sign in to access the inventory.", MessageType.Warning);
                return;
            }
            Busy = true;
            try
            {
                await action();
            }
            catch (ValidationException ex)
            {
                AddModuleMessage(ex.Message, MessageType.Warning);
            }
            catch (KeyNotFoundException ex)
            {
                AddModuleMessage(ex.Message, MessageType.Warning);
            }
            catch (InventorySaveException ex)
            {
                await logger.LogWarning(LogFunction.Update, "Inventory update failed in module {ModuleId}: {ErrorType}", InventoryId, ex.InnerException.GetType().Name);
                AddModuleMessage(ex.Message, MessageType.Warning);
            }
            catch (UnauthorizedAccessException)
            {
                AddModuleMessage("You do not have permission to perform this inventory action.", MessageType.Warning);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized)
            {
                AddModuleMessage("You do not have permission to perform this inventory action.", MessageType.Warning);
            }
            catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
            {
                await logger.LogError(LogFunction.Other, "Inventory operation failed in module {ModuleId}: {ErrorType}", InventoryId, ex.GetType().Name);
                AddModuleMessage("The inventory operation failed. Reload the page before retrying.", MessageType.Error);
            }
            finally
            {
                Busy = false;
            }
        }

        protected async Task<IReadOnlyList<string>> LoadSuggestionsAsync(string listName, int maxLength, string defaultValue)
        {
            try
            {
                var entries = await Lists.GetListsAsync(listName, ModuleState.SiteId);
                var values = entries.Where(x => x.ParentID == 0 && !string.IsNullOrWhiteSpace(x.Value) && x.Value.Length <= maxLength)
                    .Select(x => x.Value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                return values.Count == 0 ? new[] { defaultValue } : values;
            }
            catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                await logger.LogWarning(LogFunction.Read, "Unable to load inventory suggestions from {ListName}: {ErrorType}", listName, ex.GetType().Name);
                AddModuleMessage($"Suggestions from {listName} are unavailable. You can enter a value manually.", MessageType.Warning);
                return Array.Empty<string>();
            }
        }

        protected static string DisplayName(string value, int? id = null) => string.IsNullOrWhiteSpace(value)
            ? (id.HasValue ? $"#{id}" : "Not set") : value;
    }
}
