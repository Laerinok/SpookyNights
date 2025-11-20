using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace SpookyNights
{
    public class ItemSpookyCandy : Item
    {
        private static readonly Random rand = new Random();

        // --- TOOLTIP DISPLAY ---
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            string candyType = GetCandyType(inSlot.Itemstack);
            string descText = "";
            string color = "#DDDDDD";

            switch (candyType)
            {
                case "ghostcaramel":
                    descText = Lang.Get("spookynights:candy-desc-ghostcaramel");
                    color = "#BFEFFF";
                    break;
                case "mummy":
                    descText = Lang.Get("spookynights:candy-desc-mummy");
                    color = "#F0E68C";
                    break;
                case "spidergummy":
                    descText = Lang.Get("spookynights:candy-desc-spidergummy");
                    color = "#FFFFFF";
                    break;
                case "vampireteeth":
                    descText = Lang.Get("spookynights:candy-desc-vampireteeth");
                    color = "#FF3333";
                    break;
                case "shadowcube":
                    descText = Lang.Get("spookynights:candy-desc-shadowcube");
                    color = "#D02090";
                    break;
            }

            if (!string.IsNullOrEmpty(descText))
            {
                dsc.AppendLine($"\n<font color=\"{color}\">{descText}</font>");
            }
        }

        // --- INTERACTION LOGIC ---
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

            if (secondsUsed > 0.9f)
            {
                string candyType = GetCandyType(slot.Itemstack);

                // --- CLIENT SIDE: PHYSICS (Jump) ---
                if (api.Side == EnumAppSide.Client)
                {
                    if (candyType == "spidergummy")
                    {
                        // Vertical impulse + Forward dash
                        byEntity.SidedPos.Motion.Y += 0.35;
                        Vec3f look = byEntity.SidedPos.GetViewVector();
                        byEntity.SidedPos.Motion.X += look.X * 0.5;
                        byEntity.SidedPos.Motion.Z += look.Z * 0.5;
                    }
                }

                // --- SERVER SIDE: STATS & EFFECTS ---
                if (api.Side == EnumAppSide.Server)
                {
                    ApplyCandyEffect(byEntity, slot.Itemstack, candyType);
                }
            }
        }

        private void ApplyCandyEffect(EntityAgent entity, ItemStack stack, string candyType)
        {
            if (entity is not EntityPlayer playerEntity) return;
            IServerPlayer? serverPlayer = playerEntity.Player as IServerPlayer;
            if (serverPlayer == null) return;

            int particleColor = ColorUtil.ToRgba(200, 255, 255, 255);

            switch (candyType)
            {
                case "ghostcaramel":
                    // Bonus: Stability
                    double currentStab = entity.WatchedAttributes.GetDouble("temporalStability");
                    entity.WatchedAttributes.SetDouble("temporalStability", Math.Min(1.5, currentStab + 0.25));

                    // Malus: Weakness (30s) - Non persistent
                    entity.Stats.Set("meleeWeaponsDamage", "candy_weakness", -0.5f, false);

                    api.World.RegisterCallback((dt) => {
                        entity.Stats.Remove("meleeWeaponsDamage", "candy_weakness");
                        serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("spookynights:candy-expire-weakness"), EnumChatType.Notification);
                    }, 30000);

                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("spookynights:candy-msg-ghostcaramel"), EnumChatType.Notification);
                    particleColor = ColorUtil.ToRgba(200, 255, 255, 255); // Cyan
                    break;

                case "mummy":
                    // Bonus: Max Health Shield (45s) - Non persistent
                    entity.Stats.Set("maxhealth", "candy_shield", 10f, false);
                    entity.WatchedAttributes.MarkPathDirty("stats"); // Sync with client UI

                    // Bonus: Heal (+4)
                    entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Heal }, 4f);

                    // Malus: Slowness - Non persistent
                    entity.Stats.Set("walkspeed", "candy_slow", -0.25f, false);

                    api.World.RegisterCallback((dt) => {
                        entity.Stats.Remove("walkspeed", "candy_slow");
                        entity.Stats.Remove("maxhealth", "candy_shield");
                        entity.WatchedAttributes.MarkPathDirty("stats");
                        serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("spookynights:candy-expire-slow"), EnumChatType.Notification);
                    }, 45000);

                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("spookynights:candy-msg-mummy"), EnumChatType.Notification);
                    particleColor = ColorUtil.ToRgba(200, 240, 230, 140); // Sand
                    break;

                case "spidergummy":
                    // Bonus: No Fall Damage for 10 seconds (Safety net)
                    entity.Stats.Set("fallDamageMultiplier", "candy_feather", -1.0f, false);

                    api.World.RegisterCallback((dt) => {
                        entity.Stats.Remove("fallDamageMultiplier", "candy_feather");
                    }, 10000);

                    // Malus: Poison
                    api.World.RegisterCallback((dt) => {
                        entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.Poison }, 1f);
                    }, 1000);
                    api.World.RegisterCallback((dt) => {
                        entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = EnumDamageType.Poison }, 1f);
                    }, 3000);

                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("spookynights:candy-msg-spidergummy"), EnumChatType.Notification);
                    particleColor = ColorUtil.ToRgba(200, 200, 255, 200); // White/Green
                    break;

                case "vampireteeth":
                    // Bonus: Heal +6 HP
                    entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Heal }, 6f);

                    // Malus: Extreme Hunger Rate (20s) - Non persistent
                    entity.Stats.Set("hungerrate", "candy_vampire", 10.0f, false);

                    api.World.RegisterCallback((dt) => {
                        entity.Stats.Remove("hungerrate", "candy_vampire");
                    }, 20000);

                    // Malus: Direct Drain
                    DrainAllSatiety(entity);

                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("spookynights:candy-msg-vampireteeth"), EnumChatType.Notification);
                    particleColor = ColorUtil.ToRgba(200, 0, 0, 255); // Red
                    break;

                case "shadowcube":
                    ApplyShadowChaos(entity, serverPlayer);
                    particleColor = ColorUtil.ToRgba(200, 255, 0, 255); // Purple
                    break;
            }

            SpawnEatingParticles(entity, particleColor);
        }

        private void ApplyShadowChaos(EntityAgent entity, IServerPlayer serverPlayer)
        {
            int roll = rand.Next(0, 100);
            string msg = "";

            if (roll < 5) // DIVINE (5%)
            {
                entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Heal }, 20f);
                FillAllSatiety(entity);
                entity.WatchedAttributes.SetDouble("temporalStability", 1.5);
                msg = Lang.Get("spookynights:candy-msg-shadow-divine");
            }
            else if (roll < 25) // SPEED (20%)
            {
                entity.Stats.Set("walkspeed", "candy_speed", 0.5f, false);
                api.World.RegisterCallback((dt) => {
                    entity.Stats.Remove("walkspeed", "candy_speed");
                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("spookynights:candy-expire-speed"), EnumChatType.Notification);
                }, 60000);
                msg = Lang.Get("spookynights:candy-msg-shadow-speed");
            }
            else if (roll < 55) // HALLUCINATION (30%)
            {
                entity.WatchedAttributes.SetFloat("intoxication", 0.8f);
                entity.World.PlaySoundAt(new AssetLocation("spookynights:block/halloween-ghost-whisper"), entity);
                msg = Lang.Get("spookynights:candy-msg-shadow-hallucination");
            }
            else if (roll < 80) // DARKNESS (25%)
            {
                entity.Stats.Set("walkspeed", "candy_heavy_slow", -0.5f, false);
                RegisterParticleLoop(entity, 15, ColorUtil.ToRgba(255, 0, 0, 0)); // Black Cloud
                api.World.RegisterCallback((dt) => {
                    entity.Stats.Remove("walkspeed", "candy_heavy_slow");
                }, 15000);
                msg = Lang.Get("spookynights:candy-msg-shadow-blind");
            }
            else // TELEPORT FAIL (20%)
            {
                double dx = (rand.NextDouble() - 0.5) * 20;
                double dz = (rand.NextDouble() - 0.5) * 20;
                entity.TeleportToDouble(entity.Pos.X + dx, entity.Pos.Y + 1, entity.Pos.Z + dz);

                Block web = entity.World.GetBlock(new AssetLocation("game:cobweb-n-d"));
                if (web != null) entity.World.BlockAccessor.SetBlock(web.Id, entity.Pos.AsBlockPos);

                msg = Lang.Get("spookynights:candy-msg-shadow-fail");
            }

            serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.Notification);
        }

        // --- HELPER METHODS ---

        private string GetCandyType(ItemStack stack)
        {
            string candyType = stack.Attributes.GetString("type", "");
            if (string.IsNullOrEmpty(candyType))
            {
                string[] parts = this.Code.Path.Split('-');
                if (parts.Length > 1) candyType = parts[1];
            }
            return candyType;
        }

        private void DrainAllSatiety(EntityAgent entity)
        {
            ITreeAttribute hungerTree = entity.WatchedAttributes.GetTreeAttribute("hunger");
            if (hungerTree == null) return;

            hungerTree.SetFloat("saturation", 0);
            string[] categories = new string[] { "fruit", "vegetable", "grain", "protein", "dairy" };
            foreach (string cat in categories)
            {
                hungerTree.SetFloat(cat, 0f);
            }
            entity.WatchedAttributes.MarkPathDirty("hunger");
        }

        private void FillAllSatiety(EntityAgent entity)
        {
            ITreeAttribute hungerTree = entity.WatchedAttributes.GetTreeAttribute("hunger");
            if (hungerTree == null) return;

            hungerTree.SetFloat("saturation", 1500f);
            string[] categories = new string[] { "fruit", "vegetable", "grain", "protein", "dairy" };
            foreach (string cat in categories)
            {
                hungerTree.SetFloat(cat, 1500f);
            }
            entity.WatchedAttributes.MarkPathDirty("hunger");
        }

        private void RegisterParticleLoop(EntityAgent entity, int durationSeconds, int color)
        {
            long id = 0;
            int tickCount = 0;
            id = entity.Api.World.RegisterGameTickListener((dt) =>
            {
                if (tickCount >= durationSeconds * 10)
                {
                    entity.Api.World.UnregisterGameTickListener(id);
                    return;
                }
                SpawnCloudParticles(entity, color);
                tickCount++;
            }, 100);
        }

        private void SpawnCloudParticles(EntityAgent entity, int color)
        {
            SimpleParticleProperties particles = new SimpleParticleProperties(
               8, 12,
               color,
               new Vec3d(), new Vec3d(),
               new Vec3f(-0.5f, -0.5f, -0.5f),
               new Vec3f(0.5f, 0.5f, 0.5f),
               1.0f,
               0f,
               1.0f, 3.0f,
               EnumParticleModel.Quad
           );
            particles.MinPos = entity.ServerPos.XYZ.AddCopy(-1, entity.LocalEyePos.Y - 0.5, -1);
            particles.AddPos = new Vec3d(2, 1, 2);
            particles.WithTerrainCollision = false;
            particles.VertexFlags = 0;
            entity.World.SpawnParticles(particles);
        }

        private void SpawnEatingParticles(EntityAgent entity, int color)
        {
            SimpleParticleProperties particles = new SimpleParticleProperties(
                10, 15,
                color,
                new Vec3d(), new Vec3d(),
                new Vec3f(-0.2f, 0.1f, -0.2f),
                new Vec3f(0.2f, 0.5f, 0.2f),
                1.0f,
                -0.02f,
                0.2f, 0.5f,
                EnumParticleModel.Quad
            );
            particles.MinPos = entity.ServerPos.XYZ.AddCopy(-0.3, 1.0, -0.3);
            particles.AddPos = new Vec3d(0.6, 0.5, 0.6);
            particles.WithTerrainCollision = false;
            particles.VertexFlags = 255;
            particles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -255);
            entity.World.SpawnParticles(particles);
        }
    }
}