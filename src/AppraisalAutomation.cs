using System;
using HarmonyLib;

namespace XicesBetterExperience
{
    [HarmonyPatch(typeof(InspectionUIManager), "OnInspectItem", new[] { typeof(GameItem) })]
    public static class AppraisalAutomation
    {
        private static bool submitting;

        public static bool Prefix(GameItem __0)
        {
            if (submitting || !ModEntry.IsEnabled || ExperienceSettings.AppraisalAutomationFor(__0) == 0) return true;
            int value;
            try
            {
                if (!Eligible(__0)) return true;
                value = AppraisalHelper.GetCorrectBaseValue(__0);
                if (value <= 0) return true;
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("[XicesBetterExperience] Could not prepare automatic appraisal: " + error);
                return true;
            }
            // Once native settlement starts, never fall back and offer a second settlement on failure.
            submitting = true;
            try { AppraisalServiceHelper.OnEstimate(__0, value); }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("[XicesBetterExperience] Automatic appraisal settlement failed: " + error);
            }
            finally { submitting = false; }
            return false;
        }

        private static bool Eligible(GameItem item)
        {
            if (item == null || !item.IsTag("APPRAISAL_ITEM") || GeneralHelper.IsItemOwned(item) ||
                !PlayerStore.IsInstanceExist() || EmporiumEntry.Instance == null) return false;
            var element = item as GameItemElement;
            if (element != null && element.IsDestroyed()) return false;
            var client = PlayerStore.Instance.currentClientInstance;
            var blueprint = client == null ? null : client.GetClientBlueprint();
            if (blueprint == null || blueprint.clientIntent != StoreClient.ClientIntent.APPRAISAL_SERVICE) return false;
            int requiredRank = ExperienceSettings.AppraisalAutomationFor(item) - 1;
            if (!PlayerStore.Instance.isAppraisalIntroduced || AppraisalRankHelper.GetRank() < requiredRank) return false;
            foreach (GameItem certificate in PlayerStore.Instance.FindAllItem(true))
            {
                if (certificate == null || !GeneralHelper.IsItemOwned(certificate)) continue;
                var certificateElement = certificate as GameItemElement;
                if (certificateElement != null && certificateElement.IsDestroyed()) continue;
                if (AppraisalRankHelper.GetCertificateRank(certificate) >= requiredRank) return true;
            }
            return false;
        }
    }

    // Keep the native integer slider and apply its value through the standard config system.
    [HarmonyPatch(typeof(ModConfigWindow), "BuildEntry")]
    public static class AppraisalAutomationSlider
    {
        private sealed class Selection { public CustomUIElement Label; public string Key; }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CustomUIElement, Selection> labels =
            new System.Runtime.CompilerServices.ConditionalWeakTable<CustomUIElement, Selection>();
        public static void Postfix(CustomUIBuilder b, ModConfig config, ConfigEntry entry)
        {
            if (config.ModId != ModEntry.ModId || !ExperienceSettings.IsAutomationSetting(entry.Key)) return;
            AddExtras(b, config, entry);
        }

        internal static void AddExtras(CustomUIBuilder b, ModConfig config, ConfigEntry entry)
        {
            if (config.ModId != ModEntry.ModId || !ExperienceSettings.IsAutomationSetting(entry.Key)) return;
            b.BeginRow();
            b.AddLabel(ModHelper.GetLocalized(ModEntry.ModId, "automation_selected"));
            b.AddLabel(SelectionName(entry.Key, Convert.ToInt32(entry.BoxedValue)));
            CustomUIElement selected = b.Last;
            b.End();
            CustomUIElement slider = b.Handle.Get(entry.Key);
            if (slider != null)
            {
                labels.Remove(slider); labels.Add(slider, new Selection { Label = selected, Key = entry.Key });
                slider.OnSlider(value => UpdateLabel(slider, value));
            }
        }

        internal static void UpdateLabel(CustomUIElement slider, float value)
        {
            Selection selected;
            if (labels.TryGetValue(slider, out selected) && selected.Label.IsAlive)
                selected.Label.SetText(SelectionName(selected.Key, (int)Math.Round(value)));
        }

        internal static string SelectionName(string key, int index)
        {
            if (key == "injector_inspection_automation")
                return ModHelper.GetLocalized(ModEntry.ModId, "injector_automation_level_" + Math.Max(0, Math.Min(2, index)));
            return LevelName(ExperienceSettings.AutomationLevel(key, index));
        }

        internal static string LevelName(int value)
        {
            return ModHelper.GetLocalized(ModEntry.ModId, "automation_level_" + Math.Max(0, Math.Min(4, value)));
        }
    }

    [HarmonyPatch(typeof(CustomUIElement), "SetFloat")]
    public static class AppraisalAutomationSliderReset
    {
        public static void Postfix(CustomUIElement __instance, float __0)
        {
            AppraisalAutomationSlider.UpdateLabel(__instance, __0);
        }
    }
}
