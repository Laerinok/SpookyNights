
// In source/ModConfig.cs

using System.Collections.Generic;
using Vintagestory.API.Common;

namespace SpookyNights
{
    public class BossSpawningConfig
    {
        public bool Enabled { get; set; } = true;
        public List<string> AllowedMoonPhases { get; set; } = new List<string>() { "full" };
    }

    public class ModConfig
    {
        public string Version { get; set; } = "1.1.0";
        public bool EnableCandyLoot { get; set; } = false;
        public Dictionary<string, string> CandyLootTable { get; set; }
        public Dictionary<string, float> SpawnMultipliers { get; set; }
        public bool UseTimeBasedSpawning { get; set; } = true;
        public bool SpawnOnlyAtNight { get; set; } = true;
        public List<int> AllowedSpawnMonths { get; set; } = new List<int>() { 10 };
        public bool SpawnOnlyOnLastDayOfMonth { get; set; } = false;
        public bool SpawnOnlyOnLastDayOfWeek { get; set; } = false;
        public BossSpawningConfig BearSpawnConfig { get; set; } = new BossSpawningConfig();
        public bool EnableJackOLanternParticles { get; set; } = true;

        // This is the constructor. It runs when "new ModConfig()" is called.
        // We define all default collections here.
        public ModConfig()
        {
            CandyLootTable = new Dictionary<string, string>()
            {
                { "spookynights:spectraldrifter-normal", "0.2@1" },
                { "spookynights:spectraldrifter-deep", "0.3@1-2" },
                { "spookynights:spectraldrifter-tainted", "0.35@1-2" },
                { "spookynights:spectraldrifter-corrupt", "0.4@2-3" },
                { "spookynights:spectraldrifter-nightmare", "0.6@3-5" },
                { "spookynights:spectraldrifter-double-headed", "0.7@4-6" },
                { "spookynights:spectralshiver-surface", "0.2@1" },
                { "spookynights:spectralshiver-deep", "0.3@1-2" },
                { "spookynights:spectralshiver-tainted", "0.35@1-2" },
                { "spookynights:spectralshiver-corrupt", "0.4@2-3" },
                { "spookynights:spectralshiver-nightmare", "0.6@3-5" },
                { "spookynights:spectralshiver-stilt", "0.7@4-6" },
                { "spookynights:spectralshiver-bellhead", "0.7@4-6" },
                { "spookynights:spectralshiver-deepsplit", "0.7@4-6" },
                { "spookynights:spectralbowtorn-surface", "0.25@1" },
                { "spookynights:spectralbowtorn-deep", "0.35@1-2" },
                { "spookynights:spectralbowtorn-tainted", "0.4@2-3" },
                { "spookynights:spectralbowtorn-corrupt", "0.45@2-4" },
                { "spookynights:spectralbowtorn-nightmare", "0.65@3-5" },
                { "spookynights:spectralbowtorn-gearfoot", "0.75@4-6" },
                { "spookynights:spectralbear-brown-adult-*", "0.5@2-4" },
                { "spookynights:spectralwolf-eurasian-adult-*", "0.3@1-2" }
            };

            SpawnMultipliers = new Dictionary<string, float>()
            {
                { "spookynights:spectralwolf-*", 1.0f },
                { "spookynights:spectralbear-*", 1.0f },
                { "spookynights:spectraldrifter-*", 1.0f },
                { "spookynights:spectralshiver-*", 1.0f },
                { "spookynights:spectralbowtorn-*", 1.0f }
            };
        }
    }
}