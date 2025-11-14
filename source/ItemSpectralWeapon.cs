using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace SpookyNights
{
    public class ItemSpectralWeapon : Item
    {
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            float spectralBonus = inSlot.Itemstack.Attributes.GetFloat("spectralDamageBonus",
                Attributes?["spectralDamageBonus"].AsFloat(1f) ?? 1f
            );

            // This code now runs for ALL spectral weapons, including neutral ones.

            float baseDamage = GetAttackPower(inSlot.Itemstack);
            float totalSpectralDamage = baseDamage * spectralBonus;

            string originalDescription = dsc.ToString();
            string[] lines = originalDescription.Split('\n');
            string attackPowerLabel = Lang.Get("Attack power:");

            var newLines = new List<string>();
            bool inserted = false;

            foreach (string line in lines)
            {
                if (!inserted && line.StartsWith(attackPowerLabel))
                {
                    newLines.Add($"{attackPowerLabel} -{baseDamage:0.#} hp");

                    string spectralPowerText = Lang.Get("spookynights:iteminfo-spectral-attack-power");
                    string spectralLine = $"<font color=\"#a08ee0\">{spectralPowerText} -{totalSpectralDamage:0.0#} hp</font>";
                    newLines.Add(spectralLine);
                    inserted = true;
                }
                else
                {
                    newLines.Add(line);
                }
            }

            if (inserted)
            {
                dsc.Clear().Append(string.Join("\n", newLines));
            }

            // Only show the "+X% bonus" text if there is an actual bonus.
            if (spectralBonus > 1.001f)
            {
                string bonusText = Lang.Get("spookynights:iteminfo-spectralbonus-simplified", ((spectralBonus - 1) * 100).ToString("0"));
                if (!dsc.ToString().Contains(bonusText))
                {
                    dsc.AppendLine(bonusText);
                }
            }
        }
    }
}