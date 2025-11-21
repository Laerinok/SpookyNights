using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SpookyNights
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
            // Quantity: 1 to 2 candies per bag
            int amount = rand.Next(1, 3);

            for (int i = 0; i < amount; i++)
            {
                string candyCode = GetWeightedRandomCandy();
                Item candyItem = api.World.GetItem(new AssetLocation("spookynights", candyCode));

                if (candyItem != null)
                {
                    ItemStack candyStack = new ItemStack(candyItem, 1);

                    if (!byPlayer.InventoryManager.TryGiveItemstack(candyStack))
                    {
                        api.World.SpawnItemEntity(candyStack, byPlayer.Entity.SidedPos.XYZ);
                    }
                }
            }

            api.World.PlaySoundAt(new AssetLocation("game:sounds/player/collect"), byPlayer.Entity);
        }

        private string GetWeightedRandomCandy()
        {
            double roll = rand.NextDouble(); // 0.0 to 1.0

            // NEW Probability Table (Increased Shadow Cube):
            // 00% - 25% : Spider Gummy (Common)
            // 25% - 50% : Mummy (Common)
            // 50% - 70% : Ghost Caramel (Uncommon)
            // 70% - 85% : Vampire Teeth (Rare)
            // 85% - 100%: Shadow Cube (Rare)

            if (roll < 0.25) return "spookycandy-spidergummy";
            if (roll < 0.50) return "spookycandy-mummy";
            if (roll < 0.70) return "spookycandy-ghostcaramel";
            if (roll < 0.85) return "spookycandy-vampireteeth";

            return "spookycandy-shadowcube";
        }
    }
}