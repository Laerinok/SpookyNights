using Newtonsoft.Json.Linq;
using SpookyNights;
using System;
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

            config.EnableCandyLoot = true;
            config.HalloweenEventOnly = true; // Default: October Only

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
                var loadedObject = api.LoadModConfig<JObject>("spookynights-server.json");

                if (loadedObject == null)
                {
                    api.Logger.Notification("[SpookyNights] Server config file not found. Creating a new one with default values.");
                    ServerConf = GetDefaultServerConfig();
                    api.StoreModConfig(ServerConf, "spookynights-server.json");
                    return;
                }

                var defaultConfig = new ServerConfig();
                string? loadedVersion = loadedObject.ContainsKey("Version") ? loadedObject["Version"]?.ToString() : null;

                if (defaultConfig.Version != loadedVersion)
                {
                    api.Logger.Notification($"[SpookyNights] Old server config version detected ({loadedVersion} -> {defaultConfig.Version}). Migrating...");

                    // Load old config to keep user settings
                    var oldConfig = loadedObject.ToObject<ServerConfig>()!;
                    var newConfig = GetDefaultServerConfig();

                    // Migrate simple values
                    newConfig.EnableCandyLoot = oldConfig.EnableCandyLoot;
                    newConfig.HalloweenEventOnly = oldConfig.HalloweenEventOnly;
                    // Note: We intentionally do NOT migrate CandyLootTable as it is deprecated.

                    if (oldConfig.SpawnMultipliers != null)
                    {
                        foreach (var entry in oldConfig.SpawnMultipliers)
                        {
                            if (entry.Key == "spookynights:spectralbear-*")
                            {
                                newConfig.SpawnMultipliers["spookynights:spectralbear-brown-*"] = entry.Value;
                            }
                            else if (newConfig.SpawnMultipliers.ContainsKey(entry.Key))
                            {
                                newConfig.SpawnMultipliers[entry.Key] = entry.Value;
                            }
                        }
                    }

                    newConfig.UseTimeBasedSpawning = oldConfig.UseTimeBasedSpawning;
                    newConfig.SpawnOnlyAtNight = oldConfig.SpawnOnlyAtNight;
                    newConfig.NightTimeMode = oldConfig.NightTimeMode;
                    newConfig.NightStartHour = oldConfig.NightStartHour;
                    newConfig.NightEndHour = oldConfig.NightEndHour;
                    newConfig.LightLevelThreshold = oldConfig.LightLevelThreshold;
                    newConfig.AllowedSpawnMonths = oldConfig.AllowedSpawnMonths;
                    newConfig.SpawnOnlyOnLastDayOfMonth = oldConfig.SpawnOnlyOnLastDayOfMonth;
                    newConfig.SpawnOnlyOnLastDayOfWeek = oldConfig.SpawnOnlyOnLastDayOfWeek;
                    newConfig.SpawnOnlyOnFullMoon = oldConfig.SpawnOnlyOnFullMoon;
                    newConfig.FullMoonSpawnMultiplier = oldConfig.FullMoonSpawnMultiplier;
                    newConfig.Bosses = oldConfig.Bosses;
                    newConfig.EnableDebugLogging = oldConfig.EnableDebugLogging;

                    api.StoreModConfig(newConfig, "spookynights-server.json");
                    ServerConf = newConfig;
                    api.Logger.Notification("[SpookyNights] Server config migration complete.");
                }
                else
                {
                    ServerConf = loadedObject.ToObject<ServerConfig>()!;
                }
            }
            catch (Exception e)
            {
                api.Logger.Error("[SpookyNights] CRITICAL ERROR loading or migrating server config. Falling back to default settings. Details: " + e.Message);
                ServerConf = GetDefaultServerConfig();
            }
        }

        public static void LoadClientConfig(ICoreAPI api)
        {
            // Standard client loading logic (unchanged from previous version)
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