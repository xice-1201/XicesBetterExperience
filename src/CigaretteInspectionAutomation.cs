using System;
using HarmonyLib;
using UnityEngine.UI;

namespace XicesBetterExperience
{
    [HarmonyPatch(typeof(InspectionUIManager), "OnInspectItem", new[] { typeof(GameItem) })]
    public static class CigaretteInspectionAutomation
    {
        private static InspectionUIManager owner;
        private static GameItem inspected;
        private static Toggle[] disabled;
        private static bool[] previous;
        private static readonly string[] reasons = {
            "CIGARETTE_LETTERING_ISSUE", "CIGARETTE_COLOR_ISSUE", "CIGARETTE_SEAL_ISSUE",
            "CIGARETTE_LOGO_ISSUE", "CIGARETTE_BOX_ISSUE"
        };

        private static bool Qualified()
        {
            int level = ExperienceSettings.CigaretteAutomation;
            if (level == 0) return false;
            if (level == 3) return true;
            return AutoOpenAppraisalGuide.HasOwnedGuide("cigarette_guide") &&
                (level == 2 || AutoOpenAppraisalGuide.HasOwnedGuide("cigarette_color"));
        }

        public static void Postfix(InspectionUIManager __instance, GameItem __0, bool __runOriginal = true)
        {
            if (!__runOriginal || !ModEntry.IsEnabled || __instance == null || __0 == null ||
                __0.identifier != "packet_red_cigarette" || !__0.IsTag("CIGARETTE_TAG") || GeneralHelper.IsItemOwned(__0) ||
                __0.IsTag("ALREADY_CONFRONTED_TAG") || InspectionUIManager.IsConfrontLocked() ||
                !__instance.panel.activeSelf || !__instance.confrontPanel.activeSelf) return;
            try
            {
                if (!Qualified()) return;
                var feature = __0.FindItemFeatureByCategory("CATEGORY_GENUINE_CIGARETTE");
                var claim = feature == null ? null : feature.GetCurrentItemCondition();
                if (claim == null || feature.realCondition == null) return;
                string actual = feature.realCondition.identifier;
                if ((claim.identifier != "genuineCigarette" && claim.identifier != "counterfeitCigarette") ||
                    (actual != "genuineCigarette" && actual != "counterfeitCigarette")) return;
                Toggle[] toggles = { __instance.toggle1, __instance.toggle2, __instance.toggle3, __instance.toggle4, __instance.toggle5 };
                if (claim.identifier == "genuineCigarette")
                {
                    if (actual == "genuineCigarette") DisableReasons(__instance, __0, toggles);
                    else
                    {
                        for (int i = 0; i < reasons.Length; i++)
                        {
                            if (!__0.IsTag(reasons[i])) continue;
                            __instance.ResetAllConfrontToggle();
                            toggles[i].isOn = true;
                            break;
                        }
                    }
                }
                else if (actual == "genuineCigarette")
                {
                    __instance.ResetAllConfrontToggle();
                    __instance.toggle1.isOn = true;
                }
                else DisableReasons(__instance, __0, new[] { __instance.toggle1 });
            }
            catch (Exception error)
            {
                Restore();
                UnityEngine.Debug.LogError("[XicesBetterExperience] Cigarette inspection automation failed: " + error);
            }
        }

        private static void DisableReasons(InspectionUIManager ui, GameItem item, Toggle[] toggles)
        {
            Restore();
            if (ui.panel.GetComponent<InjectorInspectionLifetime>() == null)
                ui.panel.AddComponent<InjectorInspectionLifetime>();
            ui.ResetAllConfrontToggle();
            owner = ui;
            inspected = item;
            disabled = toggles;
            previous = new bool[toggles.Length];
            for (int i = 0; i < toggles.Length; i++)
            {
                previous[i] = toggles[i].interactable;
                toggles[i].interactable = false;
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
                owner.currentInspectingItem != inspected || !ModEntry.IsEnabled || ExperienceSettings.CigaretteAutomation == 0)) Restore();
        }
    }
}
