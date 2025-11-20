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
    public sealed class SpookyNightsModSystem : ModSystem
    {
        private ICoreAPI api = default!;
        private ICoreServerAPI sapi = default!;
        private List<string> spectralCreatureCodes = new List<string>();

        // --- LIFECYCLE ---

        public override void StartPre(ICoreAPI api)
        {
            this.api = api;
            if (api.Side.IsServer()) { ConfigManager.LoadServerConfig(api); }
            if (api.Side.IsClient()) { ConfigManager.LoadClientConfig(api); }
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            // Client-side: Check if we should disable particles based on config
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
            // 1. Determine particle color
            int particleColor = GetEntityGlowColor(entity);

            // 2. Spawn VFX
            SpawnSpectralParticles(entity.ServerPos.XYZ, particleColor);

            // 3. Process Drops from JSON
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

            // 4. Immediate Despawn
            entity.Die(EnumDespawnReason.Expire);
        }

        private int GetEntityGlowColor(Entity entity)
        {
            int defaultColor = ColorUtil.ToRgba(150, 0, 255, 255); // Default Cyan

            try
            {
                JsonObject? mainAttrs = entity.Properties.Attributes;
                if (mainAttrs == null) return defaultColor;

                // Priority 1: Simple string "glowColor" (Pre-resolved by engine)
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

                // Priority 2: Table "glowColorByType" (Manual resolution)
                if (mainAttrs.KeyExists("glowColorByType") && mainAttrs["glowColorByType"]?.Token is JObject glowTableByType)
                {
                    return ParseGlowTable(glowTableByType, entity, defaultColor);
                }
            }
            catch
            {
                // Fail silently, return default
            }

            return defaultColor;
        }

        private int ParseHexColor(string hexColor)
        {
            int rgb = ColorUtil.Hex2Int(hexColor);
            int r = (rgb >> 16) & 0xFF;
            int g = (rgb >> 8) & 0xFF;
            int b = rgb & 0xFF;
            // Returns: Alpha, Red, Green, Blue (VS ColorUtil specific mapping)
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
            particles.VertexFlags = 200; // Glow

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
                        // Open randomizers (like Jonas parts) immediately
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
                else // block
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

        private void HandleCandyLoot(Entity entity, DamageSource damageSource)
        {
            if (sapi == null || ConfigManager.ServerConf == null || !ConfigManager.ServerConf.EnableCandyLoot) return;

            // Seasonal Check
            if (ConfigManager.ServerConf.HalloweenEventOnly)
            {
                int currentMonth = sapi.World.Calendar.Month;
                if (currentMonth != 10) return; // Only Month 10 (October)
            }

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