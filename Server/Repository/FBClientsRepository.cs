using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using Oqtane.Modules;

namespace GIBS.Module.FBClients.Repository
{
    public interface IFBClientsRepository
    {
        IEnumerable<Models.FBClients> GetFBClientss(int ModuleId);
        Models.FBClients GetFBClients(int FBClientsId);
        Models.FBClients GetFBClients(int FBClientsId, bool tracking);
        Models.FBClients AddFBClients(Models.FBClients FBClients);
        Models.FBClients UpdateFBClients(Models.FBClients FBClients);
        void DeleteFBClients(int FBClientsId);

        IEnumerable<Models.FBClientsAddFamMem> GetFBClientsAddFamMems(int FBClientsId);
        Models.FBClientsAddFamMem GetFBClientsAddFamMem(int FBClientsAddFamMemId);
        Models.FBClientsAddFamMem GetFBClientsAddFamMem(int FBClientsAddFamMemId, bool tracking);
        Models.FBClientsAddFamMem AddFBClientsAddFamMem(Models.FBClientsAddFamMem FBClientsAddFamMem);
        Models.FBClientsAddFamMem UpdateFBClientsAddFamMem(Models.FBClientsAddFamMem FBClientsAddFamMem);
        void DeleteFBClientsAddFamMem(int FBClientsAddFamMemId);

        IEnumerable<Models.FBClientsIncomeAndExpense> GetFBClientsIncomeAndExpenses(int FBClientsId);
        Models.FBClientsIncomeAndExpense GetFBClientsIncomeAndExpense(int IeId);
        Models.FBClientsIncomeAndExpense GetFBClientsIncomeAndExpense(int IeId, bool tracking);
        Models.FBClientsIncomeAndExpense AddFBClientsIncomeAndExpense(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense);
        Models.FBClientsIncomeAndExpense UpdateFBClientsIncomeAndExpense(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense);
        void DeleteFBClientsIncomeAndExpense(int IeId);

        IEnumerable<Models.FBClientsVisits> GetFBClientsVisits(int FBClientsId);
        Models.FBClientsVisits GetFBClientsVisit(int VisitId);
        Models.FBClientsVisits GetFBClientsVisit(int VisitId, bool tracking);
        Models.FBClientsVisits AddFBClientsVisit(Models.FBClientsVisits FBClientsVisit);
        Models.FBClientsVisits UpdateFBClientsVisit(Models.FBClientsVisits FBClientsVisit);
        void DeleteFBClientsVisit(int VisitId);
        bool MarkVisitOrderLinkSent(int VisitId);

        IEnumerable<Models.FBClientsChristmas> GetFBClientsChristmases(int FBClientsId);
        Models.FBClientsChristmas GetFBClientsChristmas(int xMasId);
        Models.FBClientsChristmas GetFBClientsChristmas(int xMasId, bool tracking);
        Models.FBClientsChristmas AddFBClientsChristmas(Models.FBClientsChristmas FBClientsChristmas);
        Models.FBClientsChristmas UpdateFBClientsChristmas(Models.FBClientsChristmas FBClientsChristmas);
        void DeleteFBClientsChristmas(int xMasId);

        IEnumerable<Models.FBClientsTrueFalseQuestions> GetFBClientsTrueFalseQuestions(int FBClientsId);
        Models.FBClientsTrueFalseQuestions SaveFBClientsTrueFalseQuestion(Models.FBClientsTrueFalseQuestions FBClientsTrueFalseQuestion);

        IEnumerable<Models.FBClientsIDPhoto> GetFBClientsIDPhotos(int FBClientsId);
        Models.FBClientsIDPhoto SaveFBClientsIDPhoto(Models.FBClientsIDPhoto FBClientsIDPhoto);

        IEnumerable<Models.FBClientsSearchResult> SearchFBClients(Models.FBClientsSearchRequest request);
    }

    public class FBClientsRepository : IFBClientsRepository, ITransientService
    {
        private readonly IDbContextFactory<FBClientsContext> _factory;

        public FBClientsRepository(IDbContextFactory<FBClientsContext> factory)
        {
            _factory = factory;
        }

