// In source/ServerConfig.cs

using System.Collections.Generic;

namespace SpookyNights
{
    public class BossSpawningConfig
    {
        public bool Enabled { get; set; } = true;
        // The list is now initialized empty by default.
        public List<string> AllowedMoonPhases { get; set; } = new List<string>();
    }

    public class ServerConfig
    {
        public string? Version { get; set; } = "1.1.0";
        public bool EnableCandyLoot { get; set; } = false;
        // All collections are now initialized empty. The default data will be added by the ConfigManager.
        public Dictionary<string, string> CandyLootTable { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, float> SpawnMultipliers { get; set; } = new Dictionary<string, float>();
        public bool UseTimeBasedSpawning { get; set; } = true;
        public bool SpawnOnlyAtNight { get; set; } = true;
        public List<int> AllowedSpawnMonths { get; set; } = new List<int>();
        public bool SpawnOnlyOnLastDayOfMonth { get; set; } = false;
        public bool SpawnOnlyOnLastDayOfWeek { get; set; } = false;

        public bool SpawnOnlyOnFullMoon { get; set; } = false;
        public float FullMoonSpawnMultiplier { get; set; } = 2.0f;

        public Dictionary<string, BossSpawningConfig> Bosses { get; set; } = new Dictionary<string, BossSpawningConfig>();

        // The constructor is now empty. It creates a blank object.
        public ServerConfig()
        {
        }
    }
}