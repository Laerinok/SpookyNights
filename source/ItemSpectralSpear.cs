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
    public class ItemSpectralSpear : ItemSpear
    {
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            // 1. Get Bonus Info
            float spectralBonus = inSlot.Itemstack.Attributes.GetFloat("spectralDamageBonus",
                Attributes?["spectralDamageBonus"].AsFloat(0f) ?? 0f
            );
            float effectiveMultiplier = spectralBonus > 0 ? spectralBonus : 1.0f;

            var lines = dsc.ToString().Split('\n').ToList();
            string spectralPowerText = Lang.Get("spookynights:iteminfo-spectral-attack-power");

            // 2. MELEE Handling (Robust Numeric Search)
            // Matches logic from ItemSpectralWeapon to fix missing lines in French
            float baseMeleeDamage = GetAttackPower(inSlot.Itemstack);
            
            if (baseMeleeDamage > 0)
            {
                string numStrDot = baseMeleeDamage.ToString("0.#", CultureInfo.InvariantCulture);
                string numStrComma = baseMeleeDamage.ToString("0.#", CultureInfo.GetCultureInfo("fr-FR"));

                // Find line containing the damage number (e.g. "4" or "-4")
                int meleeIndex = lines.FindIndex(line => line.Contains(numStrDot) || line.Contains(numStrComma));

                if (meleeIndex != -1)
                {
                    float totalSpectralMelee = baseMeleeDamage * effectiveMultiplier;
                    string spectralLine = $"<font color=\"#a08ee0\">{spectralPowerText}-{totalSpectralMelee:0.##} hp</font>";
                    lines.Insert(meleeIndex + 1, spectralLine);
                }
            }

            // 3. RANGED Handling (Regex Parsing)
            // Get localized keyword for ranged damage (stripped of placeholders)
            string vanillaRangedFormat = Lang.Get("itemdescriptor-projectile-damage").Replace("{0}", "").Trim();
            if (string.IsNullOrEmpty(vanillaRangedFormat)) vanillaRangedFormat = "piercing";

            int rangedIndex = lines.FindIndex(line => line.Contains(vanillaRangedFormat));

            if (rangedIndex != -1)
            {
                string vanillaLine = lines[rangedIndex];

                // Extract number from string
                Match match = Regex.Match(vanillaLine, @"\d+([.,]\d+)?");

                if (match.Success)
                {
                    string numStr = match.Value.Replace(',', '.');
                    if (float.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float baseThrownDamage))
                    {
                        float totalSpectralThrown = baseThrownDamage * effectiveMultiplier;

                        string rangedLabel = Lang.Get("spookynights:iteminfo-spectral-ranged-damage", totalSpectralThrown.ToString("0.##"));
                        string fullRangedLine = $"<font color=\"#a08ee0\">{rangedLabel}</font>";

                        lines.Insert(rangedIndex + 1, fullRangedLine);
                    }
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