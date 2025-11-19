using HarmonyLib;
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
    [HarmonyPatch]
    public class HarmonyPatches
    {
        // PATCH 1: Melee (Swords & Spears)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Item), "GetHeldItemInfo")]
        public static void Postfix_Item_Melee(Item __instance, ItemSlot inSlot, StringBuilder dsc)
        {
            if (__instance is ItemSpectralWeapon || __instance is ItemSpectralSpear) return;
            if (__instance.Tool != EnumTool.Sword && __instance.Tool != EnumTool.Spear) return;

            string spectralPowerText = Lang.Get("spookynights:iteminfo-spectral-attack-power");
            if (dsc.ToString().Contains(spectralPowerText)) return;

            float spectralResistance = 0.5f;
            string attackPowerLabel = Lang.Get("Attack power:");

            var lines = dsc.ToString().Split('\n').ToList();
            float baseMeleeDamage = __instance.GetAttackPower(inSlot.Itemstack);

            int meleeIndex = lines.FindIndex(line => line.StartsWith(attackPowerLabel));
            if (meleeIndex != -1 && baseMeleeDamage > 0)
            {
                float damageWithMalus = baseMeleeDamage * spectralResistance;
                // Added '-' sign as requested
                string spectralLine = $"<font color=\"#ff8080\">{spectralPowerText} -{damageWithMalus:0.##} hp</font>";
                lines.Insert(meleeIndex + 1, spectralLine);
            }

            if (__instance.Tool == EnumTool.Sword)
            {
                lines.Add(Lang.Get("spookynights:iteminfo-spectralmalus"));
            }

            dsc.Clear().Append(string.Join("\n", lines));
        }

        // PATCH 2: Ranged (Spears Only)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemSpear), "GetHeldItemInfo")]
        public static void Postfix_Spear_Ranged(ItemSpear __instance, ItemSlot inSlot, StringBuilder dsc)
        {
            if (__instance is ItemSpectralSpear) return;

            float spectralResistance = 0.5f;
            var lines = dsc.ToString().Split('\n').ToList();

            // Get localized keyword for ranged damage (stripped of placeholders)
            string vanillaRangedFormat = Lang.Get("itemdescriptor-projectile-damage").Replace("{0}", "").Trim();
            if (string.IsNullOrEmpty(vanillaRangedFormat)) vanillaRangedFormat = "piercing";

            // Find the vanilla line
            int rangedIndex = lines.FindIndex(line => line.Contains(vanillaRangedFormat));

            if (rangedIndex != -1)
            {
                string vanillaLine = lines[rangedIndex];

                // Extract number
                Match match = Regex.Match(vanillaLine, @"\d+([.,]\d+)?");

                if (match.Success)
                {
                    string numStr = match.Value.Replace(',', '.');
                    if (float.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float baseThrownDamage))
                    {
                        // Calculate Malus
                        float thrownWithMalus = baseThrownDamage * spectralResistance;

                        string rangedLabel = Lang.Get("spookynights:iteminfo-spectral-ranged-damage", thrownWithMalus.ToString("0.##"));
                        string fullRangedLine = $"<font color=\"#ff8080\">{rangedLabel}</font>";

                        lines.Insert(rangedIndex + 1, fullRangedLine);
                    }
                }
            }

            string malusText = Lang.Get("spookynights:iteminfo-spectralmalus");
            if (!lines.Any(l => l.Contains(malusText)))
            {
                lines.Add(malusText);
            }

            dsc.Clear().Append(string.Join("\n", lines));
        }
        // PATCH 3: Arrows (Vanilla Malus)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemArrow), "GetHeldItemInfo")]
        public static void Postfix_Arrow(ItemArrow __instance, ItemSlot inSlot, StringBuilder dsc)
        {
            // Skip Custom Arrows
            if (__instance is ItemSpectralArrow) return;

            float spectralResistance = 0.5f;
            var lines = dsc.ToString().Split('\n').ToList();

            // Vanilla arrows store damage in attributes key "damage"
            float baseDamage = __instance.Attributes?["damage"].AsFloat(0f) ?? 0f;

            if (baseDamage > 0)
            {
                float damageWithMalus = baseDamage * spectralResistance;
                string rangedLabel = Lang.Get("spookynights:iteminfo-spectral-ranged-damage", damageWithMalus.ToString("0.##"));
                string spectralLine = $"<font color=\"#ff8080\">{rangedLabel}</font>";

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

            // Footer
            string malusText = Lang.Get("spookynights:iteminfo-spectralmalus");
            if (!lines.Any(l => l.Contains(malusText)))
            {
                lines.Add(malusText);
            }

            dsc.Clear().Append(string.Join("\n", lines));
        }
    }
}