        public IEnumerable<Models.FBClients> GetFBClientss(int ModuleId)
        {
            using var db = _factory.CreateDbContext();
            return db.FBClients
		.Where(item => item.ModuleId == ModuleId)
		.AsNoTracking()
		.ToList();
        }

        public Models.FBClients GetFBClients(int FBClientsId)
        {
            return GetFBClients(FBClientsId, false);
        }

        public Models.FBClients GetFBClients(int FBClientsId, bool tracking)
        {
            using var db = _factory.CreateDbContext();
            if (tracking)
            {
                return db.FBClients
		    .Find(FBClientsId);
            }
            else
            {
                return db.FBClients
		    .AsNoTracking()
		    .FirstOrDefault(item => item.FBClientsId == FBClientsId);
            }
        }

        public Models.FBClients AddFBClients(Models.FBClients FBClients)
        {
            using var db = _factory.CreateDbContext();
            db.FBClients.Add(FBClients);
            db.SaveChanges();
            return FBClients;
        }

        public Models.FBClients UpdateFBClients(Models.FBClients FBClients)
        {
            using var db = _factory.CreateDbContext();
            db.Entry(FBClients).State = EntityState.Modified;
            db.SaveChanges();
            return FBClients;
        }

        public void DeleteFBClients(int FBClientsId)
        {
            using var db = _factory.CreateDbContext();
            Models.FBClients FBClients = db.FBClients.Find(FBClientsId);
            if (FBClients != null)
            {
                db.FBClients.Remove(FBClients);
                db.SaveChanges();
            }
        }

        public IEnumerable<Models.FBClientsAddFamMem> GetFBClientsAddFamMems(int FBClientsId)
        {
            using var db = _factory.CreateDbContext();
            return db.FBClientsAddFamMem.AsNoTracking()
                .Where(item => item.FBClientsId == FBClientsId)
                .OrderByDescending(item => item.ClAddFamMemDOB)
                .ThenBy(item => item.ClAddFamMemFirstName)
                .ToList();
        }

        public Models.FBClientsAddFamMem GetFBClientsAddFamMem(int FBClientsAddFamMemId)
        {
            return GetFBClientsAddFamMem(FBClientsAddFamMemId, true);
        }

        public Models.FBClientsAddFamMem GetFBClientsAddFamMem(int FBClientsAddFamMemId, bool tracking)
        {
            using var db = _factory.CreateDbContext();
            if (tracking)
            {
                return db.FBClientsAddFamMem.Find(FBClientsAddFamMemId);
            }
            else
            {
                return db.FBClientsAddFamMem.AsNoTracking().FirstOrDefault(item => item.FBClientsAddFamMemId == FBClientsAddFamMemId);
            }
        }

        public Models.FBClientsAddFamMem AddFBClientsAddFamMem(Models.FBClientsAddFamMem FBClientsAddFamMem)
        {
            using var db = _factory.CreateDbContext();
            db.FBClientsAddFamMem.Add(FBClientsAddFamMem);
            db.SaveChanges();
            return FBClientsAddFamMem;
        }

        public Models.FBClientsAddFamMem UpdateFBClientsAddFamMem(Models.FBClientsAddFamMem FBClientsAddFamMem)
        {
            using var db = _factory.CreateDbContext();
            db.Entry(FBClientsAddFamMem).State = EntityState.Modified;
            db.SaveChanges();
            return FBClientsAddFamMem;
        }

        public void DeleteFBClientsAddFamMem(int FBClientsAddFamMemId)
        {
            using var db = _factory.CreateDbContext();
            Models.FBClientsAddFamMem afm = db.FBClientsAddFamMem.Find(FBClientsAddFamMemId);
            if (afm != null)
            {
                var christmasRecords = db.FBClientsChristmas
                    .Where(item => item.FBClientsAddFamMemId == FBClientsAddFamMemId)
                    .ToList();
                foreach (var record in christmasRecords)
                {
                    record.FBClientsAddFamMemId = null;
                }
                db.FBClientsAddFamMem.Remove(afm);
                db.SaveChanges();
            }
        }

