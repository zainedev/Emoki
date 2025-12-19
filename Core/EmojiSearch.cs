using System;
using System.Collections.Generic;
using System.Linq;

namespace Emoki.Core
{
    public static class EmojiSearch
    {
        /// <summary>
        /// Search implementation used by the UI.
        /// - `emojiMap`: map of stored shortcuts to emoji (keys usually include ':' markers).
        /// - `query`: the sanitized user token (including a leading ':').
        /// Returns up to 8 matches ordered by word-index (where the match starts) then by shortcut length.
        /// </summary>
        public static List<KeyValuePair<string, string>> Search(Dictionary<string, string> emojiMap, string query)
        {
            // Guard: empty query -> no results
            if (string.IsNullOrWhiteSpace(query))
                return new List<KeyValuePair<string, string>>();

            // Normalize query: trim and drop the leading ':' if present so we search raw tokens
            string searchTerm = query.Trim().ToLowerInvariant();
            if (searchTerm.StartsWith(":"))
            {
                searchTerm = searchTerm.Substring(1);
            }

            if (string.IsNullOrEmpty(searchTerm))
                return new List<KeyValuePair<string, string>>();

            // For each stored shortcut key compute a match score (word index) and keep matches
            var results = emojiMap
                .Select(kvp =>
                {
                    // cleanedKey: shortcut normalized to a simple token (no surrounding colons)
                    string cleanedKey = kvp.Key.Trim(':').ToLowerInvariant();

                    return new
                    {
                        Kvp = kvp,
                        WordIndex = GetMatchWordIndex(cleanedKey, searchTerm)
                    };
                })
                // only keep entries where the token matches starting at a word boundary
                .Where(x => x.WordIndex != -1)
                // prefer matches that begin in earlier words (word index), then shorter shortcuts
                .OrderBy(x => x.WordIndex)
                .ThenBy(x => x.Kvp.Key.Length)
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
            // Find the first occurrence of the search term
            int index = shortcut.IndexOf(searchTerm);

            // Check all occurrences until we find one that starts at a word boundary
            while (index != -1)
            {
                // Valid start if at beginning or preceded by '_' / '-'
                if (index == 0 || shortcut[index - 1] == '_' || shortcut[index - 1] == '-')
                {
                    // Compute the word index by counting delimiters before `index`
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

                // Try next occurrence
                index = shortcut.IndexOf(searchTerm, index + 1);
            }

            return -1;
        }
    }
}