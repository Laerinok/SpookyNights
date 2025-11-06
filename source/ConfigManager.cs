// In source/ConfigManager.cs

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

            config.SpawnMultipliers = new()
            {
                { "spookynights:spectralwolf-*", 1.0f },
                { "spookynights:spectralbear-*", 1.0f },
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
                    ServerConf = GetDefaultServerConfig(); // Use our new method to get a fully populated config
                    api.StoreModConfig(ServerConf, "spookynights-server.json");
                    return;
                }

                var defaultConfig = new ServerConfig(); // This is just for getting the version number
                string? loadedVersion = loadedObject.ContainsKey("Version") ? loadedObject["Version"]?.ToString() : null;

                if (defaultConfig.Version != loadedVersion)
                {
                    api.Logger.Notification("[SpookyNights] Old server config version detected. Migrating...");
                    var oldConfig = loadedObject.ToObject<ServerConfig>()!;

                    // Start with a new, fully populated default config
                    var newConfig = GetDefaultServerConfig();

                    // Now, overwrite the defaults with the user's saved values
                    newConfig.EnableCandyLoot = oldConfig.EnableCandyLoot;
                    newConfig.CandyLootTable = oldConfig.CandyLootTable;
                    newConfig.SpawnMultipliers = oldConfig.SpawnMultipliers;
                    newConfig.UseTimeBasedSpawning = oldConfig.UseTimeBasedSpawning;
                    newConfig.SpawnOnlyAtNight = oldConfig.SpawnOnlyAtNight;
                    newConfig.AllowedSpawnMonths = oldConfig.AllowedSpawnMonths;
                    newConfig.SpawnOnlyOnLastDayOfMonth = oldConfig.SpawnOnlyOnLastDayOfMonth;
                    newConfig.SpawnOnlyOnLastDayOfWeek = oldConfig.SpawnOnlyOnLastDayOfWeek;
                    newConfig.Bosses = oldConfig.Bosses;

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
                ServerConf = GetDefaultServerConfig(); // Fallback to defaults in case of error
            }
        }

        public static void LoadClientConfig(ICoreAPI api)
        {
            // This method remains correct because ClientConfig constructor is already empty.
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