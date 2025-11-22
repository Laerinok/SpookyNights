using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

            if (baseDamage > 0)
            {
                float totalSpectralDamage = baseDamage * spectralBonus;

                // 2. Robust Line Detection (Numeric Search)
                var lines = dsc.ToString().Split('\n').ToList();

                string numStrDot = baseDamage.ToString("0.#", CultureInfo.InvariantCulture);
                string numStrComma = baseDamage.ToString("0.#", CultureInfo.GetCultureInfo("fr-FR"));

                int index = lines.FindIndex(l => l.Contains(numStrDot) || l.Contains(numStrComma));

                if (index != -1)
                {
                    string spectralPowerText = Lang.Get("spookynights:iteminfo-spectral-attack-power");
                    // Append negative number as it represents damage dealt to enemy
                    string spectralLine = $"<font color=\"#a08ee0\">{spectralPowerText}-{totalSpectralDamage:0.##} hp</font>";

                    lines.Insert(index + 1, spectralLine);
                    dsc.Clear().Append(string.Join("\n", lines));
                }
            }

            // 3. Append Spectral Bonus Footer
            if (spectralBonus > 1.001f)
            {
                string bonusText = Lang.Get("spookynights:iteminfo-spectralbonus-simplified", ((spectralBonus - 1) * 100).ToString("0"));
                if (!dsc.ToString().EndsWith("\n")) dsc.Append("\n");
                if (!dsc.ToString().Contains(bonusText)) dsc.Append(bonusText);
            }

            // 4. Append Stat Modifiers (Localized)
            if (inSlot.Itemstack.ItemAttributes.KeyExists("statModifiers"))
            {
                var mods = inSlot.Itemstack.ItemAttributes["statModifiers"];

                float walkMalus = mods["walkSpeed"].AsFloat(0f);
                float hungerMalus = mods["hungerrate"].AsFloat(0f);

                if (!dsc.ToString().EndsWith("\n")) dsc.Append("\n");

                if (walkMalus != 0)
                {
                    string color = walkMalus < 0 ? "#ff8080" : "#80ff80"; // Red if negative
                    string valStr = (walkMalus * 100).ToString("0.#");

                    string text = Lang.Get("spookynights:malus-walkspeed", valStr);
                    dsc.Append($"\n<font color=\"{color}\">{text}</font>");
                }
                if (hungerMalus != 0)
                {
                    string color = hungerMalus > 0 ? "#ff8080" : "#80ff80"; // Red if positive (hunger increases faster)
                    string valStr = "+" + (hungerMalus * 100).ToString("0.#");

                    string text = Lang.Get("spookynights:malus-hungerrate", valStr);
                    dsc.Append($"\n<font color=\"{color}\">{text}</font>");
                }
            }
        }
    }
}