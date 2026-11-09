using System;
using HarmonyLib;

namespace XicesBetterExperience
{
    [HarmonyPatch(typeof(InspectionUIManager), "OnInspectItem", new[] { typeof(GameItem) })]
    public static class AutoOpenAppraisalGuide
    {
        public static void Prefix(InspectionUIManager __instance)
        {
            AutoCloseAppraisalGuide.Postfix(GetBookIndex(__instance.currentInspectingItem));
        }

        // Run after the native inspection interface is initialized; never replace its behavior.
        public static void Postfix(GameItem __0, bool __runOriginal = true)
        {
            if (!__runOriginal || !ModEntry.IsEnabled || !ExperienceSettings.AutoOpenAppraisalGuide || __0 == null) return;
            try { OpenOwnedGuide(__0); }
            catch (Exception error)
            {
                // A guide/UI failure must not interrupt the player's original inspection.
                UnityEngine.Debug.LogError("[XicesBetterExperience] Could not open appraisal guide: " + error);
            }
        }

        internal static int GetBookIndex(GameItem inspected)
        {
            if (inspected == null) return -1;
            if (inspected.IsTag("INS_INJECTOR_TAG")) return -2;
            if (inspected.IsTag("HAND_TAG")) return 6;
            if (inspected.IsTag("RIBWICH_TAG")) return 4;
            if (inspected.IsTag("PLASMA_CELL_TAG")) return 0;
            return -1;
        }

        private static void OpenOwnedGuide(GameItem inspected)
        {
            int bookIndex = GetBookIndex(inspected);
            if (bookIndex == -2)
            {
                if (HealthData.CanRead() && HasOwnedGuide("injector_guide") && PaperUIManager.Instance != null &&
                    !PaperUIManager.Instance.injectorGuidePanel.activeSelf)
                    PaperUIManager.Instance.OpenInjectorGuide();
                return;
            }
            if (bookIndex < 0) return;
            string guideId = bookIndex == 6 ? "hand_guide" : bookIndex == 4 ? "ribwich_guide" : "fuel_guide";

            if (!PlayerStore.IsInstanceExist() || EmporiumEntry.Instance == null || !HealthData.CanRead()) return;
            MultiPageUIManager ui = MultiPageUIManager.Instance;
            if (ui == null) return;

            // FindAllItem(true) includes nested/hidden storage and the document inventory.
            // Check current ownership again rather than caching books across sales or loads.
            foreach (GameItem guide in PlayerStore.Instance.FindAllItem(true))
            {
                if (guide == null || guide.identifier != guideId || !GeneralHelper.IsItemOwned(guide)) continue;
                var element = guide as GameItemElement;
                if (element != null && element.IsDestroyed()) continue;
                // Native OpenUI resets to page one. Keep an already-open matching book intact.
                if (ui.panel.activeSelf && ui.books[bookIndex].activeSelf) return;
                ui.OpenUI(bookIndex);
                return;
            }
        }

        internal static bool HasOwnedGuide(string guideId)
        {
            if (!PlayerStore.IsInstanceExist() || EmporiumEntry.Instance == null) return false;
            foreach (GameItem guide in PlayerStore.Instance.FindAllItem(true))
            {
                if (guide == null || guide.identifier != guideId || !GeneralHelper.IsItemOwned(guide)) continue;
                var element = guide as GameItemElement;
                if (element == null || !element.IsDestroyed()) return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(InspectionUIManager), "CloseUI", new Type[] { })]
    public static class AutoCloseAppraisalGuide
    {
        public static void Prefix(InspectionUIManager __instance, out int __state)
        {
            // Native CloseUI clears currentInspectingItem, so capture the mapping first.
            __state = AutoOpenAppraisalGuide.GetBookIndex(__instance.currentInspectingItem);
        }

        public static void Postfix(int __state)
        {
            if (!ModEntry.IsEnabled || !ExperienceSettings.AutoOpenAppraisalGuide || __state == -1) return;
            try
            {
                if (__state == -2)
                {
                    PaperUIManager paper = PaperUIManager.Instance;
                    if (paper != null && paper.injectorGuidePanel.activeSelf) paper.CloseInjectorGuide();
                    return;
                }
                MultiPageUIManager ui = MultiPageUIManager.Instance;
                // The player may have closed the guide or switched to a different document.
                if (ui != null && ui.panel.activeSelf && ui.books[__state].activeSelf) ui.CloseUI();
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("[XicesBetterExperience] Could not close appraisal guide: " + error);
            }
        }
    }
}
