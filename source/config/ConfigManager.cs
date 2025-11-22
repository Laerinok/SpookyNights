using Newtonsoft.Json.Linq;
using SpookyNights;
using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;

namespace Spookynights
{
    public static class ConfigManager
    {
        public static ServerConfig ServerConf { get; private set; } = default!;
        public static ClientConfig ClientConf { get; private set; } = default!;

        private static ServerConfig GetDefaultServerConfig()
        {
            var config = new ServerConfig();

            // v1.7.0+ Candy Settings
            config.EnableCandyLoot = true;
            config.AllowedCandyMonths = new List<int> { 10 }; // Default: October Only
            config.CandyOnlyOnFullMoon = false; // Default: No moon restriction

            config.UseTimeBasedSpawning = true;
            config.SpawnOnlyAtNight = false;
            config.NightTimeMode = "Auto";

            config.SpawnOnlyOnLastDayOfMonth = false;
            config.SpawnOnlyOnLastDayOfWeek = true;
            config.SpawnOnlyOnFullMoon = false;

            config.SpawnMultipliers = new()
            {
                { "spookynights:spectralwolf-*", 1.0f },
                { "spookynights:spectralbear-brown-*", 1.0f },
                { "spookynights:spectralbear-giant-*", 0.5f },
                { "spookynights:spectraldrifter-*", 1.0f },
                { "spookynights:spectralshiver-*", 1.0f },
                { "spookynights:spectralbowtorn-*", 1.0f }
            };

            config.Bosses = new()
            {
                {
                    "spookynights:spectralbear-giant-*",
                    new BossSpawningConfig() { AllowedMoonPhases = new() { "full" } }
                }
            };

            return config;
        }

        public static void LoadServerConfig(ICoreAPI api)
        {
            try
            {
                // 1. Try to load the NEW config format (v1.6+)
                var loadedObject = api.LoadModConfig<JObject>("spookynights-server.json");

                // 2. If NEW config exists, check for internal version updates (e.g. 1.6 -> 1.7)
                if (loadedObject != null)
                {
                    HandleExistingNewConfig(api, loadedObject);
                    return;
                }

                // 3. If NEW config is MISSING, check for LEGACY config (v0.2.0 - spookynightsconfig.json)
                var legacyObject = api.LoadModConfig<JObject>("spookynightsconfig.json");

                if (legacyObject != null)
                {
                    api.Logger.Notification("[SpookyNights] Legacy config (v0.2.0) found. Migrating to new format (v1.7.0+)...");
                    ServerConf = MigrateFromLegacy(api, legacyObject);

                    // Save as NEW file
                    api.StoreModConfig(ServerConf, "spookynights-server.json");

                    // --- DELETE OLD FILE ---
                    string legacyPath = Path.Combine(api.GetOrCreateDataPath("ModConfig"), "spookynightsconfig.json");
                    if (File.Exists(legacyPath))
                    {
                        File.Delete(legacyPath);
                    }

                    api.Logger.Notification("[SpookyNights] Migration complete. Old config file deleted.");
                    return;
                }

                // 4. Fallback: Fresh Install (No files found)
                api.Logger.Notification("[SpookyNights] Server config file not found. Creating a new one with default values.");
                ServerConf = GetDefaultServerConfig();
                api.StoreModConfig(ServerConf, "spookynights-server.json");

            }
            catch (Exception e)
            {
                api.Logger.Error("[SpookyNights] CRITICAL ERROR loading server config. Falling back to defaults. Details: " + e.Message);
                ServerConf = GetDefaultServerConfig();
            }
        }

        // --- MIGRATION LOGIC ---

