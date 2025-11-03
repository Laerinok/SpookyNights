// In source/ModConfig.cs

using System.Collections.Generic;

namespace SpookyNights
{
    public class ModConfig
    {
        public string Version { get; set; } = "1.0.0";

        public bool EnableCandyLoot { get; set; } = true;

        public Dictionary<string, string> CandyLootTable { get; set; } = new Dictionary<string, string>();

        // --- Spawning Config ---
        public Dictionary<string, float> SpawnMultipliers { get; set; } = new Dictionary<string, float>();

        // --- Block Config ---
        public bool EnableJackOLanternParticles { get; set; } = true;
    }
}