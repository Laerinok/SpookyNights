using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace SpookyNights
{
    public class ItemWarScythe : Item
    {
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            // First, let the base game generate the default tooltip (Durability, Attack Power, etc.)
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            // --- Start of our custom logic ---

            float spectralBonus = Attributes?["spectralDamageBonus"].AsFloat(0f) ?? 0f;

            // If there's no bonus, we don't need to do anything else.
            if (spectralBonus <= 0) return;

            // --- 1. Modify the Attack Power line ---

            float baseDamage = GetAttackPower(inSlot.Itemstack);
            float totalSpectralDamage = baseDamage * spectralBonus;

            // We convert the StringBuilder to a string so we can manipulate it.
            string originalDescription = dsc.ToString();
            string[] lines = originalDescription.Split('\n');

            // This makes our code work in any language, by getting the translated "Attack power:" text.
            string attackPowerLabel = Lang.Get("Attack power:");

            bool wasLineReplaced = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(attackPowerLabel))
                {
                    // We found the line! Let's build our new, improved line.
                    string totalDamageText = $"<font color=\"#a08ee0\">({totalSpectralDamage.ToString("0.00")})</font>";
                    lines[i] = $"{attackPowerLabel} {baseDamage.ToString("0.#")} {totalDamageText} hp";

                    wasLineReplaced = true;
                    break; // No need to check other lines.
                }
            }

            // If we successfully replaced the line, we rebuild the main description text.
            if (wasLineReplaced)
            {
                dsc.Clear();
                dsc.Append(string.Join("\n", lines));
            }

            // --- 2. Append the lore and simplified bonus text at the end ---

            string baseDescriptionKey = Code.Domain + ":itemdesc-" + Code.Path;
            string translatedDescription = Lang.Get(baseDescriptionKey);

            if (translatedDescription != baseDescriptionKey)
            {
                dsc.AppendLine("\n" + translatedDescription);
            }

            // We use a new, simpler lang key for the bonus, as the damage value is now shown above.
            dsc.AppendLine(Lang.Get("spookynights:iteminfo-spectralbonus-simplified", ((spectralBonus - 1) * 100).ToString("0")));
        }
    }
}