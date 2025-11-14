using HarmonyLib;
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
            // If the item already has the special bonus, do nothing.
            if (__instance.Attributes?["spectralDamageBonus"].Exists == true) return;

            // Only apply this logic to melee weapons.
            bool isMeleeWeapon = __instance.Tool == EnumTool.Sword || __instance.Tool == EnumTool.Spear;
            if (!isMeleeWeapon) return;

            float spectralResistance = 0.5f; // The damage malus against spectral entities
            string originalDescription = dsc.ToString();
            string[] lines = originalDescription.Split('\n');
            string attackPowerLabel = Lang.Get("Attack power:");

            bool wasLineReplaced = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(attackPowerLabel))
                {
                    float baseDamage = __instance.GetAttackPower(inSlot.Itemstack);
                    if (baseDamage == 0) continue;

                    float damageWithMalus = baseDamage * spectralResistance;
                    string malusDamageText = $"<font color=\"#ff8080\">({damageWithMalus.ToString("0.00")})</font>";
                    lines[i] = $"{attackPowerLabel} {baseDamage.ToString("0.#")} {malusDamageText} hp";
                    wasLineReplaced = true;
                    break;
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