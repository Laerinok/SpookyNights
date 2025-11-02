// In source/ModConfig.cs

using System.Collections.Generic;

namespace SpookyNights
{
    public class ModConfig
    {
        public string Version { get; set; } = "1.0.0";

        public bool EnableCandyLoot { get; set; } = true;

        // The dictionary is now much simpler!
        public Dictionary<string, string> CandyLootTable { get; set; } = new Dictionary<string, string>();

        // --- Spawning Config ---
        public bool EnableSeasonalSpawning { get; set; } = true;
        public string RequiredSeason { get; set; } = "autumn";
        public string SpawningTimeOfDay { get; set; } = "night";
    }
}