using Vintagestory.API.Common;
using System;

namespace Spookynights
{
    public class OpenBagBehavior : CollectibleBehavior
    {
        private static readonly Random rand = new Random();

        public OpenBagBehavior(CollectibleObject collObj) : base(collObj) { }

        // Correct signature
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstTick, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (byEntity.Controls.Sneak) return;
            handHandling = EnumHandHandling.Handled;
        }

        // Correct signature
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            if (byEntity.Controls.Sneak) return false;
            float useDelay = collObj.Attributes["useDelay"].AsFloat(0.5f);
            return secondsUsed < useDelay;
        }

        // Correct signature for OnHeldInteractStop
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handled)
        {
            if (byEntity.Controls.Sneak) return;

            float useDelay = collObj.Attributes["useDelay"].AsFloat(0.5f);

            // Corrected typo: EnumAppSide instead of EnumAppAppSide
            if (secondsUsed >= useDelay && byEntity.World.Side == EnumAppSide.Server)
            {
                if (byEntity is EntityPlayer player)
                {
                    slot.TakeOut(1);
                    slot.MarkDirty();
                    GiveRandomCandy(player.Player);
                }
            }
        }

        private void GiveRandomCandy(IPlayer byPlayer)
        {
            AssetLocation[] candyTypes = new AssetLocation[]
            {
                new AssetLocation("spookynights", "spookycandy-ghostcaramel"),
                new AssetLocation("spookynights", "spookycandy-shadowcube"),
                new AssetLocation("spookynights", "spookycandy-mummy"),
                new AssetLocation("spookynights", "spookycandy-spidergummy"),
                new AssetLocation("spookynights", "spookycandy-vampireteeth")
            };

            int randomIndex = rand.Next(candyTypes.Length);
            AssetLocation randomCandyCode = candyTypes[randomIndex];
            int amount = rand.Next(1, 4);

            Item candyItem = byPlayer.Entity.World.GetItem(randomCandyCode);
            if (candyItem != null)
            {
                ItemStack candyStack = new ItemStack(candyItem, amount);
                if (!byPlayer.InventoryManager.TryGiveItemstack(candyStack))
                {
                    byPlayer.Entity.World.SpawnItemEntity(candyStack, byPlayer.Entity.SidedPos.XYZ);
                }
            }
        }
    }
}