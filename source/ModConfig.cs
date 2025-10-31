namespace SpookyNights
{
    public class ModConfig
    {
        // --- Loot Config ---
        public bool EnableCandyLoot { get; set; } = true;

        // --- Spawning Config ---
        public bool EnableSeasonalSpawning { get; set; } = true;
        public string RequiredSeason { get; set; } = "autumn"; // "spring", "summer", "autumn", "winter"
        public string SpawningTimeOfDay { get; set; } = "night"; // "any", "day", "night"
    }
}