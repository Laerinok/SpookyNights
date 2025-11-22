using HarmonyLib;
using Spookynights;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SpookyNights
{
    // Helper structure for drop rates
    public struct CandyDropDefinition
    {
        public float Chance;
        public int MinQuantity;
        public int MaxQuantity;

        public CandyDropDefinition(float chance, int min, int max)
        {
            Chance = chance;
            MinQuantity = min;
            MaxQuantity = max;
        }
    }

    public sealed class SpookyNightsModSystem : ModSystem
    {
        private ICoreAPI api = default!;
        private ICoreServerAPI sapi = default!;
        private List<string> spectralCreatureCodes = new List<string>();

        // --- HARDCODED LOOT TABLE ---
        private static readonly Dictionary<string, CandyDropDefinition> LootTable = new Dictionary<string, CandyDropDefinition>()
        {
            // Weak
            { "spookynights:spectraldrifter-normal",      new CandyDropDefinition(0.15f, 1, 1) },
            { "spookynights:spectraldrifter-deep",        new CandyDropDefinition(0.20f, 1, 1) },
            
            // Medium
            { "spookynights:spectraldrifter-tainted",     new CandyDropDefinition(0.30f, 1, 1) },
            { "spookynights:spectralshiver-surface",      new CandyDropDefinition(0.25f, 1, 1) },
            { "spookynights:spectralshiver-deep",         new CandyDropDefinition(0.30f, 1, 1) },
            { "spookynights:spectralbowtorn-surface",     new CandyDropDefinition(0.25f, 1, 1) },
            { "spookynights:spectralwolf-eurasian-adult-*", new CandyDropDefinition(0.30f, 1, 1) },

            // Strong
            { "spookynights:spectraldrifter-corrupt",     new CandyDropDefinition(0.40f, 1, 2) },
            { "spookynights:spectraldrifter-nightmare",   new CandyDropDefinition(0.50f, 1, 2) },
            { "spookynights:spectralshiver-tainted",      new CandyDropDefinition(0.35f, 1, 2) },
            { "spookynights:spectralshiver-corrupt",      new CandyDropDefinition(0.40f, 2, 3) },
            { "spookynights:spectralshiver-nightmare",    new CandyDropDefinition(0.60f, 3, 5) },
            { "spookynights:spectralbowtorn-deep",        new CandyDropDefinition(0.35f, 1, 2) },
            { "spookynights:spectralbowtorn-tainted",     new CandyDropDefinition(0.40f, 2, 3) },
            { "spookynights:spectralbowtorn-corrupt",     new CandyDropDefinition(0.45f, 2, 4) },
            { "spookynights:spectralbear-brown-adult-*",  new CandyDropDefinition(0.50f, 1, 2) },

            // Elites
            { "spookynights:spectraldrifter-double-headed", new CandyDropDefinition(0.80f, 2, 3) },
            { "spookynights:spectralbowtorn-nightmare",     new CandyDropDefinition(0.65f, 3, 5) },
            { "spookynights:spectralbowtorn-gearfoot",      new CandyDropDefinition(0.75f, 4, 6) },
            { "spookynights:spectralshiver-stilt",          new CandyDropDefinition(0.70f, 4, 6) },
            { "spookynights:spectralshiver-bellhead",       new CandyDropDefinition(0.70f, 4, 6) },
            { "spookynights:spectralshiver-deepsplit",      new CandyDropDefinition(0.70f, 4, 6) },
            
            // Bosses
            { "spookynights:spectralbear-giant-adult-*",    new CandyDropDefinition(1.00f, 3, 5) }
        };


        // --- LIFECYCLE ---

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

            // Register Classes
            new Harmony("fr.laerinok.spookynights").PatchAll();
            api.RegisterEntityBehaviorClass("proximitywarning", typeof(EntityBehaviorProximityWarning));
            api.RegisterItemClass("ItemSpectralArrow", typeof(ItemSpectralArrow));
            api.RegisterItemClass("ItemSpectralSpear", typeof(ItemSpectralSpear));
            api.RegisterItemClass("ItemSpectralWeapon", typeof(ItemSpectralWeapon));
            api.RegisterItemClass("ItemCandyBag", typeof(ItemCandyBag));
            api.RegisterItemClass("ItemSpookyCandy", typeof(ItemSpookyCandy));

            // Register Behaviors
            api.RegisterEntityBehaviorClass("spectralresistance", typeof(EntityBehaviorSpectralResistance));
            api.RegisterEntityBehaviorClass("spectralhandling", typeof(EntityBehaviorSpectralHandling));

            api.Logger.Notification("🌟 Spooky Nights is loaded!");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Event.OnEntitySpawn += AddPlayerBehavior;
            api.Event.OnEntityLoaded += AddPlayerBehavior;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;

            if (ConfigManager.ServerConf != null)
            {
                sapi.Logger.Notification("[SpookyNights] Config loaded. Debug logging is set to: {0}", ConfigManager.ServerConf.EnableDebugLogging);
            }

            api.Event.OnEntityDeath += OnEntityDeath;
            api.Event.OnTrySpawnEntity += OnTrySpawnEntity;

            api.Event.OnEntitySpawn += AddPlayerBehavior;
            api.Event.OnEntityLoaded += AddPlayerBehavior;

            spectralCreatureCodes = new List<string> { "spectraldrifter", "spectralbowtorn", "spectralshiver", "spectralwolf", "spectralbear" };
            api.Event.RegisterGameTickListener(OnDaylightCheck, 5000);
        }

        // --- EVENTS ---

        private void AddPlayerBehavior(Entity entity)
        {
            if (entity is EntityPlayer)
            {
                if (!entity.HasBehavior("spectralhandling"))
                {
                    entity.AddBehavior(new EntityBehaviorSpectralHandling(entity));
                }
            }
        }

        private void OnEntityDeath(Entity entity, DamageSource damageSource)
        {
            if (damageSource == null) return;

            HandleCandyLoot(entity, damageSource);

            if (IsSpectralCreature(entity.Code))
            {
                HandleSpectralDeath(entity);
            }
        }

        // --- SPECTRAL DEATH LOGIC ---

        private void HandleSpectralDeath(Entity entity)
        {
            int particleColor = GetEntityGlowColor(entity);
            SpawnSpectralParticles(entity.ServerPos.XYZ, particleColor);

            if (entity.Properties.Attributes?["spectralDrops"]?.Token is JObject dropTable)
            {
                foreach (var entry in dropTable)
                {
                    if (WildcardUtil.Match(entry.Key, entity.Code.Path))
                    {
                        if (entry.Value is JArray drops)
                        {
                            foreach (var dropToken in drops)
                            {
                                TrySpawnDrop(dropToken, entity.ServerPos.XYZ);
                            }
                        }
                        break;
                    }
                }
            }

            entity.Die(EnumDespawnReason.Expire);
        }

        private int GetEntityGlowColor(Entity entity)
        {
            int defaultColor = ColorUtil.ToRgba(150, 0, 255, 255);

            try
            {
                JsonObject? mainAttrs = entity.Properties.Attributes;
                if (mainAttrs == null) return defaultColor;

                if (mainAttrs.KeyExists("glowColor"))
                {
                    JToken? token = mainAttrs["glowColor"]?.Token;
                    if (token != null && token.Type == JTokenType.String)
                    {
                        return ParseHexColor(token.ToString());
                    }
                    else if (token is JObject glowTable)
                    {
                        return ParseGlowTable(glowTable, entity, defaultColor);
                    }
                }

                if (mainAttrs.KeyExists("glowColorByType") && mainAttrs["glowColorByType"]?.Token is JObject glowTableByType)
                {
                    return ParseGlowTable(glowTableByType, entity, defaultColor);
                }
            }
            catch
            {
                // Fail silently
            }

            return defaultColor;
        }

        private int ParseHexColor(string hexColor)
        {
            int rgb = ColorUtil.Hex2Int(hexColor);
            int r = (rgb >> 16) & 0xFF;
            int g = (rgb >> 8) & 0xFF;
            int b = rgb & 0xFF;
            return ColorUtil.ToRgba(150, r, g, b);
        }

        private int ParseGlowTable(JObject glowTable, Entity entity, int defaultColor)
        {
            foreach (var entry in glowTable)
            {
                if (WildcardUtil.Match(entry.Key, entity.Code.Path))
                {
                    string? hexColor = entry.Value?.ToString();
                    if (string.IsNullOrEmpty(hexColor)) return defaultColor;
                    return ParseHexColor(hexColor);
                }
            }
            return defaultColor;
        }

        private void SpawnSpectralParticles(Vec3d pos, int color)
        {
            SimpleParticleProperties particles = new SimpleParticleProperties(
                15, 25,
                color,
                new Vec3d(), new Vec3d(),
                new Vec3f(-0.3f, 0f, -0.3f),
                new Vec3f(0.3f, 1.0f, 0.3f),
                2.0f,
                -0.05f,
                0.5f, 1.5f,
                EnumParticleModel.Quad
            );

            particles.MinPos = pos.AddCopy(-0.5, 0.2, -0.5);
            particles.AddPos = new Vec3d(1, 1.0, 1);
            particles.WithTerrainCollision = false;
            particles.VertexFlags = 200;

            particles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -255);
            particles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.5f);

            sapi.World.SpawnParticles(particles);
        }

        private void TrySpawnDrop(JToken dropToken, Vec3d pos)
        {
            try
            {
                JObject? drop = dropToken as JObject;
                if (drop == null) return;

                string? code = drop["code"]?.ToString();
                string? type = drop["type"]?.ToString();
                JToken? quantity = drop["quantity"];

                if (string.IsNullOrEmpty(code)) return;

                float avg = quantity?["avg"]?.ToObject<float>() ?? 1f;
                float var = quantity?["var"]?.ToObject<float>() ?? 0f;
                float finalQuantity = avg + ((float)sapi.World.Rand.NextDouble() * var) - (var / 2f);

                int stackSize = (int)finalQuantity;
                if (sapi.World.Rand.NextDouble() < (finalQuantity - stackSize)) stackSize++;

                if (stackSize <= 0) return;

                ItemStack? stack = null;

                if (type == "item")
                {
                    Item item = sapi.World.GetItem(new AssetLocation(code));
                    if (item != null)
                    {
                        if (item is ItemStackRandomizer randItem)
                        {
                            ItemStack tempStack = new ItemStack(item, 1);
                            DummySlot dummySlot = new DummySlot(tempStack);
                            randItem.Resolve(dummySlot, sapi.World, true);
                            stack = dummySlot.Itemstack;
                        }
                        else
                        {
                            stack = new ItemStack(item, stackSize);
                        }

                        if (stack != null) stack.StackSize = stackSize;
                    }
                }
                else
                {
                    Block block = sapi.World.GetBlock(new AssetLocation(code));
                    if (block != null) stack = new ItemStack(block, stackSize);
                }

                if (stack != null)
                {
                    sapi.World.SpawnItemEntity(stack, pos);
                }
            }
            catch (Exception ex)
            {
                sapi.Logger.Warning("[SpookyNights] Failed to spawn custom drop: " + ex.Message);
            }
        }

        // --- CANDY LOOT (UPDATED V1.7.0) ---

        private void HandleCandyLoot(Entity entity, DamageSource damageSource)
        {
            if (sapi == null || ConfigManager.ServerConf == null || !ConfigManager.ServerConf.EnableCandyLoot) return;

            // 1. Seasonal Check (Month)
            var allowedMonths = ConfigManager.ServerConf.AllowedCandyMonths;
            if (allowedMonths != null && allowedMonths.Count > 0)
            {
                int currentMonth = sapi.World.Calendar.Month;
                // If current month is NOT in the allowed list, stop.
                if (!allowedMonths.Contains(currentMonth)) return;
            }

            // 2. Moon Phase Check
            if (ConfigManager.ServerConf.CandyOnlyOnFullMoon)
            {
                // If it is NOT Full Moon, stop.
                if (sapi.World.Calendar.MoonPhase != EnumMoonPhase.Full) return;
            }

            if (damageSource.SourceEntity is not EntityPlayer) return;

            // Look for matching entity in Hardcoded LootTable
            CandyDropDefinition? dropDef = null;

            foreach (var entry in LootTable)
            {
                if (WildcardUtil.Match(new AssetLocation(entry.Key), entity.Code))
                {
                    dropDef = entry.Value;
                    break;
                }
            }

            if (dropDef == null) return;

            // Determine if drop happens
            if (sapi.World.Rand.NextDouble() >= dropDef.Value.Chance) return;

            // Determine quantity
            int amount = sapi.World.Rand.Next(dropDef.Value.MinQuantity, dropDef.Value.MaxQuantity + 1);
            if (amount <= 0) return;

            // Spawn
            ItemStack stack = new ItemStack(sapi.World.GetItem(new AssetLocation("spookynights", "candybag")), amount);
            sapi.World.SpawnItemEntity(stack, entity.ServerPos.XYZ);
        }

        // --- SPAWNING LOGIC ---

        private bool OnTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d spawnPosition, long herdId)
        {
            if (ConfigManager.ServerConf == null || properties.Code.Domain != "spookynights") return true;

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
                            if (!bossConfig.AllowedMoonPhases.Contains(currentMoonPhase)) return false;
                        }
                        else return false;
                        break;
                    }
                }

                if (!isHandledAsBoss)
                {
                    if (ConfigManager.ServerConf.SpawnOnlyOnFullMoon && currentMoonPhase != "full") return false;

                    if (ConfigManager.ServerConf.SpawnOnlyAtNight)
                    {
                        bool isDarkEnough;
                        if (string.Equals(ConfigManager.ServerConf.NightTimeMode, "Manual", StringComparison.OrdinalIgnoreCase))
                        {
                            float hour = sapi.World.Calendar.HourOfDay;
                            float start = ConfigManager.ServerConf.NightStartHour;
                            float end = ConfigManager.ServerConf.NightEndHour;
                            isDarkEnough = (start > end) ? (hour >= start || hour < end) : (hour >= start && hour < end);
                        }
                        else
                        {
                            int lightLevel = sapi.World.BlockAccessor.GetLightLevel(spawnPosition.AsBlockPos, EnumLightLevelType.MaxLight);
                            isDarkEnough = lightLevel <= ConfigManager.ServerConf.LightLevelThreshold;
                        }
                        if (!isDarkEnough) return false;
                    }

                    if (ConfigManager.ServerConf.AllowedSpawnMonths != null && ConfigManager.ServerConf.AllowedSpawnMonths.Count > 0)
                    {
                        if (!ConfigManager.ServerConf.AllowedSpawnMonths.Contains(sapi.World.Calendar.Month)) return false;
                    }
                    if (ConfigManager.ServerConf.SpawnOnlyOnLastDayOfMonth)
                    {
                        int currentDay = (int)(sapi.World.Calendar.TotalDays % sapi.World.Calendar.DaysPerMonth) + 1;
                        if (currentDay != sapi.World.Calendar.DaysPerMonth) return false;
                    }
                    if (ConfigManager.ServerConf.SpawnOnlyOnLastDayOfWeek)
                    {
                        const int daysInWeek = 7;
                        int currentDayOfWeek = (int)sapi.World.Calendar.TotalDays % daysInWeek;
                        if (currentDayOfWeek != daysInWeek - 1) return false;
                    }
                }
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

            if (!foundMatch) return true;

            if (!isHandledAsBoss && !ConfigManager.ServerConf.SpawnOnlyOnFullMoon && sapi.World.Calendar.MoonPhase.ToString().ToLowerInvariant() == "full")
            {
                multiplier *= ConfigManager.ServerConf.FullMoonSpawnMultiplier;
            }

            if (multiplier <= 0) return false;
            if (multiplier >= 1) return true;
            return sapi.World.Rand.NextDouble() <= multiplier;
        }

        private void OnDaylightCheck(float dt)
        {
            if (ConfigManager.ServerConf == null || !ConfigManager.ServerConf.SpawnOnlyAtNight) return;

            float hour = sapi.World.Calendar.HourOfDay;
            float start = ConfigManager.ServerConf.NightStartHour;
            float end = ConfigManager.ServerConf.NightEndHour;

            bool isDayTime = (start > end) ? (hour >= end && hour < start) : (hour < start || hour >= end);

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
            if (entityCode == null || entityCode.Domain != "spookynights") return false;
            foreach (var code in spectralCreatureCodes)
            {
                if (entityCode.Path.StartsWith(code)) return true;
            }
            return false;
        }
    }
}