        public IEnumerable<Models.FBClientsIncomeAndExpense> GetFBClientsIncomeAndExpenses(int FBClientsId)
        {
            using var db = _factory.CreateDbContext();
            return db.FBClientsIncomeAndExpenses.AsNoTracking()
                .Where(item => item.FBClientsId == FBClientsId)
                .OrderBy(item => item.IeType)
                .ThenBy(item => item.IeDescription)
                .ToList();
        }

        public Models.FBClientsIncomeAndExpense GetFBClientsIncomeAndExpense(int IeId)
        {
            return GetFBClientsIncomeAndExpense(IeId, true);
        }

        public Models.FBClientsIncomeAndExpense GetFBClientsIncomeAndExpense(int IeId, bool tracking)
        {
            using var db = _factory.CreateDbContext();
            if (tracking)
            {
                return db.FBClientsIncomeAndExpenses.Find(IeId);
            }
            else
            {
                return db.FBClientsIncomeAndExpenses.AsNoTracking().FirstOrDefault(item => item.IeId == IeId);
            }
        }

        public Models.FBClientsIncomeAndExpense AddFBClientsIncomeAndExpense(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense)
        {
            using var db = _factory.CreateDbContext();
            db.FBClientsIncomeAndExpenses.Add(FBClientsIncomeAndExpense);
            db.SaveChanges();
            return FBClientsIncomeAndExpense;
        }

        public Models.FBClientsIncomeAndExpense UpdateFBClientsIncomeAndExpense(Models.FBClientsIncomeAndExpense FBClientsIncomeAndExpense)
        {
            using var db = _factory.CreateDbContext();
            db.Entry(FBClientsIncomeAndExpense).State = EntityState.Modified;
            db.SaveChanges();
            return FBClientsIncomeAndExpense;
        }

        public void DeleteFBClientsIncomeAndExpense(int IeId)
        {
            using var db = _factory.CreateDbContext();
            Models.FBClientsIncomeAndExpense item = db.FBClientsIncomeAndExpenses.Find(IeId);
            if (item != null)
            {
                db.FBClientsIncomeAndExpenses.Remove(item);
                db.SaveChanges();
            }
        }

        public IEnumerable<Models.FBClientsVisits> GetFBClientsVisits(int FBClientsId)
        {
            using var db = _factory.CreateDbContext();
            return db.FBClientsVisits.AsNoTracking()
                .Where(item => item.FBClientsId == FBClientsId)
                .OrderByDescending(item => item.VisitDate)
                .ThenByDescending(item => item.VisitId)
                .ToList();
        }

        public Models.FBClientsVisits GetFBClientsVisit(int VisitId)
        {
            return GetFBClientsVisit(VisitId, true);
        }

        public Models.FBClientsVisits GetFBClientsVisit(int VisitId, bool tracking)
        {
            using var db = _factory.CreateDbContext();
            if (tracking)
            {
                return db.FBClientsVisits.Find(VisitId);
            }
            else
            {
                return db.FBClientsVisits.AsNoTracking().FirstOrDefault(item => item.VisitId == VisitId);
            }
        }

        public Models.FBClientsVisits AddFBClientsVisit(Models.FBClientsVisits FBClientsVisit)
        {
            using var db = _factory.CreateDbContext();
            db.FBClientsVisits.Add(FBClientsVisit);
            db.SaveChanges();
            return FBClientsVisit;
        }

        public Models.FBClientsVisits UpdateFBClientsVisit(Models.FBClientsVisits FBClientsVisit)
        {
            using var db = _factory.CreateDbContext();
            db.Entry(FBClientsVisit).State = EntityState.Modified;
            db.SaveChanges();
            return FBClientsVisit;
        }

        public bool MarkVisitOrderLinkSent(int VisitId)
        {
            using var db = _factory.CreateDbContext();
            return db.FBClientsVisits
                .Where(visit => visit.VisitId == VisitId && (visit.OrderStatusCode == null || visit.OrderStatusCode == 0))
                .ExecuteUpdate(update => update.SetProperty(visit => visit.OrderStatusCode, (int?)1)) == 1;
        }

