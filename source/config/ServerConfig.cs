using System.Collections.Generic;

namespace SpookyNights
{
    public class BossSpawningConfig
    {
        public bool Enabled { get; set; } = true;
        public List<string> AllowedMoonPhases { get; set; } = new List<string>();
    }

    public class ServerConfig
    {
        public string? Version { get; set; } = "1.4.0";
        public bool EnableCandyLoot { get; set; } = false;
        public Dictionary<string, string> CandyLootTable { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, float> SpawnMultipliers { get; set; } = new Dictionary<string, float>();
        public bool UseTimeBasedSpawning { get; set; } = true;
        public bool SpawnOnlyAtNight { get; set; } = true;

        // "Auto" mode uses light levels, "Manual" uses the hours below.
        public string NightTimeMode { get; set; } = "Auto";

        // Hours used only when NightTimeMode is "Manual"
        public float NightStartHour { get; set; } = 20f;
        public float NightEndHour { get; set; } = 6f;

        // Light level threshold used only when NightTimeMode is "Auto"
        // Vanilla drifters spawn at light level 7 or less.
        public int LightLevelThreshold { get; set; } = 7;

        public List<int> AllowedSpawnMonths { get; set; } = new List<int>();
        public bool SpawnOnlyOnLastDayOfMonth { get; set; } = false;
        public bool SpawnOnlyOnLastDayOfWeek { get; set; } = false;
        public bool SpawnOnlyOnFullMoon { get; set; } = false;
        public float FullMoonSpawnMultiplier { get; set; } = 2.0f;
        public Dictionary<string, BossSpawningConfig> Bosses { get; set; } = new Dictionary<string, BossSpawningConfig>();

        // New property to control debug logging for performance
        public bool EnableDebugLogging { get; set; } = false;

        public ServerConfig()
        {
        }
    }
}