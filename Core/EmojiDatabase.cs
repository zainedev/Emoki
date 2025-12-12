using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace Emoki.Core
{
    // Class representing a single entry in the JSON
    public class EmojiEntry
    {
        public string? Emoji { get; set; }
        public List<string>? Shortcuts { get; set; }
    }

    public class EmojiDatabase
    {
        // Private field to hold the flattened map: Shortcut (e.g., ":sob") -> Emoji (e.g., "😭")
        private Dictionary<string, string> _emojisMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Defines the expected path for the JSON file
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

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                // 1. Deserialize the structured array
                var structuredEntries = JsonSerializer.Deserialize<List<EmojiEntry>>(jsonString, options) ?? new List<EmojiEntry>();
                
                // 2. Flatten the structure into the desired Dictionary map,
                //    PREPENDING THE COLON TO EACH SHORTCUT.
                _emojisMap = structuredEntries
                    .Where(e => e.Emoji != null && e.Shortcuts != null)
                    .SelectMany(e => e.Shortcuts!.Select(s => new { Shortcut = ":" + s, Emoji = e.Emoji! })) // FIX APPLIED HERE: Prepending ":"
                    .ToDictionary(x => x.Shortcut, x => x.Emoji, StringComparer.OrdinalIgnoreCase);

                Console.WriteLine($"[Database] Loaded {_emojisMap.Count} total shortcuts (with prepended ':').");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Database] Failed to load emoji data: {ex.Message}");
            }
        }

        public Dictionary<string, string> GetAll()
        {
            return _emojisMap;
        }
    }
}