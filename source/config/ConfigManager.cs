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

            config.CandyLootTable = new()
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
                { "spookynights:spectralwolf-eurasian-adult-*", "0.3@1-2" },
                { "spookynights:spectralbear-giant-adult-*", "1.0@8-12" }
            };

            // Updated multipliers to separate giant bears from standard ones
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
                    api.Logger.Notification("[SpookyNights] Old server config version detected. Migrating...");
                    var oldConfig = loadedObject.ToObject<ServerConfig>()!;
                    var newConfig = GetDefaultServerConfig();

                    newConfig.EnableCandyLoot = oldConfig.EnableCandyLoot;
                    newConfig.CandyLootTable = oldConfig.CandyLootTable;
                    // We overwrite SpawnMultipliers with the new default to ensure the new keys exist
                    // If you want to try to preserve old values, it requires complex logic, 
                    // but for a structure change, resetting this part is safer.
                    // However, we can try to copy known keys back if they exist.
                    if (oldConfig.SpawnMultipliers != null)
                    {
                        foreach (var entry in oldConfig.SpawnMultipliers)
                        {
                            // If user had the old generic key, ignore it or map it to brown
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
            // No changes needed here based on the request, keeping existing logic
            try
            {
                var loadedObject = api.LoadModConfig<JObject>("spookynights-client.json");

                if (loadedObject == null)
                {
                    api.Logger.Notification("[SpookyNights] Client config file not found. Creating a new one.");
                    ClientConf = new ClientConfig();
                    api.StoreModConfig(ClientConf, "spookynights-client.json");
                    return;
                }

                var defaultConfig = new ClientConfig();
                string? loadedVersion = loadedObject.ContainsKey("Version") ? loadedObject["Version"]?.ToString() : null;

                if (defaultConfig.Version != loadedVersion)
                {
                    api.Logger.Notification("[SpookyNights] Old client config version detected. Migrating...");
                    var oldConfig = loadedObject.ToObject<ClientConfig>()!;
                    var newConfig = new ClientConfig();

                    newConfig.EnableJackOLanternParticles = oldConfig.EnableJackOLanternParticles;

                    api.StoreModConfig(newConfig, "spookynights-client.json");
                    ClientConf = newConfig;
                    api.Logger.Notification("[SpookyNights] Client config migration complete.");
                }
                else
                {
                    ClientConf = loadedObject.ToObject<ClientConfig>()!;
                }
            }
            catch (Exception e)
            {
                api.Logger.Error("[SpookyNights] CRITICAL ERROR loading or migrating client config. Using default settings. Details: " + e.Message);
                ClientConf = new ClientConfig();
            }
        }
    }
}