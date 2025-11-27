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

            config.EnableCandyLoot = true;
            config.AllowedCandyMonths = new List<int> { 10 };
            config.CandyOnlyOnFullMoon = false;

            config.UseTimeBasedSpawning = true;
            config.SpawnOnlyAtNight = true;
            config.NightTimeMode = "Auto";

            config.SpawnOnlyOnLastDayOfMonth = false;
            config.SpawnOnlyOnFullMoon = false;

            config.SpawnMultipliers = new()
            {
                { "spookynights:spectralwolf-*", 0.5f },
                { "spookynights:spectralbear-brown-*", 0.5f },
                { "spookynights:spectralbear-giant-*", 0.5f },
                { "spookynights:spectraldrifter-*", 0.5f },
                { "spookynights:spectralshiver-*", 0.5f },
                { "spookynights:spectralbowtorn-*", 0.5f }
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

                if (loadedObject != null)
                {
                    HandleExistingNewConfig(api, loadedObject);
                    return;
                }

                var legacyObject = api.LoadModConfig<JObject>("spookynightsconfig.json");

                if (legacyObject != null)
                {
                    api.Logger.Notification("[SpookyNights] Legacy config (v0.2.0) found. Migrating...");
                    ServerConf = MigrateFromLegacy(api, legacyObject);
                    api.StoreModConfig(ServerConf, "spookynights-server.json");

                    string legacyPath = Path.Combine(api.GetOrCreateDataPath("ModConfig"), "spookynightsconfig.json");
                    if (File.Exists(legacyPath)) File.Delete(legacyPath);

                    return;
                }

                api.Logger.Notification("[SpookyNights] Creating new server config.");
                ServerConf = GetDefaultServerConfig();
                api.StoreModConfig(ServerConf, "spookynights-server.json");

            }
            catch (Exception e)
            {
                api.Logger.Error("[SpookyNights] Error loading config: " + e.Message);
                ServerConf = GetDefaultServerConfig();
            }
        }

        private static void HandleExistingNewConfig(ICoreAPI api, JObject loadedObject)
        {
            var defaultConfig = new ServerConfig();
            string? loadedVersion = loadedObject.ContainsKey("Version") ? loadedObject["Version"]?.ToString() : null;

            if (defaultConfig.Version != loadedVersion)
            {
                var newConfig = GetDefaultServerConfig();
                try
                {
                    if (loadedObject["EnableCandyLoot"] != null) newConfig.EnableCandyLoot = loadedObject["EnableCandyLoot"]!.ToObject<bool>();

                    if (loadedObject["HalloweenEventOnly"] != null && loadedObject["HalloweenEventOnly"]!.ToObject<bool>())
                    {
                        newConfig.AllowedCandyMonths = new List<int> { 10 };
                    }
                    else if (loadedObject["AllowedCandyMonths"] != null)
                    {
                        newConfig.AllowedCandyMonths = loadedObject["AllowedCandyMonths"]!.ToObject<List<int>>() ?? new List<int>();
                    }

                    if (loadedObject["CandyOnlyOnFullMoon"] != null) newConfig.CandyOnlyOnFullMoon = loadedObject["CandyOnlyOnFullMoon"]!.ToObject<bool>();

                    if (loadedObject["SpawnMultipliers"] is JObject oldMultipliers)
                    {
                        foreach (var prop in oldMultipliers.Properties())
                        {
                            if (newConfig.SpawnMultipliers.ContainsKey(prop.Name))
                                newConfig.SpawnMultipliers[prop.Name] = prop.Value.ToObject<float>();
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

                    // We ignore SpawnOnlyOnLastDayOfWeek migration as we want to phase it out.

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
                if (legacyData["EnableCandyLoot"] != null) newConfig.EnableCandyLoot = legacyData["EnableCandyLoot"]!.ToObject<bool>();
                if (legacyData["UseTimeBasedSpawning"] != null) newConfig.UseTimeBasedSpawning = legacyData["UseTimeBasedSpawning"]!.ToObject<bool>();
                if (legacyData["SpawnOnlyAtNight"] != null) newConfig.SpawnOnlyAtNight = legacyData["SpawnOnlyAtNight"]!.ToObject<bool>();

                if (legacyData["SpawnOnlyOnLastDayOfMonth"] != null) newConfig.SpawnOnlyOnLastDayOfMonth = legacyData["SpawnOnlyOnLastDayOfMonth"]!.ToObject<bool>();

                if (legacyData["SpawnMultipliers"] is JObject oldMultipliers)
                {
                    foreach (var prop in oldMultipliers.Properties())
                    {
                        if (prop.Name == "spookynights:spectralbear-*")
                        {
                            newConfig.SpawnMultipliers["spookynights:spectralbear-brown-*"] = prop.Value.ToObject<float>();
                        }
                        else if (newConfig.SpawnMultipliers.ContainsKey(prop.Name))
                        {
                            newConfig.SpawnMultipliers[prop.Name] = prop.Value.ToObject<float>();
                        }
                    }
                }

                if (legacyData["AllowedSpawnMonths"] != null)
                {
                    newConfig.AllowedSpawnMonths = legacyData["AllowedSpawnMonths"]!.ToObject<List<int>>() ?? new List<int>();
                }

                if (legacyData["BearSpawnConfig"] is JObject oldBearConfig)
                {
                    var bossConfig = new BossSpawningConfig();
                    if (oldBearConfig["Enabled"] != null) bossConfig.Enabled = oldBearConfig["Enabled"]!.ToObject<bool>();
                    if (oldBearConfig["AllowedMoonPhases"] != null) bossConfig.AllowedMoonPhases = oldBearConfig["AllowedMoonPhases"]!.ToObject<List<string>>() ?? new List<string>();
                    newConfig.Bosses["spookynights:spectralbear-giant-*"] = bossConfig;
                }
            }
            catch (Exception ex)
            {
                api.Logger.Warning("[SpookyNights] Error migrating legacy settings: " + ex.Message);
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