using HarmonyLib;
using Spookynights;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace SpookyNights
{
    public sealed class SpookyNightsModSystem : ModSystem
    {
        private ICoreAPI api = default!;
        private ICoreServerAPI sapi = default!;
        private List<string> spectralCreatureCodes = new List<string>();

        public override void StartPre(ICoreAPI api)
        {
            this.api = api;
            if (api.Side.IsServer()) { ConfigManager.LoadServerConfig(api); }
            if (api.Side.IsClient()) { ConfigManager.LoadClientConfig(api); }
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            if (api.Side.IsClient())
            {
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

            // Register all custom classes here
            new Harmony("fr.laerinok.spookynights").PatchAll();
            api.RegisterItemClass("ItemSpectralArrow", typeof(ItemSpectralArrow));
            api.RegisterItemClass("ItemSpectralSpear", typeof(ItemSpectralSpear));
            api.RegisterItemClass("ItemSpectralWeapon", typeof(ItemSpectralWeapon));
            api.RegisterItemClass("ItemCandyBag", typeof(ItemCandyBag));
            api.RegisterEntityBehaviorClass("spectralresistance", typeof(EntityBehaviorSpectralResistance));

            api.Logger.Notification("🌟 Spooky Nights is loaded!");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;

            if (ConfigManager.ServerConf != null)
            {
                sapi.Logger.Notification("[SpookyNights] Config loaded. Debug logging is set to: {0}", ConfigManager.ServerConf.EnableDebugLogging);
            }
            else
            {
                sapi.Logger.Error("[SpookyNights] CRITICAL: Server config was not loaded at StartServerSide!");
            }

            api.Event.OnEntityDeath += OnEntityDeath;
            api.Event.OnTrySpawnEntity += OnTrySpawnEntity;

            spectralCreatureCodes = new List<string> { "spectraldrifter", "spectralbowtorn", "spectralshiver", "spectralwolf", "spectralbear" };
            api.Event.RegisterGameTickListener(OnDaylightCheck, 5000);
        }

        private void OnEntityDeath(Entity entity, DamageSource damageSource)
        {

            if (damageSource == null) return;

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
                    ItemStack stack = new ItemStack(sapi.World.GetItem(new AssetLocation("spookynights", "candybag")), amount);
                    sapi.World.SpawnItemEntity(stack, entity.ServerPos.XYZ);
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

            bool debugEnabled = ConfigManager.ServerConf.EnableDebugLogging;

            if (debugEnabled)
            {
                sapi.Logger.Debug("[SpookyNights] Game wants to spawn '{0}'. Checking our custom rules...", properties.Code);
            }

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
                        if (bossConfig == null) continue;

                        if (bossConfig.Enabled)
                        {
                            if (!bossConfig.AllowedMoonPhases.Contains(currentMoonPhase))
                            {
                                if (debugEnabled) sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for boss '{0}': incorrect moon phase ('{1}').", properties.Code, currentMoonPhase);
                                return false;
                            }
                        }
                        else
                        {
                            if (debugEnabled) sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for boss '{0}': disabled in config.", properties.Code);
                            return false;
                        }
                        break;
                    }
                }

                if (!isHandledAsBoss)
                {
                    if (ConfigManager.ServerConf.SpawnOnlyOnFullMoon && currentMoonPhase != "full")
                    {
                        if (debugEnabled) sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': not a full moon.", properties.Code);
                        return false;
                    }

                    if (ConfigManager.ServerConf.SpawnOnlyAtNight)
                    {
                        bool isDarkEnough;
                        if (string.Equals(ConfigManager.ServerConf.NightTimeMode, "Manual", StringComparison.OrdinalIgnoreCase))
                        {
                            float hour = sapi.World.Calendar.HourOfDay;
                            float start = ConfigManager.ServerConf.NightStartHour;
                            float end = ConfigManager.ServerConf.NightEndHour;
                            if (start > end) { isDarkEnough = hour >= start || hour < end; }
                            else { isDarkEnough = hour >= start && hour < end; }
                        }
                        else
                        {
                            int lightLevel = sapi.World.BlockAccessor.GetLightLevel(spawnPosition.AsBlockPos, EnumLightLevelType.MaxLight);
                            isDarkEnough = lightLevel <= ConfigManager.ServerConf.LightLevelThreshold;
                        }

                        if (!isDarkEnough)
                        {
                            if (debugEnabled) sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': not dark enough.", properties.Code);
                            return false;
                        }
                    }
                    if (ConfigManager.ServerConf.AllowedSpawnMonths != null && ConfigManager.ServerConf.AllowedSpawnMonths.Count > 0)
                    {
                        int currentMonth = sapi.World.Calendar.Month;
                        if (!ConfigManager.ServerConf.AllowedSpawnMonths.Contains(currentMonth))
                        {
                            if (debugEnabled) sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': incorrect month ('{1}').", properties.Code, currentMonth);
                            return false;
                        }
                    }
                    if (ConfigManager.ServerConf.SpawnOnlyOnLastDayOfMonth)
                    {
                        int currentDay = (int)(sapi.World.Calendar.TotalDays % sapi.World.Calendar.DaysPerMonth) + 1;
                        if (currentDay != sapi.World.Calendar.DaysPerMonth)
                        {
                            if (debugEnabled) sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': not the last day of the month.", properties.Code);
                            return false;
                        }
                    }
                    if (ConfigManager.ServerConf.SpawnOnlyOnLastDayOfWeek)
                    {
                        const int daysInWeek = 7;
                        int currentDayOfWeek = (int)sapi.World.Calendar.TotalDays % daysInWeek;
                        if (currentDayOfWeek != daysInWeek - 1)
                        {
                            if (debugEnabled) sapi.Logger.Debug("[SpookyNights] CANCELLING spawn for '{0}': not the last day of the week.", properties.Code);
                            return false;
                        }
                    }
                }
            }

            if (debugEnabled)
            {
                sapi.Logger.Debug("[SpookyNights] Time checks PASSED for '{0}'. Now checking multiplier...", properties.Code);
            }

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

            if (!foundMatch) { return true; }

            if (!isHandledAsBoss && !ConfigManager.ServerConf.SpawnOnlyOnFullMoon && sapi.World.Calendar.MoonPhase.ToString().ToLowerInvariant() == "full")
            {
                if (debugEnabled)
                {
                    sapi.Logger.Debug("[SpookyNights] Applying full moon multiplier ({0}x) to '{1}'.", ConfigManager.ServerConf.FullMoonSpawnMultiplier, properties.Code);
                }
                multiplier *= ConfigManager.ServerConf.FullMoonSpawnMultiplier;
            }

            if (multiplier <= 0) { return false; }
            if (multiplier >= 1) { return true; }
            if (sapi.World.Rand.NextDouble() > multiplier) { return false; }

            return true;
        }

        private void OnDaylightCheck(float dt)
        {
            if (ConfigManager.ServerConf == null || !ConfigManager.ServerConf.SpawnOnlyAtNight) { return; }

            float hour = sapi.World.Calendar.HourOfDay;
            float start = ConfigManager.ServerConf.NightStartHour;
            float end = ConfigManager.ServerConf.NightEndHour;

            bool isDayTime;
            if (start > end) { isDayTime = hour >= end && hour < start; }
            else { isDayTime = hour < start || hour >= end; }

            if (isDayTime)
            {
                foreach (var entity in sapi.World.LoadedEntities.Values)
                {
                    if (IsSpectralCreature(entity.Code))
                    {
                        if (sapi.World.BlockAccessor.GetLightLevel(entity.ServerPos.AsBlockPos, EnumLightLevelType.TimeOfDaySunLight) > 0)
                        {
                            entity.Die(EnumDespawnReason.Expire);
                        }
                    }
                }
            }
        }

        private bool IsSpectralCreature(AssetLocation entityCode)
        {
            if (entityCode == null || entityCode.Domain != "spookynights") { return false; }
            foreach (var code in spectralCreatureCodes)
            {
                if (entityCode.Path.StartsWith(code)) { return true; }
            }
            return false;
        }
    }
}