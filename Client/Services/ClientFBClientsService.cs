using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Oqtane.Services;
using Oqtane.Shared;

namespace GIBS.Module.FBClients.Services
{

    public class ClientFBClientsService : ServiceBase, IFBClientsService
    {
        public ClientFBClientsService(HttpClient http, SiteState siteState) : base(http, siteState) { }

        private string Apiurl => CreateApiUrl("FBClients");

        public async Task<List<Models.FBClients>> GetFBClientssAsync(int ModuleId)
        {
            List<Models.FBClients> FBClientss = await GetJsonAsync<List<Models.FBClients>>(CreateAuthorizationPolicyUrl($"{Apiurl}?moduleid={ModuleId}", EntityNames.Module, ModuleId), Enumerable.Empty<Models.FBClients>().ToList());
            return FBClientss.OrderBy(item => item.ClientLastName).ToList();
        }

        public async Task<Models.FBClients> GetFBClientsAsync(int FBClientsId, int ModuleId)
        {
            return await GetJsonAsync<Models.FBClients>(CreateAuthorizationPolicyUrl($"{Apiurl}/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<Models.FBClients> AddFBClientsAsync(Models.FBClients FBClients)
        {
            return await PostJsonAsync<Models.FBClients>(CreateAuthorizationPolicyUrl($"{Apiurl}", EntityNames.Module, FBClients.ModuleId), FBClients);
        }

        public async Task<Models.FBClients> UpdateFBClientsAsync(Models.FBClients FBClients)
        {
            return await PutJsonAsync<Models.FBClients>(CreateAuthorizationPolicyUrl($"{Apiurl}/{FBClients.FBClientsId}", EntityNames.Module, FBClients.ModuleId), FBClients);
        }

        public async Task DeleteFBClientsAsync(int FBClientsId, int ModuleId)
        {
            await DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId)); 
        }

        public async Task<List<Models.FBClientsAddFamMem>> GetFBClientsAddFamMemsAsync(int FBClientsId, int ModuleId)
        {
            return await GetJsonAsync<List<Models.FBClientsAddFamMem>>(CreateAuthorizationPolicyUrl($"{Apiurl}/afm/list/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId), Enumerable.Empty<Models.FBClientsAddFamMem>().ToList());
        }

        public async Task<Models.FBClientsAddFamMem> GetFBClientsAddFamMemAsync(int FBClientsAddFamMemId, int ModuleId)
        {
            return await GetJsonAsync<Models.FBClientsAddFamMem>(CreateAuthorizationPolicyUrl($"{Apiurl}/afm/{FBClientsAddFamMemId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<Models.FBClientsAddFamMem> AddFBClientsAddFamMemAsync(Models.FBClientsAddFamMem FBClientsAddFamMem, int ModuleId)
        {
            return await PostJsonAsync<Models.FBClientsAddFamMem>(CreateAuthorizationPolicyUrl($"{Apiurl}/afm/{ModuleId}", EntityNames.Module, ModuleId), FBClientsAddFamMem);
        }

        public async Task<Models.FBClientsAddFamMem> UpdateFBClientsAddFamMemAsync(Models.FBClientsAddFamMem FBClientsAddFamMem, int ModuleId)
        {
            return await PutJsonAsync<Models.FBClientsAddFamMem>(CreateAuthorizationPolicyUrl($"{Apiurl}/afm/{FBClientsAddFamMem.FBClientsAddFamMemId}/{ModuleId}", EntityNames.Module, ModuleId), FBClientsAddFamMem);
        }

        public async Task DeleteFBClientsAddFamMemAsync(int FBClientsAddFamMemId, int ModuleId)
        {
            await DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/afm/{FBClientsAddFamMemId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<List<Models.FBClientsIncomeAndExpense>> GetFBClientsIncomeAndExpensesAsync(int FBClientsId, int ModuleId)
        {
            return await GetJsonAsync<List<Models.FBClientsIncomeAndExpense>>(CreateAuthorizationPolicyUrl($"{Apiurl}/ie/list/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId), Enumerable.Empty<Models.FBClientsIncomeAndExpense>().ToList());
        }

        public async Task<Models.FBClientsIncomeAndExpense> GetFBClientsIncomeAndExpenseAsync(int IeId, int ModuleId)
        {
            return await GetJsonAsync<Models.FBClientsIncomeAndExpense>(CreateAuthorizationPolicyUrl($"{Apiurl}/ie/{IeId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<Models.FBClientsIncomeAndExpense> AddFBClientsIncomeAndExpenseAsync(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense, int ModuleId)
        {
            return await PostJsonAsync<Models.FBClientsIncomeAndExpense>(CreateAuthorizationPolicyUrl($"{Apiurl}/ie/{ModuleId}", EntityNames.Module, ModuleId), FBClientsIncomeAndExpense);
        }

        public async Task<Models.FBClientsIncomeAndExpense> UpdateFBClientsIncomeAndExpenseAsync(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense, int ModuleId)
        {
            return await PutJsonAsync<Models.FBClientsIncomeAndExpense>(CreateAuthorizationPolicyUrl($"{Apiurl}/ie/{FBClientsIncomeAndExpense.IeId}/{ModuleId}", EntityNames.Module, ModuleId), FBClientsIncomeAndExpense);
        }

        public async Task DeleteFBClientsIncomeAndExpenseAsync(int IeId, int ModuleId)
        {
            await DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/ie/{IeId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<List<Models.FBClientsVisits>> GetFBClientsVisitsAsync(int FBClientsId, int ModuleId)
        {
            return await GetJsonAsync<List<Models.FBClientsVisits>>(CreateAuthorizationPolicyUrl($"{Apiurl}/visit/list/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId), Enumerable.Empty<Models.FBClientsVisits>().ToList());
        }

        public async Task<Models.FBClientsVisits> GetFBClientsVisitAsync(int VisitId, int ModuleId)
        {
            return await GetJsonAsync<Models.FBClientsVisits>(CreateAuthorizationPolicyUrl($"{Apiurl}/visit/{VisitId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<Models.FBClientsVisits> AddFBClientsVisitAsync(Models.FBClientsVisits FBClientsVisit, int ModuleId)
        {
            return await PostJsonAsync<Models.FBClientsVisits>(CreateAuthorizationPolicyUrl($"{Apiurl}/visit/{ModuleId}", EntityNames.Module, ModuleId), FBClientsVisit);
        }

        public async Task<Models.FBClientsVisits> UpdateFBClientsVisitAsync(Models.FBClientsVisits FBClientsVisit, int ModuleId)
        {
            return await PutJsonAsync<Models.FBClientsVisits>(CreateAuthorizationPolicyUrl($"{Apiurl}/visit/{FBClientsVisit.VisitId}/{ModuleId}", EntityNames.Module, ModuleId), FBClientsVisit);
        }

        public async Task DeleteFBClientsVisitAsync(int VisitId, int ModuleId)
        {
            await DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/visit/{VisitId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<List<Models.FBClientsChristmas>> GetFBClientsChristmasesAsync(int FBClientsId, int ModuleId)
        {
            return await GetJsonAsync<List<Models.FBClientsChristmas>>(CreateAuthorizationPolicyUrl($"{Apiurl}/christmas/list/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId), Enumerable.Empty<Models.FBClientsChristmas>().ToList());
        }

        public async Task<Models.FBClientsChristmas> GetFBClientsChristmasAsync(int xMasId, int ModuleId)
        {
            return await GetJsonAsync<Models.FBClientsChristmas>(CreateAuthorizationPolicyUrl($"{Apiurl}/christmas/{xMasId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<Models.FBClientsChristmas> AddFBClientsChristmasAsync(Models.FBClientsChristmas FBClientsChristmas, int ModuleId)
        {
            return await PostJsonAsync<Models.FBClientsChristmas>(CreateAuthorizationPolicyUrl($"{Apiurl}/christmas/{ModuleId}", EntityNames.Module, ModuleId), FBClientsChristmas);
        }

        public async Task<Models.FBClientsChristmas> UpdateFBClientsChristmasAsync(Models.FBClientsChristmas FBClientsChristmas, int ModuleId)
        {
            return await PutJsonAsync<Models.FBClientsChristmas>(CreateAuthorizationPolicyUrl($"{Apiurl}/christmas/{FBClientsChristmas.xMasId}/{ModuleId}", EntityNames.Module, ModuleId), FBClientsChristmas);
        }

        public async Task DeleteFBClientsChristmasAsync(int xMasId, int ModuleId)
        {
            await DeleteAsync(CreateAuthorizationPolicyUrl($"{Apiurl}/christmas/{xMasId}/{ModuleId}", EntityNames.Module, ModuleId));
        }

        public async Task<List<Models.FBClientsTrueFalseQuestions>> GetFBClientsTrueFalseQuestionsAsync(int FBClientsId, int ModuleId)
        {
            return await GetJsonAsync<List<Models.FBClientsTrueFalseQuestions>>(CreateAuthorizationPolicyUrl($"{Apiurl}/tf/list/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId), Enumerable.Empty<Models.FBClientsTrueFalseQuestions>().ToList());
        }

        public async Task<Models.FBClientsTrueFalseQuestions> SaveFBClientsTrueFalseQuestionAsync(Models.FBClientsTrueFalseQuestions FBClientsTrueFalseQuestion, int ModuleId)
        {
            return await PostJsonAsync<Models.FBClientsTrueFalseQuestions>(CreateAuthorizationPolicyUrl($"{Apiurl}/tf/{ModuleId}", EntityNames.Module, ModuleId), FBClientsTrueFalseQuestion);
        }

        public async Task<List<Models.FBClientsIDPhoto>> GetFBClientsIDPhotosAsync(int FBClientsId, int ModuleId)
        {
            return await GetJsonAsync<List<Models.FBClientsIDPhoto>>(CreateAuthorizationPolicyUrl($"{Apiurl}/photo/list/{FBClientsId}/{ModuleId}", EntityNames.Module, ModuleId), Enumerable.Empty<Models.FBClientsIDPhoto>().ToList());
        }

        public async Task<Models.FBClientsIDPhoto> SaveFBClientsIDPhotoAsync(Models.FBClientsIDPhoto FBClientsIDPhoto, int ModuleId)
        {
            return await PostJsonAsync<Models.FBClientsIDPhoto>(CreateAuthorizationPolicyUrl($"{Apiurl}/photo/{ModuleId}", EntityNames.Module, ModuleId), FBClientsIDPhoto);
        }

        public async Task<List<Models.FBClientsSearchResult>> SearchFBClientsAsync(Models.FBClientsSearchRequest request)
        {
            return await PostJsonAsync<Models.FBClientsSearchRequest, List<Models.FBClientsSearchResult>>(CreateAuthorizationPolicyUrl($"{Apiurl}/search/{request.ModuleId}", EntityNames.Module, request.ModuleId), request);
        }
    }
}
