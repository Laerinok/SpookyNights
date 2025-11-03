// In source/SpookyNightsModSystems.cs

using SpookyNights;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Spookynights
{
    public sealed class SpookyNights : ModSystem
    {
        public static ModConfig LoadedConfig { get; private set; } = default!;
        private ICoreAPI api = default!;
        private ICoreServerAPI sapi = default!;

        public override void StartPre(ICoreAPI api)
        {
            this.api = api;
            LoadAndMigrateConfig(api);
        }

        // ... (The LoadAndMigrateConfig method from the last correct version)
        private void LoadAndMigrateConfig(ICoreAPI api)
        {
            try
            {
                LoadedConfig = api.LoadModConfig<ModConfig>("spookynightsconfig.json");
                if (LoadedConfig == null)
                {
                    LoadedConfig = new ModConfig();
                    api.StoreModConfig(LoadedConfig, "spookynightsconfig.json");
                }
                else
                {
                    bool configUpdated = false;
                    ModConfig defaultConfig = new ModConfig();
                    foreach (var pair in defaultConfig.SpawnMultipliers)
                    {
                        if (!LoadedConfig.SpawnMultipliers.ContainsKey(pair.Key))
                        {
                            LoadedConfig.SpawnMultipliers[pair.Key] = pair.Value;
                            configUpdated = true;
                        }
                    }
                    if (LoadedConfig.Version != defaultConfig.Version || configUpdated)
                    {
                        LoadedConfig.Version = defaultConfig.Version;
                        api.StoreModConfig(LoadedConfig, "spookynightsconfig.json");
                    }
                }
            }
            catch (Exception e)
            {
                api.Logger.Error("[SpookyNights] Failed to load or create config file, using defaults: " + e.Message);
                LoadedConfig = new ModConfig();
            }
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            if (LoadedConfig.EnableJackOLanternParticles) return;
            string[] variants = { "north", "east", "south", "west" };
            foreach (string variant in variants)
            {
                AssetLocation blockCode = new AssetLocation("spookynights", "jackolantern-" + variant);
                Block block = api.World.GetBlock(blockCode);
                if (block != null) { block.ParticleProperties = null; }
            }
            api.Logger.Debug("[SpookyNights] Particles for jack-o'-lanterns have been disabled via config.");
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("ItemCandyBag", typeof(ItemCandyBag));
            api.Logger.Notification("🌟 Spooky Nights is loaded!");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            api.Event.OnEntityDeath += OnEntityDeath;
            api.Event.OnTrySpawnEntity += OnTrySpawnEntity;
        }

        private void OnEntityDeath(Entity entity, DamageSource damageSource)
        {
            // ... (The OnEntityDeath method remains unchanged)
            if (sapi == null || !LoadedConfig.EnableCandyLoot) return;
            if (damageSource.SourceEntity is not EntityPlayer) return;
            string? matchedKey = null;
            foreach (var key in LoadedConfig.CandyLootTable.Keys)
            {
                if (WildcardUtil.Match(new AssetLocation(key), entity.Code))
                {
                    matchedKey = key;
                    break;
                }
            }
            if (matchedKey == null) return;
            if (LoadedConfig.CandyLootTable.TryGetValue(matchedKey, out string? lootConfigString))
            {
                if (string.IsNullOrEmpty(lootConfigString)) return;
                try
                {
                    string[] parts = lootConfigString.Split('@');
                    float chance = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                    if (sapi.World.Rand.NextDouble() >= chance) return;
                    string[] quantityParts = parts[1].Split('-');
                    int min = int.Parse(quantityParts[0]);
                    int max = (quantityParts.Length > 1) ? int.Parse(quantityParts[1]) : min;
                    int amount = sapi.World.Rand.Next(min, max + 1);
                    if (amount <= 0) return;
                    AssetLocation candyBagCode = new AssetLocation("spookynights", "candybag");
                    Item? candyBagItem = sapi.World.GetItem(candyBagCode);
                    if (candyBagItem != null)
                    {
                        ItemStack stack = new ItemStack(candyBagItem, amount);
                        sapi.World.SpawnItemEntity(stack, entity.ServerPos.XYZ);
                    }
                }
                catch (Exception e)
                {
                    sapi.Logger.Warning("[SpookyNights] Could not parse loot config string for '{0}'. Error: {1}", entity.Code, e.Message);
                }
            }
        }

        // THIS METHOD IS NOW CORRECTLY PLACED AT THE CLASS LEVEL
        // AND USES .Debug() FOR LOGGING
        private bool OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
        {
            if (properties.Code.Domain != "spookynights")
            {
                return true;
            }

            sapi.Logger.Debug("[SpookyNights] Game wants to spawn '{0}'. Checking our custom rules...", properties.Code);

            if (LoadedConfig.UseTimeBasedSpawning)
            {
                if (WildcardUtil.Match(new AssetLocation("spookynights:spectralbear-*"), properties.Code))
                {
                    if (LoadedConfig.BearSpawnConfig.Enabled)
                    {
                        string currentMoonPhase = sapi.World.Calendar.MoonPhase.ToString().ToLowerInvariant();
                        if (!LoadedConfig.BearSpawnConfig.AllowedMoonPhases.Contains(currentMoonPhase))
                        {
                            sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': incorrect moon phase ('{1}').", properties.Code, currentMoonPhase);
                            return false;
                        }
                    }
                }
                if (LoadedConfig.SpawnOnlyAtNight)
                {
                    float hour = sapi.World.Calendar.HourOfDay;
                    if (hour > 6 && hour < 20)
                    {
                        sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': it's daytime.", properties.Code);
                        return false;
                    }
                }
                if (LoadedConfig.AllowedSpawnMonths != null && LoadedConfig.AllowedSpawnMonths.Count > 0)
                {
                    int currentMonth = sapi.World.Calendar.Month;
                    if (!LoadedConfig.AllowedSpawnMonths.Contains(currentMonth))
                    {
                        sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': incorrect month ('{1}').", properties.Code, currentMonth);
                        return false;
                    }
                }
                if (LoadedConfig.SpawnOnlyOnLastDayOfMonth)
                {
                    int currentDay = (int)(sapi.World.Calendar.TotalDays % sapi.World.Calendar.DaysPerMonth) + 1;
                    int daysInMonth = sapi.World.Calendar.DaysPerMonth;
                    if (currentDay != daysInMonth)
                    {
                        sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': not the last day of the month.", properties.Code);
                        return false;
                    }
                }
                if (LoadedConfig.SpawnOnlyOnLastDayOfWeek)
                {
                    const int daysInWeek = 7;
                    int currentDayOfWeek = (int)sapi.World.Calendar.TotalDays % daysInWeek;
                    if (currentDayOfWeek != daysInWeek - 1)
                    {
                        sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': not the last day of the week.", properties.Code);
                        return false;
                    }
                }
            }

            sapi.Logger.Debug("[SpookyNights] Time checks PASSED for '{0}'. Now checking multiplier...", properties.Code);

            float multiplier = 1.0f;
            bool foundMatch = false;
            foreach (var pair in LoadedConfig.SpawnMultipliers)
            {
                if (WildcardUtil.Match(new AssetLocation(pair.Key), properties.Code))
                {
                    multiplier = pair.Value;
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
            {
                sapi.Logger.Debug("[SpookyNights] ALLOWING spawn for '{0}': no multiplier found, defaulting to allow.", properties.Code);
                return true;
            }

            if (multiplier <= 0)
            {
                sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': multiplier is {1}.", properties.Code, multiplier);
                return false;
            }

            if (multiplier >= 1)
            {
                sapi.Logger.Debug("[SpookyNights] ALLOWING spawn for '{0}': multiplier is {1}.", properties.Code, multiplier);
                return true;
            }

            if (sapi.World.Rand.NextDouble() > multiplier)
            {
                sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': failed chance roll with multiplier {1}.", properties.Code, multiplier);
                return false;
            }

            sapi.Logger.Debug("[SpookyNights] ALLOWING spawn for '{0}': passed chance roll with multiplier {1}.", properties.Code, multiplier);
            return true;
        }

    } 
} 