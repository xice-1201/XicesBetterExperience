using System;
using HarmonyLib;

namespace XicesBetterExperience
{
    [HarmonyPatch(typeof(GameItemElement), "GetTooltipBasic")]
    internal static class SingleCellValueTooltip
    {
        internal sealed class Context { public GameItemElement Item; public string Expected, Replacement; public bool Prepared, Done; }
        [ThreadStatic] private static Context current;

        public static void Prefix(GameItemElement __instance, out Context __state)
        {
            __state = current;
            current = ModEntry.IsEnabled && ExperienceSettings.SingleCellValue ? new Context { Item = __instance } : null;
        }
        public static Exception Finalizer(Exception __exception, Context __state) { current = __state; return __exception; }

        [HarmonyPatch(typeof(RichTextBuilder), "AddLine")]
        internal static class EstimateLinePatch
        {
            public static void Prefix(ref string text)
            {
                Context context = current;
                if (context == null || context.Done || String.IsNullOrEmpty(text)) return;
                if (!context.Prepared) Prepare(context);
                if (context.Expected == null || !String.Equals(text, context.Expected, StringComparison.Ordinal)) return;
                text = context.Replacement;
                context.Done = true;
            }
            private static void Prepare(Context context)
            {
                context.Prepared = true;
                GameItemElement item = context.Item;
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
                context.Expected = prefix + suffix;
                context.Replacement = prefix + valueLine + suffix;
            }
        }
    }
}
