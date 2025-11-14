using HarmonyLib;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace SpookyNights
{
    public class ItemSpectralArrow : ItemArrow
    {
        private static readonly Regex arrowDamageRegex = new Regex(@"([+\-][0-9\.,]+) ([\w\s]+)");

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            float spectralBonus = Attributes?["spectralDamageBonus"].AsFloat(0f) ?? 0f;
            if (spectralBonus <= 0) return;

            float baseDamage = GetAttackPower(inSlot.Itemstack);
            // THE FIX IS HERE: We calculate the final damage.
            float totalSpectralDamage = baseDamage * spectralBonus;

            string originalDescription = dsc.ToString();
            string[] lines = originalDescription.Split('\n');
            bool wasLineReplaced = false;

            for (int i = 0; i < lines.Length; i++)
            {
                Match match = arrowDamageRegex.Match(lines[i]);
                if (match.Success)
                {
                    string bonusDamageText = $"<font color=\"#a08ee0\">({totalSpectralDamage.ToString("0.00")})</font>";
                    lines[i] = $"{match.Groups[1].Value} {bonusDamageText} {match.Groups[2].Value}";
                    wasLineReplaced = true;
                    break;
                }
            }

            if (wasLineReplaced)
            {
                dsc.Clear();
                dsc.Append(string.Join("\n", lines));
            }

            string bonusText = Lang.Get("spookynights:iteminfo-spectralbonus-simplified", ((spectralBonus - 1) * 100).ToString("0.00"));
            if (!dsc.ToString().Contains(bonusText))
            {
                dsc.AppendLine(bonusText);
            }

            string loreText = Lang.Get(Code.Domain + ":itemdesc-" + Code.Path);
            if (loreText != Code.Domain + ":itemdesc-" + Code.Path && !dsc.ToString().Contains(loreText))
            {
                dsc.AppendLine(loreText);
            }
        }
    }
}