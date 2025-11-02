// In source/SpookyNightsModSystems.cs

using SpookyNights;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Spookynights
{
    public sealed class SpookyNights : ModSystem
    {
        public static ModConfig LoadedConfig { get; private set; } = default!;
        private ICoreServerAPI sapi = default!;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("ItemCandyBag", typeof(ItemCandyBag));
            api.Logger.Notification("🌟 Spooky Nights is loaded!");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            LoadAndMigrateConfig(api);
            api.Event.OnEntityDeath += OnEntityDeath;
        }

        private void LoadAndMigrateConfig(ICoreAPI api)
        {
            ModConfig defaultConfig = new ModConfig();
            defaultConfig.EnableCandyLoot = true;

            // Our new, simplified default config
            defaultConfig.CandyLootTable = new Dictionary<string, string>()
            {
                // Spectral Drifters
                { "spookynights:spectraldrifter-normal", "0.2@1" },
                { "spookynights:spectraldrifter-deep", "0.3@1-2" },
                { "spookynights:spectraldrifter-tainted", "0.35@1-2" },
                { "spookynights:spectraldrifter-corrupt", "0.4@2-3" },
                { "spookynights:spectraldrifter-nightmare", "0.6@3-5" },
                { "spookynights:spectraldrifter-double-headed", "0.7@4-6" },

                // Spectral Shivers
                { "spookynights:spectralshiver-surface", "0.2@1" },
                { "spookynights:spectralshiver-deep", "0.3@1-2" },
                { "spookynights:spectralshiver-tainted", "0.35@1-2" },
                { "spookynights:spectralshiver-corrupt", "0.4@2-3" },
                { "spookynights:spectralshiver-nightmare", "0.6@3-5" },
                { "spookynights:spectralshiver-stilt", "0.7@4-6" },
                { "spookynights:spectralshiver-bellhead", "0.7@4-6" },
                { "spookynights:spectralshiver-deepsplit", "0.7@4-6" },
                
                // Spectral Bowtorn
                { "spookynights:spectralbowtorn-surface", "0.25@1" },
                { "spookynights:spectralbowtorn-deep", "0.35@1-2" },
                { "spookynights:spectralbowtorn-tainted", "0.4@2-3" },
                { "spookynights:spectralbowtorn-corrupt", "0.45@2-4" },
                { "spookynights:spectralbowtorn-nightmare", "0.65@3-5" },
                { "spookynights:spectralbowtorn-gearfoot", "0.75@4-6" },

                // Spectral Animals (using wildcard *)
                { "spookynights:spectralbear-brown-adult-*", "0.5@2-4" },
                { "spookynights:spectralwolf-eurasian-adult-*", "0.3@1-2" }
            };

            // The rest of the load logic is similar
            try
            {
                LoadedConfig = api.LoadModConfig<ModConfig>("spookynightsconfig.json");

                if (LoadedConfig == null)
                {
                    LoadedConfig = defaultConfig;
                    api.StoreModConfig(LoadedConfig, "spookynightsconfig.json");
                }
                else if (LoadedConfig.Version != defaultConfig.Version)
                {
                    defaultConfig.EnableCandyLoot = LoadedConfig.EnableCandyLoot;
                    LoadedConfig = defaultConfig;
                    api.StoreModConfig(LoadedConfig, "spookynightsconfig.json");
                }
            }
            catch (Exception e)
            {
                api.Logger.Error("[SpookyNights] Failed to load or create config file, using defaults: " + e.Message);
                LoadedConfig = new ModConfig();
            }
        }

        private void OnEntityDeath(Entity entity, DamageSource damageSource)
        {
            if (!LoadedConfig.EnableCandyLoot) return;
            if (damageSource.SourceEntity is not EntityPlayer) return;

            string entityCode = entity.Code.ToString();

            // Your correction is here, and it's perfect.
            if (LoadedConfig.CandyLootTable.TryGetValue(entityCode, out string? lootConfigString))
            {
                // Because of the 'if', we know lootConfigString cannot be null here.
                // However, it's good practice to add a check for safety.
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