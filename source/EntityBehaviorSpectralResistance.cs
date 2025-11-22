using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

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
            ItemStack? sourceStack = null;

            // Identify source item
            if (damageSource.SourceEntity is EntityAgent agent)
            {
                // Melee: Check player hand
                sourceStack = agent.RightHandItemSlot?.Itemstack;
            }
            else if (damageSource.SourceEntity is EntityProjectile projectile)
            {
                // Ranged: Check projectile contents
                sourceStack = projectile.ProjectileStack;
            }

            // No item found (fall damage, fire, empty hand) -> Apply resistance
            if (sourceStack == null)
            {
                damage *= this.resistance;
                return;
            }

            // Default to 0f to distinguish Vanilla (0) from Spectrium (1.0)
            float spectralBonus = sourceStack.Attributes.GetFloat("spectralDamageBonus",
                sourceStack.Collectible.Attributes?["spectralDamageBonus"].AsFloat(0f) ?? 0f);

            if (spectralBonus > 0f)
            {
                // Spectral weapon: Apply specific multiplier (1.0 = neutral, >1.0 = bonus)
                damage *= spectralBonus;
            }
            else
            {
                // Vanilla weapon: Apply resistance (malus)
                damage *= this.resistance;
            }
        }

        public override string PropertyName() => "spectralresistance";
    }
}