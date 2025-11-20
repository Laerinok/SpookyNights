using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config; // For GlobalConstants
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace SpookyNights
{
    public class ItemSpookyCandy : Item
    {
        private static readonly Random rand = new Random();

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

            if (byEntity.World.Side == EnumAppSide.Server && secondsUsed > 0.9f)
            {
                ApplyCandyEffect(byEntity, slot.Itemstack);
            }
        }

        private void ApplyCandyEffect(EntityAgent entity, ItemStack stack)
        {
            if (entity is not EntityPlayer playerEntity) return;

            // FIX CS1061: Cast IPlayer to IServerPlayer to use SendMessage
            IServerPlayer? serverPlayer = playerEntity.Player as IServerPlayer;
            if (serverPlayer == null) return;

            string candyType = stack.Attributes.GetString("type", "");

            if (string.IsNullOrEmpty(candyType))
            {
                string[] parts = this.Code.Path.Split('-');
                if (parts.Length > 1) candyType = parts[1];
            }

            switch (candyType)
            {
                case "ghostcaramel":
                    // Effect: Temporal Stability Boost
                    double currentStab = entity.WatchedAttributes.GetDouble("temporalStability");
                    entity.WatchedAttributes.SetDouble("temporalStability", Math.Min(1.5, currentStab + 0.2));
                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "[SpookyNights] You feel lighter...", EnumChatType.Notification);
                    break;

                case "mummy":
                    // Effect: Heal
                    entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Heal }, 4f);
                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "[SpookyNights] Wrapped in comfort.", EnumChatType.Notification);
                    break;

                case "spidergummy":
                    // Effect: Sticky Web Trap (Funny)
                    Block web = entity.World.GetBlock(new AssetLocation("game:cobweb-n-d"));
                    if (web != null)
                    {
                        entity.World.BlockAccessor.SetBlock(web.Id, entity.Pos.AsBlockPos);
                        serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "[SpookyNights] Sticky!", EnumChatType.Notification);
                    }
                    break;

                case "vampireteeth":
                    // Effect: Life Steal (Big Heal + Hunger Malus)
                    entity.ReceiveDamage(new DamageSource() { Type = EnumDamageType.Heal }, 10f);
                    float saturation = entity.WatchedAttributes.GetTreeAttribute("hunger").GetFloat("saturation");
                    entity.WatchedAttributes.GetTreeAttribute("hunger").SetFloat("saturation", Math.Max(0, saturation - 200));
                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "[SpookyNights] The hunger... it grows...", EnumChatType.Notification);
                    break;

                case "shadowcube":
                    // Chaos Effect
                    ApplyShadowChaos(entity, serverPlayer);
                    break;
            }
        }

        private void ApplyShadowChaos(EntityAgent entity, IServerPlayer serverPlayer)
        {
            int effectId = rand.Next(0, 4);
            switch (effectId)
            {
                case 0: // Teleport
                    double dx = (rand.NextDouble() - 0.5) * 10;
                    double dz = (rand.NextDouble() - 0.5) * 10;
                    entity.TeleportToDouble(entity.Pos.X + dx, entity.Pos.Y + 1, entity.Pos.Z + dz);
                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "[SpookyNights] *POOF*", EnumChatType.Notification);
                    break;
                case 1: // Drunk
                    entity.WatchedAttributes.SetFloat("intoxication", 1.0f);
                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "[SpookyNights] Whoa... dizzy...", EnumChatType.Notification);
                    break;
                case 2: // Sound
                    entity.World.PlaySoundAt(new AssetLocation("spookynights:block/halloween-ghost-whisper"), entity);
                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "[SpookyNights] Did you hear that?", EnumChatType.Notification);
                    break;
                case 3: // Message Only
                    serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "[SpookyNights] You feel watched...", EnumChatType.Notification);
                    break;
            }
        }
    }
}