        public void DeleteFBClientsVisit(int VisitId)
        {
            using var db = _factory.CreateDbContext();
            Models.FBClientsVisits visit = db.FBClientsVisits.Find(VisitId);
            if (visit != null)
            {
                db.FBClientsVisits.Remove(visit);
                db.SaveChanges();
            }
        }

        public IEnumerable<Models.FBClientsChristmas> GetFBClientsChristmases(int FBClientsId)
        {
            using var db = _factory.CreateDbContext();
            return db.FBClientsChristmas.AsNoTracking()
                .Where(item => item.FBClientsId == FBClientsId)
                .OrderByDescending(item => item.xMasYear)
                .ThenByDescending(item => item.xMasId)
                .ToList();
        }

        public Models.FBClientsChristmas GetFBClientsChristmas(int xMasId)
        {
            return GetFBClientsChristmas(xMasId, true);
        }

        public Models.FBClientsChristmas GetFBClientsChristmas(int xMasId, bool tracking)
        {
            using var db = _factory.CreateDbContext();
            if (tracking)
            {
                return db.FBClientsChristmas.Find(xMasId);
            }
            else
            {
                return db.FBClientsChristmas.AsNoTracking().FirstOrDefault(item => item.xMasId == xMasId);
            }
        }

        public Models.FBClientsChristmas AddFBClientsChristmas(Models.FBClientsChristmas FBClientsChristmas)
        {
            using var db = _factory.CreateDbContext();
            db.FBClientsChristmas.Add(FBClientsChristmas);
            db.SaveChanges();
            return FBClientsChristmas;
        }

        public Models.FBClientsChristmas UpdateFBClientsChristmas(Models.FBClientsChristmas FBClientsChristmas)
        {
            using var db = _factory.CreateDbContext();
            db.Entry(FBClientsChristmas).State = EntityState.Modified;
            db.SaveChanges();
            return FBClientsChristmas;
        }

        public void DeleteFBClientsChristmas(int xMasId)
        {
            using var db = _factory.CreateDbContext();
            Models.FBClientsChristmas record = db.FBClientsChristmas.Find(xMasId);
            if (record != null)
            {
                db.FBClientsChristmas.Remove(record);
                db.SaveChanges();
            }
        }

        public IEnumerable<Models.FBClientsTrueFalseQuestions> GetFBClientsTrueFalseQuestions(int FBClientsId)
        {
            using var db = _factory.CreateDbContext();
            return db.FBClientsTrueFalseQuestions.AsNoTracking()
                .Where(item => item.FBClientsId == FBClientsId)
                .OrderBy(item => item.TfQuestion)
                .ToList();
        }

        public Models.FBClientsTrueFalseQuestions SaveFBClientsTrueFalseQuestion(Models.FBClientsTrueFalseQuestions FBClientsTrueFalseQuestion)
        {
            using var db = _factory.CreateDbContext();
            var existing = db.FBClientsTrueFalseQuestions.FirstOrDefault(item =>
                item.FBClientsId == FBClientsTrueFalseQuestion.FBClientsId &&
                item.TfQuestion == FBClientsTrueFalseQuestion.TfQuestion);
            if (existing != null)
            {
                existing.TfAnswer = FBClientsTrueFalseQuestion.TfAnswer;
                existing.ModifiedBy = FBClientsTrueFalseQuestion.ModifiedBy;
                existing.ModifiedOn = FBClientsTrueFalseQuestion.ModifiedOn;
                db.SaveChanges();
                return existing;
            }
            else
            {
                db.FBClientsTrueFalseQuestions.Add(FBClientsTrueFalseQuestion);
                db.SaveChanges();
                return FBClientsTrueFalseQuestion;
            }
        }

        public IEnumerable<Models.FBClientsIDPhoto> GetFBClientsIDPhotos(int FBClientsId)
        {
            using var db = _factory.CreateDbContext();
            return db.FBClientsIDPhotos.AsNoTracking()
                .Where(item => item.FBClientsId == FBClientsId)
                .OrderByDescending(item => item.ImageId)
                .ToList();
        }

