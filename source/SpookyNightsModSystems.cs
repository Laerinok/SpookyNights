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

        public override void AssetsFinalize(ICoreAPI api)
        {
            if (LoadedConfig.EnableJackOLanternParticles) return;

            string[] variants = { "north", "east", "south", "west" };

            foreach (string variant in variants)
            {
                AssetLocation blockCode = new AssetLocation("spookynights", "jackolantern-" + variant);
                Block block = api.World.GetBlock(blockCode);

                if (block != null)
                {
                    block.ParticleProperties = null;
                }
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

        private void LoadAndMigrateConfig(ICoreAPI api)
        {
            ModConfig defaultConfig = new ModConfig();
            defaultConfig.EnableCandyLoot = true;
            defaultConfig.EnableJackOLanternParticles = true;

            defaultConfig.CandyLootTable = new Dictionary<string, string>()
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
                { "spookynights:spectralwolf-eurasian-adult-*", "0.3@1-2" }
            };

            defaultConfig.SpawnMultipliers = new Dictionary<string, float>()
            {
                { "spookynights:spectralwolf-*", 1.0f },
                { "spookynights:spectralbear-*", 1.0f },
                { "spookynights:spectraldrifter-*", 1.0f },
                { "spookynights:spectralshiver-*", 1.0f },
                { "spookynights:spectralbowtorn-*", 1.0f }
            };

            try
            {
                LoadedConfig = api.LoadModConfig<ModConfig>("spookynightsconfig.json");

                if (LoadedConfig == null)
                {
                    LoadedConfig = defaultConfig;
                    api.StoreModConfig(LoadedConfig, "spookynightsconfig.json");
                }
                else
                {
                    bool configUpdated = false;
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
                        LoadedConfig.CandyLootTable = defaultConfig.CandyLootTable;
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

        private bool OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
        {
            float multiplier = 1.0f;
            bool foundMatch = false;

            foreach (var pair in LoadedConfig.SpawnMultipliers)
            {
                if (WildcardUtil.Match(new AssetLocation(pair.Key), properties.Code))
                {
                    multiplier = pair.Value;
                    foundMatch = true;
                    // Log that we found a match!
                    sapi.Logger.Debug($"[SpookyNights] Found spawn multiplier '{multiplier}' for entity '{properties.Code}'.");
                    break;
                }
            }

            if (!foundMatch)
            {
                return true;
            }

            if (multiplier <= 0)
            {
                // Log the cancellation
                sapi.Logger.Notification($"[SpookyNights] Spawn CANCELLED for '{properties.Code}' (multiplier is 0).");
                return false;
            }

            if (multiplier >= 1)
            {
                return true;
            }

            if (sapi.World.Rand.NextDouble() > multiplier)
            {
                // Log the cancellation by chance
                sapi.Logger.Notification($"[SpookyNights] Spawn CANCELLED by chance for '{properties.Code}' (multiplier {multiplier}).");
                return false;
            }

            // Log the success
            sapi.Logger.Debug($"[SpookyNights] Spawn ALLOWED for '{properties.Code}' (multiplier {multiplier}).");
            return true;
        }

        private void OnEntityDeath(Entity entity, DamageSource damageSource)
        {
            if (sapi == null || !LoadedConfig.EnableCandyLoot) return;
            if (damageSource.SourceEntity is not EntityPlayer) return;

            string entityCode = entity.Code.ToString();
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
                    sapi.Logger.Warning("[SpookyNights] Could not parse loot config string for '{0}'. Expected format 'Chance@Min-Max'. String was: '{1}'. Error: {2}", entityCode, lootConfigString, e.Message);
                }
            }
        }
    }
}