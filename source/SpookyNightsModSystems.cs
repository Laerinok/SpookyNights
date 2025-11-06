// In source/SpookyNightsModSystems.cs

using SpookyNights; // We keep this to access ConfigManager
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

// Note: using Newtonsoft.Json.Linq; is no longer needed here.

namespace Spookynights
{
    public sealed class SpookyNights : ModSystem
    {
        // The config properties are now in ConfigManager.
        // We can remove them from this file.

        private ICoreAPI api = default!;
        private ICoreServerAPI sapi = default!;
        private List<string> spectralCreatureCodes = new List<string>();

        public override void StartPre(ICoreAPI api)
        {
            this.api = api;

            // The logic is now neatly encapsulated in the ConfigManager.
            if (api.Side.IsServer())
            {
                ConfigManager.LoadServerConfig(api);
            }

            if (api.Side.IsClient())
            {
                ConfigManager.LoadClientConfig(api);
            }
        }

        // The LoadServerConfig and LoadClientConfig methods are now
        // in ConfigManager and can be completely removed from this file.

        public override void AssetsFinalize(ICoreAPI api)
        {
            if (api.Side.IsClient())
            {
                // We now access the client config via ConfigManager.ClientConf
                if (ConfigManager.ClientConf != null && !ConfigManager.ClientConf.EnableJackOLanternParticles)
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
            // All references to ServerConf are now prefixed with ConfigManager.
            if (sapi == null || ConfigManager.ServerConf == null || !ConfigManager.ServerConf.EnableCandyLoot) return;
            if (damageSource.SourceEntity is not EntityPlayer) return;
            string? matchedKey = null;
            foreach (var key in ConfigManager.ServerConf.CandyLootTable.Keys)
            {
                if (WildcardUtil.Match(new AssetLocation(key), entity.Code))
                {
                    matchedKey = key;
                    break;
                }
            }
            if (matchedKey == null) return;
            if (ConfigManager.ServerConf.CandyLootTable.TryGetValue(matchedKey, out string? lootConfigString))
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
            if (ConfigManager.ServerConf == null || properties.Code.Domain != "spookynights")
            {
                return true;
            }

            sapi.Logger.Debug("[SpookyNights] Game wants to spawn '{0}'. Checking our custom rules...", properties.Code);

            bool isHandledAsBoss = false;

            if (ConfigManager.ServerConf.UseTimeBasedSpawning)
            {
                string currentMoonPhase = sapi.World.Calendar.MoonPhase.ToString().ToLowerInvariant();

                foreach (var bossEntry in ConfigManager.ServerConf.Bosses)
                {
                    if (WildcardUtil.Match(new AssetLocation(bossEntry.Key), properties.Code))
                    {
                        isHandledAsBoss = true;
                        BossSpawningConfig bossConfig = bossEntry.Value;
                        if (bossConfig.Enabled)
                        {
                            if (!bossConfig.AllowedMoonPhases.Contains(currentMoonPhase))
                            {
                                sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for boss '{0}': incorrect moon phase ('{1}').", properties.Code, currentMoonPhase);
                                return false;
                            }
                        }
                        else
                        {
                            sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for boss '{0}': disabled in config.", properties.Code);
                            return false;
                        }
                        break;
                    }
                }

                if (!isHandledAsBoss)
                {
                    if (ConfigManager.ServerConf.SpawnOnlyOnFullMoon && currentMoonPhase != "full")
                    {
                        sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': not a full moon.", properties.Code);
                        return false;
                    }

                    if (ConfigManager.ServerConf.SpawnOnlyAtNight)
                    {
                        float hour = sapi.World.Calendar.HourOfDay;
                        if (hour > 6 && hour < 20)
                        {
                            sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': it's daytime.", properties.Code);
                            return false;
                        }
                    }
                    if (ConfigManager.ServerConf.AllowedSpawnMonths != null && ConfigManager.ServerConf.AllowedSpawnMonths.Count > 0)
                    {
                        int currentMonth = sapi.World.Calendar.Month;
                        if (!ConfigManager.ServerConf.AllowedSpawnMonths.Contains(currentMonth))
                        {
                            sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': incorrect month ('{1}').", properties.Code, currentMonth);
                            return false;
                        }
                    }
                    if (ConfigManager.ServerConf.SpawnOnlyOnLastDayOfMonth)
                    {
                        int currentDay = (int)(sapi.World.Calendar.TotalDays % sapi.World.Calendar.DaysPerMonth) + 1;
                        int daysInMonth = sapi.World.Calendar.DaysPerMonth;
                        if (currentDay != daysInMonth)
                        {
                            sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': not the last day of the month.", properties.Code);
                            return false;
                        }
                    }
                    if (ConfigManager.ServerConf.SpawnOnlyOnLastDayOfWeek)
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
            }

            sapi.Logger.Debug("[SpookyNights] Time checks PASSED for '{0}'. Now checking multiplier...", properties.Code);

            float multiplier = 1.0f;
            bool foundMatch = false;
            foreach (var pair in ConfigManager.ServerConf.SpawnMultipliers)
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

            if (!isHandledAsBoss && !ConfigManager.ServerConf.SpawnOnlyOnFullMoon && sapi.World.Calendar.MoonPhase.ToString().ToLowerInvariant() == "full")
            {
                sapi.Logger.Debug("[SpookyNights] Applying full moon multiplier ({0}x) to '{1}'.", ConfigManager.ServerConf.FullMoonSpawnMultiplier, properties.Code);
                multiplier *= ConfigManager.ServerConf.FullMoonSpawnMultiplier;
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

            sapi.Logger.Debug("[SpookyNights] ALLOWING spawn for '{0}': passed chance roll with multiplier {1}.", properties.Code);
            return true;
        }

        private void OnDaylightCheck(float dt)
        {
            if (ConfigManager.ServerConf == null || !ConfigManager.ServerConf.SpawnOnlyAtNight)
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