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
            float spectralBonus = 0f;
            ItemStack? sourceStack = null;

            // Case: Damage from a direct melee attack by a player
            if (damageSource.SourceEntity is EntityPlayer player)
            {
                sourceStack = player.Player.InventoryManager.ActiveHotbarSlot?.Itemstack;
            }

            // Now, check the source item if it was held by a player
            if (sourceStack != null)
            {
                spectralBonus = sourceStack.Collectible.Attributes?["spectralDamageBonus"].AsFloat(0f) ?? 0f;
            }

            // Apply bonus or resistance based on the result
            if (spectralBonus > 0)
            {
                damage *= spectralBonus; // Apply bonus damage
            }
            else
            {
                damage *= this.resistance; // Apply resistance malus
            }
        }

        public override string PropertyName() => "spectralresistance";
    }
}