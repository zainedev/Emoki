using System;
using System.Collections.Generic;
using System.Linq;

namespace Emoki.Core
{
    public static class EmojiSearch
    {
        public static List<KeyValuePair<string, string>> Search(Dictionary<string, string> db, string query)
        {
            // FIX APPLIED HERE: Force the input query to lowercase to match the database keys
            query = query.ToLower();
            
            return db
                .Where(kvp => kvp.Key.StartsWith(query, StringComparison.Ordinal))
                .Take(5)
                .ToList();
        }
    }
}