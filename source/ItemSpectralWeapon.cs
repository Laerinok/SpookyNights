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

            // 1. Retrieve attributes
            float spectralBonus = inSlot.Itemstack.Attributes.GetFloat("spectralDamageBonus",
                Attributes?["spectralDamageBonus"].AsFloat(1f) ?? 1f
            );

            float baseDamage = GetAttackPower(inSlot.Itemstack);
            float totalSpectralDamage = baseDamage * spectralBonus;

            // 2. Rebuild the main description
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

            if (inserted || newLines.Count > lines.Length)
            {
                dsc.Clear().Append(string.Join("\n", newLines));
            }

            // 3. Append Spectral Bonus Footer
            if (spectralBonus > 1.001f)
            {
                string bonusText = Lang.Get("spookynights:iteminfo-spectralbonus-simplified", ((spectralBonus - 1) * 100).ToString("0"));
                if (!dsc.ToString().EndsWith("\n")) dsc.Append("\n");
                if (!dsc.ToString().Contains(bonusText)) dsc.Append(bonusText);
            }

            // 4. Append Stat Modifiers
            if (inSlot.Itemstack.ItemAttributes.KeyExists("statModifiers"))
            {
                var mods = inSlot.Itemstack.ItemAttributes["statModifiers"];

                float walkMalus = mods["walkSpeed"].AsFloat(0f);
                float hungerMalus = mods["hungerrate"].AsFloat(0f);

                if (!dsc.ToString().EndsWith("\n")) dsc.Append("\n");

                if (walkMalus != 0)
                {
                    string color = walkMalus < 0 ? "#ff8080" : "#80ff80";
                    dsc.Append($"\n<font color=\"{color}\">Walk Speed: {walkMalus * 100:0.#}%</font>");
                }
                if (hungerMalus != 0)
                {
                    // Hunger Rate > 0 is a malus (Red)
                    string color = hungerMalus > 0 ? "#ff8080" : "#80ff80";
                    dsc.Append($"\n<font color=\"{color}\">Hunger Rate: +{hungerMalus * 100:0.#}%</font>");
                }
            }
        }
    }
}