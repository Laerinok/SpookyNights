// In source/SpookyNightsModSystems.cs

using SpookyNights;
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

            // --- DEBUG LOG ---
            // Let's print the value we have in memory right after loading.
            api.Logger.Notification("[SpookyNights DEBUG] Final config value for EnableCandyLoot: {0}", LoadedConfig.EnableCandyLoot);

            // We register the event listener regardless of the config.
            // The check will happen inside the function itself.
            api.Event.OnEntityDeath += OnEntityDeath;
        }

        private void LoadAndMigrateConfig(ICoreAPI api)
        {
            // Note: You probably have more properties in your ModConfig now.
            // Make sure they are all present in your ModConfig.cs file.
            ModConfig defaultConfig = new ModConfig();

            try
            {
                LoadedConfig = api.LoadModConfig<ModConfig>("spookynightsconfig.json");

                if (LoadedConfig == null)
                {
                    api.Logger.Notification("[SpookyNights] No config file found. Creating a new one.");
                    LoadedConfig = defaultConfig;
                    api.StoreModConfig(LoadedConfig, "spookynightsconfig.json");
                }
                else if (LoadedConfig.Version != defaultConfig.Version)
                {
                    api.Logger.Notification("[SpookyNights] Config file is outdated. Updating...");

                    // IMPORTANT: When you add new config options, you must preserve them here.
                    // Let's assume you added EnableSeasonalSpawning to your ModConfig.cs
                    defaultConfig.EnableCandyLoot = LoadedConfig.EnableCandyLoot;
                    // defaultConfig.EnableSeasonalSpawning = LoadedConfig.EnableSeasonalSpawning; // etc. for all old properties

                    LoadedConfig = defaultConfig;
                    api.StoreModConfig(LoadedConfig, "spookynightsconfig.json");
                }
            }
            catch (System.Exception e)
            {
                api.Logger.Error("[SpookyNights] Failed to load or create config file, using defaults: " + e.Message);
                LoadedConfig = new ModConfig();
            }
        }

        private void OnEntityDeath(Entity entity, DamageSource damageSource)
        {
            // --- THIS IS THE INFALLIBLE ON/OFF SWITCH ---
            // This is the VERY FIRST thing the method does.
            // If the config value is false, the function stops immediately.
            if (!LoadedConfig.EnableCandyLoot) return;

            // The rest of the logic is unchanged.
            if (damageSource.SourceEntity is not EntityPlayer) return;
            if (entity.Properties.Attributes?["hostile"].AsBool() != true) return;

            if (sapi.World.Rand.NextDouble() < 0.05)
            {
                AssetLocation candyBagCode = new AssetLocation("spookynights", "candybag");
                Item candyBagItem = sapi.World.GetItem(candyBagCode);

                if (candyBagItem != null)
                {
                    ItemStack stack = new ItemStack(candyBagItem);
                    sapi.World.SpawnItemEntity(stack, entity.ServerPos.XYZ);
                }
            }
        }
    }
}