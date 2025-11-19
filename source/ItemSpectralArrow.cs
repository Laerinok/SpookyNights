using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SpookyNights
{
    public class ItemSpectralArrow : ItemArrow
    {
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            // 1. Get Bonus
            float spectralBonus = inSlot.Itemstack.Attributes.GetFloat("spectralDamageBonus",
                Attributes?["spectralDamageBonus"].AsFloat(0f) ?? 0f
            );
            float effectiveMultiplier = spectralBonus > 0 ? spectralBonus : 1.0f;

            var lines = dsc.ToString().Split('\n').ToList();

            // 2. Get Base Damage directly from Attribute (Robust method)
            float baseDamage = Attributes?["spectralRangedDamage"].AsFloat(0f) ?? 0f;

            if (baseDamage > 0)
            {
                float totalSpectralDamage = baseDamage * effectiveMultiplier;

                // Use the same label key as spears for consistency
                string rangedLabel = Lang.Get("spookynights:iteminfo-spectral-ranged-damage", totalSpectralDamage.ToString("0.##"));
                string spectralLine = $"<font color=\"#a08ee0\">{rangedLabel}</font>";

                // 3. Find where to insert (Look for the damage number)
                string numStrDot = baseDamage.ToString(CultureInfo.InvariantCulture);
                string numStrComma = baseDamage.ToString(CultureInfo.GetCultureInfo("fr-FR"));

                int index = lines.FindLastIndex(line => line.Contains(numStrDot) || line.Contains(numStrComma));

                if (index != -1)
                {
                    lines.Insert(index + 1, spectralLine);
                }
                else
                {
                    lines.Add(spectralLine);
                }
            }

            // 4. Footer
            if (spectralBonus > 1.001f)
            {
                string bonusText = Lang.Get("spookynights:iteminfo-spectralbonus-simplified", ((spectralBonus - 1) * 100).ToString("0"));
                if (!lines.Contains(bonusText))
                {
                    lines.Add(bonusText);
                }
            }

            dsc.Clear().Append(string.Join("\n", lines));
        }
    }
}