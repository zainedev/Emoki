using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace Emoki.Core
{
    public class EmojiEntry
    {
        public string? Emoji { get; set; }
        public List<string>? Shortcuts { get; set; }
    }

    public class EmojiDatabase
    {
        // Internal map of normalized shortcut -> emoji. Case-insensitive comparer used.
        private Dictionary<string, string> _emojisMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Relative path (from AppDomain.BaseDirectory) to the JSON data file
        private const string EmojiDataFilePath = "Data/emojis.json";

        public EmojiDatabase()
        {
            Load();
        }

        private void Load()
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(baseDirectory, EmojiDataFilePath);
                
                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"[Database] Error: Emoji data file not found at: {fullPath}");
                    return;
                }

                string jsonString = File.ReadAllText(fullPath);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                var structuredEntries = JsonSerializer.Deserialize<List<EmojiEntry>>(jsonString, options) ?? new List<EmojiEntry>();
                
                // 1. Create a flattened list of all {ShortcutKey, EmojiValue} pairs.
                var allPairs = structuredEntries
                    .Where(e => e.Emoji != null && e.Shortcuts != null)
                    .SelectMany(e => e.Shortcuts!
                        .Select(s => 
                        {
                            // Standardize stored keys to a canonical ":shortcut:" lowercase form
                            string cleanedShortcut = s.Trim().Trim(':').ToLowerInvariant();
                            string key = $":{cleanedShortcut}:";

                            return new { Shortcut = key, Emoji = e.Emoji! };
                        }))
                    .ToList();
                
                // 2. Handle duplicates by grouping keys and taking the first encountered Emoji.
                // This ensures ToDictionary doesn't throw an exception on duplicate keys, 
                // and should correct the count if a duplicate exists.
                _emojisMap = allPairs
                    .GroupBy(x => x.Shortcut, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key, 
                        g => g.First().Emoji, // Takes the Emoji from the first element in the group
                        StringComparer.OrdinalIgnoreCase
                    ); 
                
                // Check if any keys were dropped due to duplication
                if (allPairs.Count != _emojisMap.Count)
                {
                    int droppedCount = allPairs.Count - _emojisMap.Count;
                    Console.WriteLine($"[Database] Warning: {droppedCount} duplicate shortcut keys were found and discarded.");
                }

                Console.WriteLine($"[Database] Loaded {_emojisMap.Count} total shortcuts (standardized to :shortcut: format).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Database] Failed to load emoji data: {ex.Message}");
            }
        }

        // Expose the internal emoji map. Caller should treat result as read-only.
        public Dictionary<string, string> GetAll()
        {
            return _emojisMap;
        }
    }
}