        private static void HandleExistingNewConfig(ICoreAPI api, JObject loadedObject)
        {
            var defaultConfig = new ServerConfig();
            string? loadedVersion = loadedObject.ContainsKey("Version") ? loadedObject["Version"]?.ToString() : null;

            if (defaultConfig.Version != loadedVersion)
            {
                api.Logger.Notification($"[SpookyNights] Updating server config version ({loadedVersion} -> {defaultConfig.Version}).");

                var newConfig = GetDefaultServerConfig();

                try
                {
                    // 1. Basic Transfer (Values that exist in both)
                    if (loadedObject["EnableCandyLoot"] != null)
                        newConfig.EnableCandyLoot = loadedObject["EnableCandyLoot"]!.ToObject<bool>();

                    // 2. MIGRATION: HalloweenEventOnly (Removed) -> AllowedCandyMonths (Added)
                    // We check if the OLD key exists in the JSON
                    bool wasHalloweenOnly = false;
                    if (loadedObject["HalloweenEventOnly"] != null)
                    {
                        wasHalloweenOnly = loadedObject["HalloweenEventOnly"]!.ToObject<bool>();
                    }

                    // If it was Halloween Only, set month to [10]. Otherwise empty [] (All year).
                    if (wasHalloweenOnly)
                    {
                        newConfig.AllowedCandyMonths = new List<int> { 10 };
                    }
                    else if (loadedObject["AllowedCandyMonths"] != null)
                    {
                        // If updating from a version that ALREADY had AllowedCandyMonths (future proofing)
                        newConfig.AllowedCandyMonths = loadedObject["AllowedCandyMonths"]!.ToObject<List<int>>() ?? new List<int>();
                    }
                    else
                    {
                        newConfig.AllowedCandyMonths = new List<int>();
                    }

                    // 3. New Options
                    if (loadedObject["CandyOnlyOnFullMoon"] != null)
                        newConfig.CandyOnlyOnFullMoon = loadedObject["CandyOnlyOnFullMoon"]!.ToObject<bool>();

                    // 4. Spawn Settings (Preserve old values)
                    if (loadedObject["SpawnMultipliers"] is JObject oldMultipliers)
                    {
                        foreach (var prop in oldMultipliers.Properties())
                        {
                            if (newConfig.SpawnMultipliers.ContainsKey(prop.Name))
                            {
                                newConfig.SpawnMultipliers[prop.Name] = prop.Value.ToObject<float>();
                            }
                        }
                    }

                    if (loadedObject["UseTimeBasedSpawning"] != null) newConfig.UseTimeBasedSpawning = loadedObject["UseTimeBasedSpawning"]!.ToObject<bool>();
                    if (loadedObject["SpawnOnlyAtNight"] != null) newConfig.SpawnOnlyAtNight = loadedObject["SpawnOnlyAtNight"]!.ToObject<bool>();
                    if (loadedObject["NightTimeMode"] != null) newConfig.NightTimeMode = loadedObject["NightTimeMode"]!.ToString();
                    if (loadedObject["NightStartHour"] != null) newConfig.NightStartHour = loadedObject["NightStartHour"]!.ToObject<float>();
                    if (loadedObject["NightEndHour"] != null) newConfig.NightEndHour = loadedObject["NightEndHour"]!.ToObject<float>();
                    if (loadedObject["LightLevelThreshold"] != null) newConfig.LightLevelThreshold = loadedObject["LightLevelThreshold"]!.ToObject<int>();

                    if (loadedObject["AllowedSpawnMonths"] != null)
                        newConfig.AllowedSpawnMonths = loadedObject["AllowedSpawnMonths"]!.ToObject<List<int>>() ?? new List<int>();

                    if (loadedObject["SpawnOnlyOnLastDayOfMonth"] != null) newConfig.SpawnOnlyOnLastDayOfMonth = loadedObject["SpawnOnlyOnLastDayOfMonth"]!.ToObject<bool>();
                    if (loadedObject["SpawnOnlyOnLastDayOfWeek"] != null) newConfig.SpawnOnlyOnLastDayOfWeek = loadedObject["SpawnOnlyOnLastDayOfWeek"]!.ToObject<bool>();
                    if (loadedObject["SpawnOnlyOnFullMoon"] != null) newConfig.SpawnOnlyOnFullMoon = loadedObject["SpawnOnlyOnFullMoon"]!.ToObject<bool>();
                    if (loadedObject["FullMoonSpawnMultiplier"] != null) newConfig.FullMoonSpawnMultiplier = loadedObject["FullMoonSpawnMultiplier"]!.ToObject<float>();

                    if (loadedObject["Bosses"] != null)
                        newConfig.Bosses = loadedObject["Bosses"]!.ToObject<Dictionary<string, BossSpawningConfig>>() ?? newConfig.Bosses;

                    if (loadedObject["EnableDebugLogging"] != null) newConfig.EnableDebugLogging = loadedObject["EnableDebugLogging"]!.ToObject<bool>();
                }
                catch (Exception ex)
                {
                    api.Logger.Warning("[SpookyNights] Partial error migrating config: " + ex.Message);
                }

                api.StoreModConfig(newConfig, "spookynights-server.json");
                ServerConf = newConfig;
            }
            else
            {
                ServerConf = loadedObject.ToObject<ServerConfig>()!;
            }
        }

