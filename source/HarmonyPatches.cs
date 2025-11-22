using HarmonyLib;
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
    [HarmonyPatch]
    public class HarmonyPatches
    {
        // PATCH 1: Melee (Swords & Spears & Falx)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Item), "GetHeldItemInfo")]
        public static void Postfix_Item_Melee(Item __instance, ItemSlot inSlot, StringBuilder dsc)
        {
            if (__instance is ItemSpectralWeapon || __instance is ItemSpectralSpear) return;
            if (__instance.Tool != EnumTool.Sword && __instance.Tool != EnumTool.Spear) return;

            // GUARD: Prevent duplicates. If text exists, stop.
            string spectralPowerText = Lang.Get("spookynights:iteminfo-spectral-attack-power");
            if (dsc.ToString().Contains(spectralPowerText)) return;

            float spectralResistance = 0.5f;
            float baseMeleeDamage = __instance.GetAttackPower(inSlot.Itemstack);

            if (baseMeleeDamage > 0)
            {
                var lines = dsc.ToString().Split('\n').ToList();

                // SEARCH STRATEGY: Numeric Search (Most Robust)
                // Instead of looking for "Attack Power" text which varies by language/formatting,
                // we look for the line containing the damage number (e.g. "3.8" or "3,8").
                string numStrDot = baseMeleeDamage.ToString("0.#", CultureInfo.InvariantCulture);
                string numStrComma = baseMeleeDamage.ToString("0.#", CultureInfo.GetCultureInfo("fr-FR"));

                // We look for the specific line index
                int meleeIndex = lines.FindIndex(line => line.Contains(numStrDot) || line.Contains(numStrComma));

                if (meleeIndex != -1)
                {
                    float damageWithMalus = baseMeleeDamage * spectralResistance;
                    // Insert the spectral damage line just below the normal damage
                    string spectralLine = $"<font color=\"#ff8080\">{spectralPowerText}-{damageWithMalus:0.##} hp</font>";
                    lines.Insert(meleeIndex + 1, spectralLine);
                }

                // Add Malus Footer (Swords/Falx only)
                if (__instance.Tool == EnumTool.Sword)
                {
                    string malusText = Lang.Get("spookynights:iteminfo-spectralmalus");
                    // Only add if not already present
                    if (!lines.Any(l => l.Contains("50%")))
                    {
                        lines.Add(malusText);
                    }
                }

                dsc.Clear().Append(string.Join("\n", lines));
            }
        }

        // PATCH 2: Ranged (Spears Only)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemSpear), "GetHeldItemInfo")]
        public static void Postfix_Spear_Ranged(ItemSpear __instance, ItemSlot inSlot, StringBuilder dsc)
        {
            if (__instance is ItemSpectralSpear) return;

            // GUARD: Prevent duplicates
            string uniqueKey = Lang.Get("spookynights:iteminfo-spectral-ranged-damage", "").Trim();
            string checkStr = uniqueKey.Split(':')[0]; // Check label only
            if (dsc.ToString().Contains(checkStr)) return;

            float spectralResistance = 0.5f;
            var lines = dsc.ToString().Split('\n').ToList();

            string vanillaRangedFormat = Lang.Get("itemdescriptor-projectile-damage").Replace("{0}", "").Trim();
            if (string.IsNullOrEmpty(vanillaRangedFormat)) vanillaRangedFormat = "piercing";

            int rangedIndex = lines.FindIndex(line => line.Contains(vanillaRangedFormat));

            if (rangedIndex != -1)
            {
                string vanillaLine = lines[rangedIndex];
                // Extract number using Regex to handle decimals in any locale
                Match match = Regex.Match(vanillaLine, @"\d+([.,]\d+)?");

                if (match.Success)
                {
                    string numStr = match.Value.Replace(',', '.');
                    if (float.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float baseThrownDamage))
                    {
                        float thrownWithMalus = baseThrownDamage * spectralResistance;
                        string rangedLabel = Lang.Get("spookynights:iteminfo-spectral-ranged-damage", thrownWithMalus.ToString("0.##"));
                        string fullRangedLine = $"<font color=\"#ff8080\">{rangedLabel}</font>";

                        lines.Insert(rangedIndex + 1, fullRangedLine);
                    }
                }
            }

            // Ensure footer (Check generally for the 50% text to match duplications)
            string malusText = Lang.Get("spookynights:iteminfo-spectralmalus");
            if (!lines.Any(l => l.Contains("50%")))
            {
                lines.Add(malusText);
            }

            dsc.Clear().Append(string.Join("\n", lines));
        }

        // PATCH 3: Arrows (Vanilla)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemArrow), "GetHeldItemInfo")]
        public static void Postfix_Arrow(ItemArrow __instance, ItemSlot inSlot, StringBuilder dsc)
        {
            if (__instance is ItemSpectralArrow) return;

            // GUARD: Prevent duplicates
            string uniqueKey = Lang.Get("spookynights:iteminfo-spectral-ranged-damage", "").Trim();
            string checkStr = uniqueKey.Split(':')[0];
            if (dsc.ToString().Contains(checkStr)) return;

            float spectralResistance = 0.5f;
            var lines = dsc.ToString().Split('\n').ToList();

            float baseDamage = __instance.Attributes?["damage"].AsFloat(0f) ?? 0f;

            if (baseDamage > 0)
            {
                float damageWithMalus = baseDamage * spectralResistance;
                string rangedLabel = Lang.Get("spookynights:iteminfo-spectral-ranged-damage", damageWithMalus.ToString("0.##"));
                string spectralLine = $"<font color=\"#ff8080\">{rangedLabel}</font>";

                string numStrDot = baseDamage.ToString(CultureInfo.InvariantCulture);
                string numStrComma = baseDamage.ToString(CultureInfo.GetCultureInfo("fr-FR"));

                // Find the line containing the damage number
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

            string malusText = Lang.Get("spookynights:iteminfo-spectralmalus");
            if (!lines.Any(l => l.Contains("50%")))
            {
                lines.Add(malusText);
            }

            dsc.Clear().Append(string.Join("\n", lines));
        }
    }
}