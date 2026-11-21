using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    internal static class FreeStartDifficulty
    {
        internal const string Id = "xice.betterexperience.free_difficulty";

        internal static void Register()
        {
            ModHelper.AddDifficulty(ModEntry.ModId, Id, 0,
                () => FreeStartScenario.Text("free_difficulty_title"),
                () => FreeStartScenario.Text("free_difficulty_subtitle"),
                basedOn: "normal", iconKey: Difficulty.Normal.iconKey);
        }

        internal static void Remove() { ModHelper.RemoveDifficulties(ModEntry.ModId); }

        internal static bool IsFreeStart()
        {
            return NewGameData.Instance != null &&
                StartingScenario.ResolveId(NewGameData.Instance.startType,
                    NewGameData.Instance.startScenarioId) == FreeStartScenario.Id;
        }

        internal static bool CanSelect(string id)
        {
            return !ModEntry.IsEnabled || (IsFreeStart() ? id == Id : id != Id);
        }

        [HarmonyPatch(typeof(Difficulty), "GetEffectLines")]
        internal static class Description
        {
            static bool Prefix(Difficulty difficulty, ref List<string> __result)
            {
                if (!ModEntry.IsEnabled || difficulty == null || difficulty.id != Id) return true;
                // The native formatter omits unchanged rules for non-normal IDs.
                var display = new Difficulty { id = "normal", rules = Difficulty.Normal.rules };
                if (IsFreeStart()) display.rules = FreeStartEconomy.Preview(display.rules);
                __result = Difficulty.GetEffectLines(display);
                if (IsFreeStart())
                {
                    foreach (var effect in Difficulty.Effects)
                    {
                        if (effect.key == "supplier_markup")
                        {
                            int supplierIndex = __result.IndexOf(effect.describe(display.rules));
                            int total = ExperienceSettings.FreeStartRunnerMarkup + ExperienceSettings.FreeStartDoctorMarkup +
                                ExperienceSettings.FreeStartRetiredMarkup;
                            if (supplierIndex >= 0) __result[supplierIndex] = FreeStartScenario.Text(total <= 30
                                ? "free_difficulty_supplier_low" : total >= 100 ? "free_difficulty_supplier_high" : "free_difficulty_supplier_standard");
                            continue;
                        }
                        if (effect.key != "service_fees") continue;
                        int index = __result.IndexOf(effect.describe(display.rules));
                        if (index >= 0)
                        {
                            int sum = display.rules.powerCostMultiplier + display.rules.insuranceCostMultiplier;
                            string key = sum <= 0 ? "free_difficulty_service_low" : sum <= 2
                                ? "free_difficulty_service_standard" : "free_difficulty_service_high";
                            __result[index] = FreeStartScenario.Text(key);
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(NewGameDifficultyUI), "Open")]
        internal static class Selection
        {
            static void Prefix()
            {
                if (!ModEntry.IsEnabled || NewGameData.Instance == null) return;
                NewGameData data = NewGameData.Instance;
                if (IsFreeStart()) data.difficultyId = Id;
                else if (data.difficultyId == Id) data.difficultyId = "normal";
                if (data.difficultyId == Id || data.difficultyId == "normal") data.hardMode = false;
                // Open may reuse a hidden window. Rebuild if its selection no longer
                // matches the profession, without invoking the native back action.
                Difficulty selected = NewGameDifficultyUI.Selected;
                if (selected != null && (IsFreeStart() || !CanSelect(selected.id)))
                    NewGameDifficultyUI.CloseWithoutReturning();
            }
        }

        [HarmonyPatch(typeof(NewGameDifficultyUI), "BuildPlate")]
        internal static class DisabledAppearance
        {
            static void Postfix(Difficulty difficulty, NewGameDifficultyFrame ___frame)
            {
                if (CanSelect(difficulty.id) || ___frame == null) return;
                Transform plate = ___frame.platesHost.Find("plate_" + difficulty.id);
                if (plate == null) return;
                CanvasGroup group = plate.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0.4f;
                WildNetworkSlot slot = plate.GetComponent<WildNetworkSlot>();
                if (slot != null) { slot.onClick = null; slot.onHover = null; }
            }
        }

        [HarmonyPatch(typeof(NewGameDifficultyUI), "OnPlateClicked")]
        internal static class BlockSelection
        {
            static bool Prefix(string id, List<Difficulty> ___difficulties)
            {
                int index;
                return !ModEntry.IsEnabled ||
                    (int.TryParse(id, out index) && index >= 0 && index < ___difficulties.Count &&
                    CanSelect(___difficulties[index].id));
            }
        }

        [HarmonyPatch(typeof(NewGameDifficultyUI), "OnStartClicked")]
        internal static class ValidateStart
        {
            static bool Prefix()
            {
                return !ModEntry.IsEnabled || (NewGameDifficultyUI.Selected != null &&
                    CanSelect(NewGameDifficultyUI.Selected.id));
            }
        }
    }
}
