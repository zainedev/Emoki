using System;
using System.Collections.Generic;
using System.Linq;

namespace Emoki.Core
{
    public static class EmojiSearch
    {
        /// <summary>
        /// Searches the emoji map for shortcuts.
        /// Matches are allowed to cross word boundaries (e.g., "gift_" matches "gift_heart").
        /// Results are still ranked by which word index the match begins at.
        /// </summary>
        public static List<KeyValuePair<string, string>> Search(Dictionary<string, string> emojiMap, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) 
                return new List<KeyValuePair<string, string>>();

            // 1. Clean the query: Remove the leading colon and convert to lowercase
            string searchTerm = query.Trim().ToLowerInvariant();
            if (searchTerm.StartsWith(":"))
            {
                searchTerm = searchTerm.Substring(1);
            }

            if (string.IsNullOrEmpty(searchTerm)) 
                return new List<KeyValuePair<string, string>>();

            // 2. Filter and Rank
            var results = emojiMap
                .Select(kvp => {
                    // Normalize the shortcut key by removing colons
                    string cleanedKey = kvp.Key.Trim(':').ToLowerInvariant();
                    return new { 
                        Kvp = kvp, 
                        WordIndex = GetMatchWordIndex(cleanedKey, searchTerm) 
                    };
                })
                .Where(x => x.WordIndex != -1) // Keep only matches that start at a word boundary
                .OrderBy(x => x.WordIndex)     // Priority 1: Match starts in earlier word
                .ThenBy(x => x.Kvp.Key.Length) // Priority 2: Shorter total shortcut length
                .Select(x => x.Kvp)
                .Take(8)
                .ToList();

            return results;
        }

        /// <summary>
        /// Finds if the searchTerm exists in the shortcut starting at a word boundary.
        /// Returns the word index (0-based) where the match starts, or -1 if no match.
        /// </summary>
        private static int GetMatchWordIndex(string shortcut, string searchTerm)
        {
            // Look for the search term in the string
            int index = shortcut.IndexOf(searchTerm);

            // We iterate through all occurrences to find one that starts at a word boundary
            while (index != -1)
            {
                // A valid "Word Start" match is at index 0 
                // OR follows a delimiter like '_' or '-'
                if (index == 0 || shortcut[index - 1] == '_' || shortcut[index - 1] == '-')
                {
                    // Calculate the word index by counting delimiters before this position
                    int wordCount = 0;
                    for (int i = 0; i < index; i++)
                    {
                        if (shortcut[i] == '_' || shortcut[i] == '-')
                        {
                            wordCount++;
                        }
                    }
                    return wordCount;
                }

                // If this occurrence wasn't at a boundary, look for the next one
                index = shortcut.IndexOf(searchTerm, index + 1);
            }

            return -1;
        }
    }
}