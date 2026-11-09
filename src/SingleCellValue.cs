using System;
using HarmonyLib;

namespace XicesBetterExperience
{
    [HarmonyPatch(typeof(GameItemElement), "GetTooltipBasic")]
    internal static class SingleCellValueTooltip
    {
        [ThreadStatic]
        private static GameItemElement currentItem;

        public static void Prefix(GameItemElement __instance)
        {
            currentItem = __instance;
        }

        public static void Postfix()
        {
            currentItem = null;
        }

        [HarmonyPatch(typeof(RichTextBuilder), "AddLine")]
        internal static class EstimateLinePatch
        {
            public static void Prefix(ref string text)
            {
                GameItemElement item = currentItem;
                if (item == null || !ModEntry.IsEnabled || !ExperienceSettings.SingleCellValue ||
                    String.IsNullOrEmpty(text)) return;
                long totalEstimated = item.GetCurrentValue(false, true, true, false, 0L);
                long baseValue = item.GetBaseValue();
                string estimateMarker = "\u001eXICE_ESTIMATE\u001e";
                string baseMarker = "\u001eXICE_BASE\u001e";
                string template = LocHelper.GetLocalizedMechanic("mech_tooltip_est_value",
                    new object[] { estimateMarker, baseMarker });
                int estimateIndex = template.IndexOf(estimateMarker, StringComparison.Ordinal);
                int baseIndex = template.IndexOf(baseMarker, StringComparison.Ordinal);
                if (estimateIndex < 0 || baseIndex < estimateIndex) return;

                string nativeEstimate = Convert.ToString(totalEstimated);
                string nativeBase = Convert.ToString(baseValue);
                string prefix = template.Substring(0, estimateIndex) + nativeEstimate;
                string middle = template.Substring(estimateIndex + estimateMarker.Length,
                    baseIndex - estimateIndex - estimateMarker.Length);
                string suffix = middle + nativeBase + template.Substring(baseIndex + baseMarker.Length);
                if (!String.Equals(text, prefix + suffix, StringComparison.Ordinal)) return;

                // GridShape can be sparse (for example, pipe-shaped items).
                // Count occupied cells from the native shape mask instead of
                // multiplying its bounding-box dimensions.
                int cells = Math.Max(1, item.GetModifiedItemSize());
                // Match the native estimated-value line, including the item's
                // complete current value. Integer division deliberately drops
                // the fractional remainder, as the UI displays whole values.
                long selfEstimated = totalEstimated;
                string valueLine = String.Format(ModHelper.GetLocalized(ModEntry.ModId,
                    "single_cell_value_line"), selfEstimated / Math.Max(1, cells));
                text = prefix + valueLine + suffix;
            }
        }
    }
}
