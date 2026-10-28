using System.Collections.Generic;
using System.Threading.Tasks;

namespace GIBS.Module.FBClients.Services
{
    public interface IFBClientsService 
    {
        Task<List<Models.FBClients>> GetFBClientssAsync(int ModuleId);

        Task<Models.FBClients> GetFBClientsAsync(int FBClientsId, int ModuleId);

        Task<Models.FBClients> AddFBClientsAsync(Models.FBClients FBClients);

        Task<Models.FBClients> UpdateFBClientsAsync(Models.FBClients FBClients);

        Task DeleteFBClientsAsync(int FBClientsId, int ModuleId);

        Task<List<Models.FBClientsAddFamMem>> GetFBClientsAddFamMemsAsync(int FBClientsId, int ModuleId);
        Task<Models.FBClientsAddFamMem> GetFBClientsAddFamMemAsync(int FBClientsAddFamMemId, int ModuleId);
        Task<Models.FBClientsAddFamMem> AddFBClientsAddFamMemAsync(Models.FBClientsAddFamMem FBClientsAddFamMem, int ModuleId);
        Task<Models.FBClientsAddFamMem> UpdateFBClientsAddFamMemAsync(Models.FBClientsAddFamMem FBClientsAddFamMem, int ModuleId);
        Task DeleteFBClientsAddFamMemAsync(int FBClientsAddFamMemId, int ModuleId);

        Task<List<Models.FBClientsIncomeAndExpense>> GetFBClientsIncomeAndExpensesAsync(int FBClientsId, int ModuleId);
        Task<Models.FBClientsIncomeAndExpense> GetFBClientsIncomeAndExpenseAsync(int IeId, int ModuleId);
        Task<Models.FBClientsIncomeAndExpense> AddFBClientsIncomeAndExpenseAsync(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense, int ModuleId);
        Task<Models.FBClientsIncomeAndExpense> UpdateFBClientsIncomeAndExpenseAsync(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense, int ModuleId);
        Task DeleteFBClientsIncomeAndExpenseAsync(int IeId, int ModuleId);

        Task<List<Models.FBClientsVisits>> GetFBClientsVisitsAsync(int FBClientsId, int ModuleId);
        Task<Models.FBClientsVisits> GetFBClientsVisitAsync(int VisitId, int ModuleId);
        Task<Models.FBClientsVisits> AddFBClientsVisitAsync(Models.FBClientsVisits FBClientsVisit, int ModuleId);
        Task<Models.FBClientsVisits> UpdateFBClientsVisitAsync(Models.FBClientsVisits FBClientsVisit, int ModuleId);
        Task DeleteFBClientsVisitAsync(int VisitId, int ModuleId);
        Task<Models.FBClientsOrderTextResult> SendFBClientsOrderTextAsync(int VisitId, int ModuleId);

        Task<List<Models.FBClientsChristmas>> GetFBClientsChristmasesAsync(int FBClientsId, int ModuleId);
        Task<Models.FBClientsChristmas> GetFBClientsChristmasAsync(int xMasId, int ModuleId);
        Task<Models.FBClientsChristmas> AddFBClientsChristmasAsync(Models.FBClientsChristmas FBClientsChristmas, int ModuleId);
        Task<Models.FBClientsChristmas> UpdateFBClientsChristmasAsync(Models.FBClientsChristmas FBClientsChristmas, int ModuleId);
        Task DeleteFBClientsChristmasAsync(int xMasId, int ModuleId);

        Task<List<Models.FBClientsTrueFalseQuestions>> GetFBClientsTrueFalseQuestionsAsync(int FBClientsId, int ModuleId);
        Task<Models.FBClientsTrueFalseQuestions> SaveFBClientsTrueFalseQuestionAsync(Models.FBClientsTrueFalseQuestions FBClientsTrueFalseQuestion, int ModuleId);

        Task<List<Models.FBClientsIDPhoto>> GetFBClientsIDPhotosAsync(int FBClientsId, int ModuleId);
        Task<Models.FBClientsIDPhoto> SaveFBClientsIDPhotoAsync(Models.FBClientsIDPhoto FBClientsIDPhoto, int ModuleId);

        Task<List<Models.FBClientsSearchResult>> SearchFBClientsAsync(Models.FBClientsSearchRequest request);
    }
}