        public Models.FBClientsIDPhoto SaveFBClientsIDPhoto(Models.FBClientsIDPhoto FBClientsIDPhoto)
        {
            using var db = _factory.CreateDbContext();
            var existing = db.FBClientsIDPhotos
                .OrderByDescending(item => item.ImageId)
                .FirstOrDefault(item => item.FBClientsId == FBClientsIDPhoto.FBClientsId);
            if (existing != null)
            {
                existing.IDPhoto = FBClientsIDPhoto.IDPhoto;
                existing.ModifiedBy = FBClientsIDPhoto.ModifiedBy;
                existing.ModifiedOn = FBClientsIDPhoto.ModifiedOn;
                db.SaveChanges();
                return existing;
            }
            else
            {
                db.FBClientsIDPhotos.Add(FBClientsIDPhoto);
                db.SaveChanges();
                return FBClientsIDPhoto;
            }
        }

        public IEnumerable<Models.FBClientsSearchResult> SearchFBClients(Models.FBClientsSearchRequest request)
        {
            using var db = _factory.CreateDbContext();
            var query = db.FBClients.AsNoTracking()
                .Where(c => c.ModuleId == request.ModuleId);

            if (!request.IncludeInactive)
            {
                query = query.Where(c => c.IsActive == true);
            }

            if (request.IsAfmSearch)
            {
                var afmQuery = from c in query
                               join afm in db.FBClientsAddFamMem.AsNoTracking()
                                   on c.FBClientsId equals afm.FBClientsId
                               select new { c, afm };

                if (!string.IsNullOrWhiteSpace(request.AfmLastName))
                {
                    var pattern = ToLikePattern(request.AfmLastName);
                    afmQuery = afmQuery.Where(x => x.afm.ClAddFamMemLastName != null && EF.Functions.Like(x.afm.ClAddFamMemLastName, pattern));
                }

                if (!string.IsNullOrWhiteSpace(request.AfmFirstName))
                {
                    var pattern = ToLikePattern(request.AfmFirstName);
                    afmQuery = afmQuery.Where(x => x.afm.ClAddFamMemFirstName != null && EF.Functions.Like(x.afm.ClAddFamMemFirstName, pattern));
                }

                if (request.AfmDOB.HasValue)
                {
                    var dob = request.AfmDOB.Value;
                    afmQuery = afmQuery.Where(x => x.afm.ClAddFamMemDOB == dob);
                }

                return afmQuery
                    .OrderBy(x => x.afm.ClAddFamMemLastName)
                    .ThenBy(x => x.afm.ClAddFamMemFirstName)
                    .Select(x => new Models.FBClientsSearchResult
                    {
                        FBClientsId = x.c.FBClientsId,
                        LegacyClientId = x.c.LegacyClientId,
                        ClientFirstName = x.c.ClientFirstName,
                        ClientMiddleInitial = x.c.ClientMiddleInitial,
                        ClientLastName = x.c.ClientLastName,
                        ClientSuffix = x.c.ClientSuffix,
                        ClientDOB = x.c.ClientDOB,
                        ClientDOBVerify = x.c.ClientDOBVerify,
                        ClientAddress = x.c.ClientAddress,
                        ClientUnit = x.c.ClientUnit,
                        ClientCity = x.c.ClientCity,
                        ClientTown = x.c.ClientTown,
                        ClientState = x.c.ClientState,
                        ClientZipCode = x.c.ClientZipCode,
                        ClientType = x.c.ClientType,
                        IsActive = x.c.IsActive,
                        IsLocked = x.c.IsLocked,
                        ClAddFamMemFirstName = x.afm.ClAddFamMemFirstName,
                        ClAddFamMemLastName = x.afm.ClAddFamMemLastName,
                        LastVisitDate = db.FBClientsVisits
                            .Where(v => v.FBClientsId == x.c.FBClientsId)
                            .OrderByDescending(v => v.VisitDate)
                            .ThenByDescending(v => v.VisitId)
                            .Select(v => (System.DateOnly?)v.VisitDate)
                            .FirstOrDefault(),
                        VisitNumBags = db.FBClientsVisits
                            .Where(v => v.FBClientsId == x.c.FBClientsId)
                            .OrderByDescending(v => v.VisitDate)
                            .ThenByDescending(v => v.VisitId)
                            .Select(v => v.VisitNumBags)
                            .FirstOrDefault()
                    })
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.ClientId))
            {
                var trimmedId = request.ClientId.Trim().TrimStart('0');
                if (int.TryParse(trimmedId, out var parsedId))
                {
                    query = query.Where(c => c.FBClientsId == parsedId || c.LegacyClientId == parsedId);
                }
                else
                {
                    return Enumerable.Empty<Models.FBClientsSearchResult>();
                }
            }

