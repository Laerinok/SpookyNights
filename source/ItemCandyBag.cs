using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Spookynights
{
    public class ItemCandyBag : Item
    {
        private static readonly Random rand = new Random();

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "spookynights:heldhelp-openbag",
                    MouseButton = EnumMouseButton.Right
                }
            };
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstTick, ref EnumHandHandling handHandling)
        {
            handHandling = EnumHandHandling.Handled;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            float useDelay = slot.Itemstack.Attributes.GetFloat("useDelay", 0.5f);
            return secondsUsed < useDelay;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            float useDelay = slot.Itemstack.Attributes.GetFloat("useDelay", 0.5f);
            if (secondsUsed < useDelay) return;

            if (api.Side != EnumAppSide.Server) return;

            if (byEntity is EntityPlayer entityPlayer)
            {
                slot.TakeOut(1);
                slot.MarkDirty();
                GiveRandomCandy(entityPlayer.Player);
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

            Item candyItem = api.World.GetItem(randomCandyCode);
            if (candyItem != null)
            {
                ItemStack candyStack = new ItemStack(candyItem, amount);
                if (!byPlayer.InventoryManager.TryGiveItemstack(candyStack))
                {
                    api.World.SpawnItemEntity(candyStack, byPlayer.Entity.SidedPos.XYZ);
                }
            }
        }
    }
}