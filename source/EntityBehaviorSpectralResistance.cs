using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace SpookyNights
{
    public class EntityBehaviorSpectralResistance : EntityBehavior
    {
        private float resistance;

        public EntityBehaviorSpectralResistance(Entity entity) : base(entity) { }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            this.resistance = attributes["resistance"].AsFloat(0.5f);
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            ItemStack? sourceStack = (damageSource.SourceEntity as EntityPlayer)?.Player.InventoryManager.ActiveHotbarSlot?.Itemstack;

            if (sourceStack == null)
            {
                damage *= this.resistance;
                return;
            }

            if (sourceStack.Collectible is ItemSpectralWeapon)
            {
                float spectralBonus = sourceStack.Attributes.GetFloat("spectralDamageBonus",
                    sourceStack.Collectible.Attributes?["spectralDamageBonus"].AsFloat(1f) ?? 1f);

                damage *= spectralBonus;
            }
            else
            {
                damage *= this.resistance;
            }
        }

        public override string PropertyName() => "spectralresistance";
    }
}