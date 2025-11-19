using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // Used to parse the existing text
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

            // 2. Prepare Strings
            string spectralPowerText = Lang.Get("spookynights:iteminfo-spectral-attack-power");
            string attackPowerLabel = Lang.Get("Attack power:");

            // Get keywords to identify the ranged line (e.g., "piercing damage")
            // We strip the "{0}" placeholder to get the raw text
            string vanillaRangedFormat = Lang.Get("itemdescriptor-projectile-damage").Replace("{0}", "").Trim();
            if (string.IsNullOrEmpty(vanillaRangedFormat)) vanillaRangedFormat = "piercing"; // Fallback

            var lines = dsc.ToString().Split('\n').ToList();

            // 3. MELEE Handling
            float baseMeleeDamage = GetAttackPower(inSlot.Itemstack);
            int meleeIndex = lines.FindIndex(line => line.StartsWith(attackPowerLabel));

            if (meleeIndex != -1)
            {
                float totalSpectralMelee = baseMeleeDamage * effectiveMultiplier;
                // Added '-' sign as requested
                string spectralLine = $"<font color=\"#a08ee0\">{spectralPowerText} -{totalSpectralMelee:0.##} hp</font>";
                lines.Insert(meleeIndex + 1, spectralLine);
            }

            // 4. RANGED Handling (Parsing Strategy)
            // We look for the line that contains the vanilla ranged text
            int rangedIndex = lines.FindIndex(line => line.Contains(vanillaRangedFormat));

            if (rangedIndex != -1)
            {
                string vanillaLine = lines[rangedIndex];

                // Regex to find a decimal number in the string (handles 5.75 or 5,75)
                Match match = Regex.Match(vanillaLine, @"\d+([.,]\d+)?");

                if (match.Success)
                {
                    // Parse the number using normalized culture (replace comma with dot)
                    string numStr = match.Value.Replace(',', '.');
                    if (float.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float baseThrownDamage))
                    {
                        // Calculate Spectral Damage
                        float totalSpectralThrown = baseThrownDamage * effectiveMultiplier;

                        // Create the line
                        string rangedLabel = Lang.Get("spookynights:iteminfo-spectral-ranged-damage", totalSpectralThrown.ToString("0.##"));
                        string fullRangedLine = $"<font color=\"#a08ee0\">{rangedLabel}</font>";

                        // Insert immediately after
                        lines.Insert(rangedIndex + 1, fullRangedLine);
                    }
                }
            }

            // 5. Footer
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