            if (!string.IsNullOrWhiteSpace(request.ClientIdCard))
            {
                var idCard = request.ClientIdCard.Trim();
                query = query.Where(c => c.ClientIdCard == idCard);
            }

            if (!string.IsNullOrWhiteSpace(request.ClientLastName))
            {
                var pattern = ToLikePattern(request.ClientLastName);
                query = query.Where(c => c.ClientLastName != null && EF.Functions.Like(c.ClientLastName, pattern));
            }

            if (!string.IsNullOrWhiteSpace(request.ClientFirstName))
            {
                var pattern = ToLikePattern(request.ClientFirstName);
                query = query.Where(c => c.ClientFirstName != null && EF.Functions.Like(c.ClientFirstName, pattern));
            }

            if (!string.IsNullOrWhiteSpace(request.ClientAddress))
            {
                var pattern = ToLikePattern(request.ClientAddress);
                query = query.Where(c => c.ClientAddress != null && EF.Functions.Like(c.ClientAddress, pattern));
            }

            if (!string.IsNullOrWhiteSpace(request.ClientCity))
            {
                var city = request.ClientCity.Trim();
                query = query.Where(c => c.ClientCity == city);
            }

            if (!string.IsNullOrWhiteSpace(request.ClientType))
            {
                var clientType = request.ClientType.Trim();
                query = query.Where(c => c.ClientType == clientType);
            }

            if (request.ClientDOB.HasValue)
            {
                var dob = request.ClientDOB.Value;
                query = query.Where(c => c.ClientDOB == dob);
            }

            return query
                .OrderBy(c => c.ClientLastName)
                .ThenBy(c => c.ClientFirstName)
                .Select(c => new Models.FBClientsSearchResult
                {
                    FBClientsId = c.FBClientsId,
                    LegacyClientId = c.LegacyClientId,
                    ClientFirstName = c.ClientFirstName,
                    ClientMiddleInitial = c.ClientMiddleInitial,
                    ClientLastName = c.ClientLastName,
                    ClientSuffix = c.ClientSuffix,
                    ClientDOB = c.ClientDOB,
                    ClientDOBVerify = c.ClientDOBVerify,
                    ClientAddress = c.ClientAddress,
                    ClientUnit = c.ClientUnit,
                    ClientCity = c.ClientCity,
                    ClientTown = c.ClientTown,
                    ClientState = c.ClientState,
                    ClientZipCode = c.ClientZipCode,
                    ClientType = c.ClientType,
                    IsActive = c.IsActive,
                    IsLocked = c.IsLocked,
                    LastVisitDate = db.FBClientsVisits
                        .Where(v => v.FBClientsId == c.FBClientsId)
                        .OrderByDescending(v => v.VisitDate)
                        .ThenByDescending(v => v.VisitId)
                        .Select(v => (System.DateOnly?)v.VisitDate)
                        .FirstOrDefault(),
                    VisitNumBags = db.FBClientsVisits
                        .Where(v => v.FBClientsId == c.FBClientsId)
                        .OrderByDescending(v => v.VisitDate)
                        .ThenByDescending(v => v.VisitId)
                        .Select(v => v.VisitNumBags)
                        .FirstOrDefault()
                })
                .ToList();
        }

        private static string ToLikePattern(string value)
        {
            var trimmed = value.Trim();
            return trimmed.EndsWith("%") ? trimmed : trimmed + "%";
        }
    }
}
