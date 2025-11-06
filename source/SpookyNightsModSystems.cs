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
        public static ServerConfig ServerConf { get; private set; } = default!;
        public static ClientConfig ClientConf { get; private set; } = default!;

        private ICoreAPI api = default!;
        private ICoreServerAPI sapi = default!;
        private List<string> spectralCreatureCodes = new List<string>();

        public override void StartPre(ICoreAPI api)
        {
            this.api = api;

            if (api.Side.IsServer())
            {
                LoadServerConfig(api);
            }

            if (api.Side.IsClient())
            {
                LoadClientConfig(api);
            }
        }

        private void LoadServerConfig(ICoreAPI api)
        {
            try
            {
                // Change is on the next line
                ServerConf = api.LoadModConfig<ServerConfig>("spookynights-server.json");
                if (ServerConf == null)
                {
                    ServerConf = new ServerConfig();
                    // And on the next line
                    api.StoreModConfig(ServerConf, "spookynights-server.json");
                }
                else
                {
                    ServerConfig defaultConfig = new ServerConfig();
                    if (ServerConf.Version != defaultConfig.Version)
                    {
                        // And on the next line
                        api.StoreModConfig(ServerConf, "spookynights-server.json");
                    }
                }
            }
            catch (Exception e)
            {
                api.Logger.Error("[SpookyNights] Failed to load or create server config file, using defaults: " + e.Message);
                ServerConf = new ServerConfig();
            }
        }

        private void LoadClientConfig(ICoreAPI api)
        {
            try
            {
                ClientConf = api.LoadModConfig<ClientConfig>("spookynights-client.json");
                if (ClientConf == null)
                {
                    ClientConf = new ClientConfig();
                    api.StoreModConfig(ClientConf, "spookynights-client.json");
                }
            }
            catch (Exception e)
            {
                api.Logger.Error("[SpookyNights] Failed to load or create client config file, using defaults: " + e.Message);
                ClientConf = new ClientConfig();
            }
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            if (api.Side.IsClient())
            {
                if (!ClientConf.EnableJackOLanternParticles)
                {
                    string[] variants = { "north", "east", "south", "west" };
                    foreach (string variant in variants)
                    {
                        AssetLocation blockCode = new AssetLocation("spookynights", "jackolantern-" + variant);
                        Block block = api.World.GetBlock(blockCode);
                        if (block != null) { block.ParticleProperties = null; }
                    }
                    api.Logger.Debug("[SpookyNights] Particles for jack-o'-lanterns have been disabled via client config.");
                }
            }
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

            spectralCreatureCodes = new List<string>
            {
                "spectraldrifter",
                "spectralbowtorn",
                "spectralshiver",
                "spectralwolf",
                "spectralbear"
            };
            api.Event.RegisterGameTickListener(OnDaylightCheck, 5000);
        }

        private void OnEntityDeath(Entity entity, DamageSource damageSource)
        {
            if (sapi == null || !ServerConf.EnableCandyLoot) return;
            if (damageSource.SourceEntity is not EntityPlayer) return;
            string? matchedKey = null;
            foreach (var key in ServerConf.CandyLootTable.Keys)
            {
                if (WildcardUtil.Match(new AssetLocation(key), entity.Code))
                {
                    matchedKey = key;
                    break;
                }
            }
            if (matchedKey == null) return;
            if (ServerConf.CandyLootTable.TryGetValue(matchedKey, out string? lootConfigString))
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

        private bool OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
        {
            if (properties.Code.Domain != "spookynights")
            {
                return true;
            }

            sapi.Logger.Debug("[SpookyNights] Game wants to spawn '{0}'. Checking our custom rules...", properties.Code);

            if (ServerConf.UseTimeBasedSpawning)
            {
                if (WildcardUtil.Match(new AssetLocation("spookynights:spectralbear-*"), properties.Code))
                {
                    if (ServerConf.BearSpawnConfig.Enabled)
                    {
                        string currentMoonPhase = sapi.World.Calendar.MoonPhase.ToString().ToLowerInvariant();
                        if (!ServerConf.BearSpawnConfig.AllowedMoonPhases.Contains(currentMoonPhase))
                        {
                            sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': incorrect moon phase ('{1}').", properties.Code, currentMoonPhase);
                            return false;
                        }
                    }
                }
                if (ServerConf.SpawnOnlyAtNight)
                {
                    float hour = sapi.World.Calendar.HourOfDay;
                    if (hour > 6 && hour < 20)
                    {
                        sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': it's daytime.", properties.Code);
                        return false;
                    }
                }
                if (ServerConf.AllowedSpawnMonths != null && ServerConf.AllowedSpawnMonths.Count > 0)
                {
                    int currentMonth = sapi.World.Calendar.Month;
                    if (!ServerConf.AllowedSpawnMonths.Contains(currentMonth))
                    {
                        sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': incorrect month ('{1}').", properties.Code, currentMonth);
                        return false;
                    }
                }
                if (ServerConf.SpawnOnlyOnLastDayOfMonth)
                {
                    int currentDay = (int)(sapi.World.Calendar.TotalDays % sapi.World.Calendar.DaysPerMonth) + 1;
                    int daysInMonth = sapi.World.Calendar.DaysPerMonth;
                    if (currentDay != daysInMonth)
                    {
                        sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': not the last day of the month.", properties.Code);
                        return false;
                    }
                }
                if (ServerConf.SpawnOnlyOnLastDayOfWeek)
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
            foreach (var pair in ServerConf.SpawnMultipliers)
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

        private void OnDaylightCheck(float dt)
        {
            if (!ServerConf.SpawnOnlyAtNight)
            {
                return;
            }

            bool isDayTime = sapi.World.Calendar.HourOfDay > 6 && sapi.World.Calendar.HourOfDay < 20;

            if (isDayTime)
            {
                foreach (var entity in sapi.World.LoadedEntities.Values)
                {
                    if (IsSpectralCreature(entity.Code))
                    {
                        entity.Die(EnumDespawnReason.Expire);
                    }
                }
            }
        }

        private bool IsSpectralCreature(AssetLocation entityCode)
        {
            if (entityCode == null || entityCode.Domain != "spookynights")
            {
                return false;
            }

            foreach (var code in spectralCreatureCodes)
            {
                if (entityCode.Path.StartsWith(code))
                {
                    return true;
                }
            }
            return false;
        }
    }
}