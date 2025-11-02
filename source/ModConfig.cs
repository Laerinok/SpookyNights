namespace SpookyNights
{
    public class ModConfig
    {
        // Configuration version. Used to apply updates.
        public string Version { get; set; } = "1.0.0";

        // --- Loot Config ---
        public bool EnableCandyLoot { get; set; } = true;

        // --- Spawning Config ---
        public bool EnableSeasonalSpawning { get; set; } = true;
        public string RequiredSeason { get; set; } = "autumn"; // "spring", "summer", "autumn", "winter"
        public string SpawningTimeOfDay { get; set; } = "night"; // "any", "day", "night"
    }
}