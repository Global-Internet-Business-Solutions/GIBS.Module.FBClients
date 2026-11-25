using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using GIBS.Module.FBClients.Repository;
using Oqtane.Modules;

namespace GIBS.Module.FBClients.Lists.Repository
{
    public interface IListsRepository
    {
        IEnumerable<Models.Lists> GetListss(int ModuleId);
        IEnumerable<Models.Lists> GetLists(string ListName, int SiteId);
        IEnumerable<Models.Lists> GetChildLists(int parentId, int moduleId, int siteId);
        Models.Lists GetLists(int ListsId);
        Models.Lists GetLists(int ListsId, bool tracking);
        Models.Lists AddLists(Models.Lists Lists);
        Models.Lists UpdateLists(Models.Lists Lists);
        void DeleteLists(int ListsId);
        void DeleteEntries(IEnumerable<int> listsIds, int moduleId);
        void UpdateSortOrders(IEnumerable<Models.Lists> entries, int moduleId);
    }

    public class ListsRepository : IListsRepository, ITransientService
    {
        private readonly IDbContextFactory<FBClientsContext> _factory;

        public ListsRepository(IDbContextFactory<FBClientsContext> factory)
        {
            _factory = factory;
        }

        public IEnumerable<Models.Lists> GetListss(int ModuleId)
        {
            using var db = _factory.CreateDbContext();
            return db.Lists
		.Where(item => item.ModuleId == ModuleId)
		.AsNoTracking()
		.ToList();
        }

        public IEnumerable<Models.Lists> GetLists(string ListName, int SiteId)
        {
            using var db = _factory.CreateDbContext();
            return db.Lists
                .Where(item => item.SiteID == SiteId && item.ListName == ListName)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.ListsId)
                .AsNoTracking()
                .ToList();
        }

        public IEnumerable<Models.Lists> GetChildLists(int parentId, int moduleId, int siteId)
        {
            using var db = _factory.CreateDbContext();
            return db.Lists.AsNoTracking()
                .Where(item => item.ParentID == parentId && item.ModuleId == moduleId && item.SiteID == siteId)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.ListsId)
                .ToList();
        }

        public Models.Lists GetLists(int ListsId)
        {
            return GetLists(ListsId, false);
        }

        public Models.Lists GetLists(int ListsId, bool tracking)
        {
            using var db = _factory.CreateDbContext();
            if (tracking)
            {
                return db.Lists
		    .Find(ListsId);
            }
            else
            {
                return db.Lists
		    .AsNoTracking()
		    .FirstOrDefault(item => item.ListsId == ListsId);
            }
        }

        public Models.Lists AddLists(Models.Lists Lists)
        {
            using var db = _factory.CreateDbContext();
            db.Lists.Add(Lists);
            db.SaveChanges();
            return Lists;
        }

        public Models.Lists UpdateLists(Models.Lists Lists)
        {
            using var db = _factory.CreateDbContext();
            db.Entry(Lists).State = EntityState.Modified;
            db.SaveChanges();
            return Lists;
        }

        public void DeleteLists(int ListsId)
        {
            using var db = _factory.CreateDbContext();
            var entry = db.Lists.Find(ListsId);
            db.Lists.Remove(entry);
            db.SaveChanges();
        }

        public void DeleteEntries(IEnumerable<int> listsIds, int moduleId)
        {
            using var db = _factory.CreateDbContext();
            var ids = listsIds.ToArray();
            db.Lists.RemoveRange(db.Lists.Where(item => item.ModuleId == moduleId && ids.Contains(item.ListsId)));
            db.SaveChanges();
        }

        public void UpdateSortOrders(IEnumerable<Models.Lists> entries, int moduleId)
        {
            using var db = _factory.CreateDbContext();
            foreach (var entry in entries)
            {
                var stored = db.Lists.Single(item => item.ListsId == entry.ListsId && item.ModuleId == moduleId);
                stored.SortOrder = entry.SortOrder;
            }

            db.SaveChanges();
        }
    }
}
