using System;
using System.Collections.Generic;
using System.Linq;

namespace Emoki.Core
{
    public static class EmojiSearch
    {
        public static List<KeyValuePair<string, string>> Search(
            Dictionary<string, string> db,
            string query)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));

            if (string.IsNullOrWhiteSpace(query))
                return new List<KeyValuePair<string, string>>();

            // The 'query' (token) from Program.cs is already cleaned and lower-cased (e.g., ":sob").
            // The database keys are in the format ":sob:".
            // We use the query as a prefix match against the database keys.
            
            return db
                .Where(kvp => kvp.Key.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();
        }
    }
}