using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace SpookyNights
{
    [HarmonyPatch]
    public class HarmonyPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Item), "GetHeldItemInfo")]
        public static void Postfix_GetHeldItemInfo(Item __instance, ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if (__instance is ItemSpectralWeapon) return;
            if (__instance.Tool != EnumTool.Sword && __instance.Tool != EnumTool.Spear) return;

            // --- THE DEFINITIVE FIX IS HERE ---
            // This is the guard clause. If our text already exists, stop immediately to prevent duplication.
            string spectralPowerText = Lang.Get("spookynights:iteminfo-spectral-attack-power");
            if (dsc.ToString().Contains(spectralPowerText))
            {
                return;
            }
            // ------------------------------------

            string originalDescription = dsc.ToString();
            var lines = new List<string>(originalDescription.Split('\n'));
            string attackPowerLabel = Lang.Get("Attack power:");

            int index = lines.FindIndex(line => line.StartsWith(attackPowerLabel));

            if (index != -1)
            {
                float baseDamage = __instance.GetAttackPower(inSlot.Itemstack);
                if (baseDamage > 0)
                {
                    float spectralResistance = 0.5f;
                    float damageWithMalus = baseDamage * spectralResistance;

                    lines[index] = $"{attackPowerLabel} -{baseDamage:0.#} hp";

                    string spectralLine = $"<font color=\"#ff8080\">{spectralPowerText} -{damageWithMalus:0.0#} hp</font>";
                    lines.Insert(index + 1, spectralLine);

                    dsc.Clear().Append(string.Join("\n", lines));

                    string malusText = Lang.Get("spookynights:iteminfo-spectralmalus");
                    if (!dsc.ToString().Contains(malusText))
                    {
                        dsc.AppendLine(malusText);
                    }
                }
            }
        }
    }
}