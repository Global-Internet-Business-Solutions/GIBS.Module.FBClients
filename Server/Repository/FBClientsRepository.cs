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
            db.FBClients.Remove(FBClients);
            db.SaveChanges();
        }
    }
}
