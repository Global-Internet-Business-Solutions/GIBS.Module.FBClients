using System;
using System.Collections.Generic;
using System.Linq;

namespace GIBS.Module.FBClients.Lists.Models
{
    public class ListGroup
    {
        public string ListName { get; private set; }
        public int ParentID { get; private set; }
        public string Key => $"{ParentID}:{ListName}";
        public string ParentKey { get; private set; }
        public string DisplayName => ParentID == 0 ? ListName : $"{ParentKey}:{ListName}";
        public bool EnableSortOrder => Entries.Any(entry => entry.SortOrder > 0);
        public List<Lists> Entries { get; private set; }

        public static List<ListGroup> Create(IEnumerable<Lists> source)
        {
            var entries = source.ToList();
            var byId = entries.ToDictionary(entry => entry.ListsId);
            return entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.ListName))
                .GroupBy(entry => $"{entry.ParentID}:{entry.ListName}", StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var first = group.First();
                    return new ListGroup
                    {
                        ListName = first.ListName,
                        ParentID = first.ParentID,
                        ParentKey = GetParentKey(first.ParentID, byId),
                        Entries = group.OrderBy(entry => entry.SortOrder)
                            .ThenBy(entry => entry.Text)
                            .ThenBy(entry => entry.ListsId)
                            .ToList()
                    };
                })
                .OrderBy(group => group.DisplayName)
                .ToList();
        }

        private static string GetParentKey(int parentId, Dictionary<int, Lists> entries)
        {
            var path = new Stack<string>();
            var visited = new HashSet<int>();
            while (parentId != 0)
            {
                if (!visited.Add(parentId) || !entries.TryGetValue(parentId, out var parent))
                {
                    throw new InvalidOperationException("The list hierarchy contains a missing or circular parent.");
                }

                path.Push($"{parent.ListName}.{parent.Value}");
                parentId = parent.ParentID;
            }

            return string.Join(":", path);
        }
    }
}
