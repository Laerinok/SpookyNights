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
            // Read the resistance value from the entity's JSON attributes. Default to 0.5 if not specified.
            this.resistance = attributes["resistance"].AsFloat(0.5f);
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            // By default, assume the entity resists damage.
            float damageMultiplier = this.resistance;

            // Check if the damage comes from a player
            if (damageSource.SourceEntity is EntityPlayer player)
            {
                ItemSlot weaponSlot = player.Player.InventoryManager.ActiveHotbarSlot;

                if (weaponSlot != null && !weaponSlot.Empty)
                {
                    ItemStack weaponStack = weaponSlot.Itemstack;

                    // Check for the special attribute on the weapon. Default to 0 if not present.
                    float spectralBonus = weaponStack.Collectible.Attributes?["spectralDamageBonus"].AsFloat(0f) ?? 0f;

                    // If the weapon has a spectral bonus, use it as the multiplier instead of the resistance.
                    if (spectralBonus > 0)
                    {
                        damageMultiplier = spectralBonus;
                    }
                }
            }

            // Apply the final damage multiplier
            damage *= damageMultiplier;
        }

        public override string PropertyName() => "spectralresistance";
    }
}