        private static ServerConfig MigrateFromLegacy(ICoreAPI api, JObject legacyData)
        {
            var newConfig = GetDefaultServerConfig();

            try
            {
                // 1. Candy Logic (Legacy used 'EnableCandyLoot' + hardcoded month check in code)
                // In v0.2.0, there was NO 'HalloweenEventOnly' key. It was implicitly assumed if month was 10.
                // We default to [10] (October) for safety to match the "Spooky" theme of old config.
                if (legacyData["EnableCandyLoot"] != null)
                    newConfig.EnableCandyLoot = legacyData["EnableCandyLoot"]!.ToObject<bool>();

                // 2. Spawn Logic
                if (legacyData["UseTimeBasedSpawning"] != null)
                    newConfig.UseTimeBasedSpawning = legacyData["UseTimeBasedSpawning"]!.ToObject<bool>();

                if (legacyData["SpawnOnlyAtNight"] != null)
                    newConfig.SpawnOnlyAtNight = legacyData["SpawnOnlyAtNight"]!.ToObject<bool>();

                if (legacyData["SpawnOnlyOnLastDayOfMonth"] != null)
                    newConfig.SpawnOnlyOnLastDayOfMonth = legacyData["SpawnOnlyOnLastDayOfMonth"]!.ToObject<bool>();

                if (legacyData["SpawnOnlyOnLastDayOfWeek"] != null)
                    newConfig.SpawnOnlyOnLastDayOfWeek = legacyData["SpawnOnlyOnLastDayOfWeek"]!.ToObject<bool>();

                // 3. Spawn Multipliers (Name changes)
                if (legacyData["SpawnMultipliers"] is JObject oldMultipliers)
                {
                    foreach (var prop in oldMultipliers.Properties())
                    {
                        if (prop.Name == "spookynights:spectralbear-*")
                        {
                            float val = prop.Value.ToObject<float>();
                            newConfig.SpawnMultipliers["spookynights:spectralbear-brown-*"] = val;
                        }
                        else if (newConfig.SpawnMultipliers.ContainsKey(prop.Name))
                        {
                            newConfig.SpawnMultipliers[prop.Name] = prop.Value.ToObject<float>();
                        }
                    }
                }

                // 4. Allowed Months
                if (legacyData["AllowedSpawnMonths"] != null)
                {
                    newConfig.AllowedSpawnMonths = legacyData["AllowedSpawnMonths"]!.ToObject<List<int>>() ?? new List<int>();
                }

                // 5. Boss Migration (BearSpawnConfig -> Bosses)
                if (legacyData["BearSpawnConfig"] is JObject oldBearConfig)
                {
                    var bossConfig = new BossSpawningConfig();

                    if (oldBearConfig["Enabled"] != null)
                        bossConfig.Enabled = oldBearConfig["Enabled"]!.ToObject<bool>();

                    if (oldBearConfig["AllowedMoonPhases"] != null)
                        bossConfig.AllowedMoonPhases = oldBearConfig["AllowedMoonPhases"]!.ToObject<List<string>>() ?? new List<string>();

                    newConfig.Bosses["spookynights:spectralbear-giant-*"] = bossConfig;
                }
            }
            catch (Exception ex)
            {
                api.Logger.Warning("[SpookyNights] Error migrating legacy settings: " + ex.Message + ". Defaults used.");
            }

            return newConfig;
        }

        public static void LoadClientConfig(ICoreAPI api)
        {
            try
            {
                var loadedObject = api.LoadModConfig<JObject>("spookynights-client.json");

                if (loadedObject == null)
                {
                    ClientConf = new ClientConfig();
                    api.StoreModConfig(ClientConf, "spookynights-client.json");
                    return;
                }
                ClientConf = loadedObject.ToObject<ClientConfig>()!;
            }
            catch
            {
                ClientConf = new ClientConfig();
            }
        }
    }
}