using HarmonyLib;
using System.Globalization;
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
        private static readonly Regex arrowDamageRegex = new Regex(@"([+\-][0-9\.,]+) ([\w\s]+)");

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Item), "GetHeldItemInfo")]
        public static void Postfix_GetHeldItemInfo(Item __instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if (__instance.Attributes?["spectralDamageBonus"].Exists == true) return;

            bool isMeleeWeapon = __instance.Tool == EnumTool.Sword || __instance.Tool == EnumTool.Spear;
            bool isArrow = __instance is ItemArrow;
            if (!isMeleeWeapon && !isArrow) return;

            float spectralResistance = 0.5f;
            string originalDescription = dsc.ToString();
            string[] lines = originalDescription.Split('\n');
            string attackPowerLabel = Lang.Get("Attack power:");

            bool wasLineReplaced = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (isMeleeWeapon && lines[i].StartsWith(attackPowerLabel))
                {
                    float baseDamage = __instance.GetAttackPower(inSlot.Itemstack);
                    if (baseDamage == 0) continue;
                    float damageWithMalus = baseDamage * spectralResistance;
                    string malusDamageText = $"<font color=\"#ff8080\">({damageWithMalus.ToString("0.00")})</font>";
                    lines[i] = $"{attackPowerLabel} {baseDamage.ToString("0.#")} {malusDamageText} hp";
                    wasLineReplaced = true;
                    break;
                }

                Match match = arrowDamageRegex.Match(lines[i]);
                if (isArrow && match.Success)
                {
                    if (float.TryParse(match.Groups[1].Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out float baseDamage))
                    {
                        // THE FIX IS HERE: We calculate the final damage.
                        float damageWithMalus = baseDamage * spectralResistance;
                        string malusDamageText = $"<font color=\"#ff8080\">({damageWithMalus.ToString("0.00")})</font>";
                        lines[i] = $"{match.Groups[1].Value} {malusDamageText} {match.Groups[2].Value}";
                        wasLineReplaced = true;
                        break;
                    }
                }
            }

            if (wasLineReplaced)
            {
                dsc.Clear();
                dsc.Append(string.Join("\n", lines));

                string malusText = Lang.Get("spookynights:iteminfo-spectralmalus");
                if (!dsc.ToString().Contains(malusText))
                {
                    dsc.AppendLine(malusText);
                }
            }
        }
    }
}