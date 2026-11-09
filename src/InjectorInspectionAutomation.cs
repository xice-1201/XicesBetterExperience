using System;
using HarmonyLib;
using UnityEngine.UI;

namespace XicesBetterExperience
{
    [HarmonyPatch(typeof(InspectionUIManager), "OnInspectItem", new[] { typeof(GameItem) })]
    public static class InjectorInspectionAutomation
    {
        private static InspectionUIManager owner;
        private static GameItem inspected;
        private static Toggle[] disabled;
        private static bool[] previous;
        private static readonly string[] reasons = {
            "ISSUE_INDICATOR", "ISSUE_LABEL", "ISSUE_SERIAL", "ISSUE_BODY", "ISSUE_COLOR"
        };

        public static void Prefix(InspectionUIManager __instance)
        {
            Restore();
        }

        public static void Postfix(InspectionUIManager __instance, GameItem __0, bool __runOriginal = true)
        {
            if (!__runOriginal || !ModEntry.IsEnabled || ExperienceSettings.InjectorAutomation == 0 ||
                __0 == null || !__0.IsTag("INS_INJECTOR_TAG") || GeneralHelper.IsItemOwned(__0) ||
                __0.IsTag("ALREADY_CONFRONTED_TAG") || InspectionUIManager.IsConfrontLocked() ||
                !__instance.panel.activeSelf || !__instance.confrontPanel.activeSelf) return;
            try
            {
                if (ExperienceSettings.InjectorAutomation == 1 && !AutoOpenAppraisalGuide.HasOwnedGuide("injector_guide")) return;
                var feature = __0.FindItemFeatureByCategory("CATEGORY_GENUINE_INJECTOR");
                if (feature == null || feature.GetCurrentItemCondition() == null ||
                    feature.GetCurrentItemCondition().identifier != "genuineInjector") return;
                Toggle[] toggles = { __instance.toggle1, __instance.toggle2, __instance.toggle3, __instance.toggle4, __instance.toggle5 };
                for (int i = 0; i < reasons.Length; i++)
                {
                    if (!__0.IsTag(reasons[i])) continue;
                    __instance.ResetAllConfrontToggle();
                    toggles[i].isOn = true;
                    return;
                }
                // Missing issue data is not proof of authenticity.
                if (feature.realCondition == null || feature.realCondition.identifier != "genuineInjector") return;
                if (__instance.panel.GetComponent<InjectorInspectionLifetime>() == null)
                    __instance.panel.AddComponent<InjectorInspectionLifetime>();
                __instance.ResetAllConfrontToggle();
                owner = __instance;
                inspected = __0;
                disabled = toggles;
                previous = new bool[toggles.Length];
                for (int i = 0; i < toggles.Length; i++)
                {
                    previous[i] = toggles[i].interactable;
                    toggles[i].interactable = false;
                }
            }
            catch (Exception error)
            {
                Restore();
                UnityEngine.Debug.LogError("[XicesBetterExperience] Injector inspection automation failed: " + error);
            }
        }

        internal static void Restore()
        {
            if (disabled != null)
                for (int i = 0; i < disabled.Length; i++)
                    if (disabled[i] != null) disabled[i].interactable = previous[i];
            disabled = null;
            previous = null;
            owner = null;
            inspected = null;
        }

        internal static void CheckSession()
        {
            if (disabled != null && (owner == null || !owner.panel.activeInHierarchy ||
                owner.currentInspectingItem != inspected || !ModEntry.IsEnabled || ExperienceSettings.InjectorAutomation == 0)) Restore();
        }
    }

    public sealed class InjectorInspectionLifetime : UnityEngine.MonoBehaviour
    {
        public void OnDisable() { InjectorInspectionAutomation.Restore(); }
        public void OnDestroy() { InjectorInspectionAutomation.Restore(); }
    }

    [HarmonyPatch(typeof(InspectionUIManager), "CloseUI")]
    public static class InjectorInspectionExit
    {
        public static void Prefix() { InjectorInspectionAutomation.Restore(); }
    }

    [HarmonyPatch(typeof(InspectionUIManager), "ResetUI")]
    public static class InjectorInspectionReset
    {
        public static void Prefix() { InjectorInspectionAutomation.Restore(); }
    }

    [HarmonyPatch(typeof(InspectionUIManager), "Update")]
    public static class InjectorInspectionSession
    {
        public static void Postfix() { InjectorInspectionAutomation.CheckSession(); }
    }
}
