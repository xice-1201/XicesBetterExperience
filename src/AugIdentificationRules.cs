using System;
using System.Text;

namespace XicesBetterExperience
{
    internal static class AugIdentificationRules
    {
        internal static bool Active(bool enabled, bool introduced, bool ended, bool isAug)
        { return enabled && introduced && !ended && isAug; }

        internal static bool IsSuspiciousKey(string key)
        {
            if (String.IsNullOrEmpty(key)) return false;
            return key.StartsWith("aug_dialog_budget_sus_", StringComparison.Ordinal) ||
                key == "aug_dialog_tier2_hint_req_b" || key == "aug_dialog_tier2_hint_unit_b" ||
                key == "aug_dialog_tier3_req_a" ||
                key.StartsWith("aug_dialog_med_t", StringComparison.Ordinal) ||
                key.StartsWith("aug_dialog_mat_t", StringComparison.Ordinal) ||
                key.StartsWith("aug_dialog_unholster_ob", StringComparison.Ordinal);
        }

        internal static string Normalize(string text)
        {
            if (String.IsNullOrEmpty(text)) return "";
            StringBuilder result = new StringBuilder(text.Length);
            bool tag = false;
            foreach (char c in text)
            {
                if (c == '<') { tag = true; continue; }
                if (c == '>') { tag = false; continue; }
                if (!tag && !Char.IsWhiteSpace(c)) result.Append(c);
            }
            return result.ToString();
        }
    }
}
