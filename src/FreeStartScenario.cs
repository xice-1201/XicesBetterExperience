using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    internal static class FreeStartScenario
    {
        internal const string Id = "xice.betterexperience.free_start";
        private static int? selectedCash;
        private static bool? selectedStorage;
        private static bool? selectedFood, selectedHousehold;
        private static bool? selectedNoSkipSupplies;
        private static bool? selectedWaterTools, selectedWaterContact;
        private static bool? selectedSmugglerStorage, selectedContraband, selectedBlackLamps, selectedExchange;
        internal static bool SuppressSkipSupplies()
        {
            return ModEntry.IsEnabled && PlayerStore.IsInstanceExist() &&
                PlayerStore.Instance.startType == NewGameData.StartType.MODDED &&
                PlayerStore.Instance.startScenarioId == Id &&
                (selectedNoSkipSupplies ?? ExperienceSettings.FreeStartNoSkipSupplies);
        }
        private static Sprite icon;
        internal static string Text(string key) { return ModHelper.GetLocalized(ModEntry.ModId, key); }
        internal static void Register()
        {
            StartingScenario.Register(new StartingScenario
            {
                id = Id, source = ModEntry.ModId, startType = NewGameData.StartType.MODDED,
                titleProvider = () => Text("free_start_title"),
                descriptionProvider = () => Text("free_start_description"),
                tagProvider = () => Text("free_start_tag"), tagTone = StartingScenario.TagTone.Positive,
                onStart = GiveStartingItems
            });
        }
        internal static void Remove()
        {
            FreeStartFundsPanel.HideCurrent();
            StartingScenario.RemoveMod(ModEntry.ModId);
            FreeStartExtras.Reset();
            selectedCash = null;
            selectedStorage = null; selectedFood = null; selectedHousehold = null; selectedNoSkipSupplies = null; selectedWaterTools = null; selectedWaterContact = null; selectedSmugglerStorage = null; selectedContraband = null; selectedBlackLamps = null; selectedExchange = null;
        }

        internal static void GiveStartingItems()
        {
            if (!ModEntry.IsEnabled || !PlayerStore.IsInstanceExist()) return;
            PlayerStore store = PlayerStore.Instance;
            if (store.startType != NewGameData.StartType.MODDED || store.startScenarioId != Id ||
                EmporiumEntry.Instance == null) return;
            if ((selectedStorage ?? ExperienceSettings.FreeStartStorage) &&
                !ModHelper.GetModDataBool(ModEntry.ModId, "free_start_storage_granted", false))
            {
                GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement,
                    ItemFeatureHelper.SetShabby(DirectoryMaster.Item("storage_bay")));
                ModHelper.SetModDataBool(ModEntry.ModId, "free_start_storage_granted", true);
            }
            GiveBundle(selectedFood ?? ExperienceSettings.FreeStartFood, "free_start_food_granted",
                new[] { "processed_juice", "processed_meat", "processed_meat" });
            GiveBundle(selectedHousehold ?? ExperienceSettings.FreeStartHousehold, "free_start_household_granted",
                new[] { "toothpaste", "toothpaste", "toilet_paper", "toilet_paper" });
            if ((selectedWaterTools ?? ExperienceSettings.FreeStartWaterTools) &&
                !ModHelper.GetModDataBool(ModEntry.ModId, "free_start_water_tools_granted", false))
            {
                FreeStartWater.GiveTools();
                ModHelper.SetModDataBool(ModEntry.ModId, "free_start_water_tools_granted", true);
            }
            if (selectedWaterContact ?? ExperienceSettings.FreeStartWaterContact)
            {
                store.networkUpgrade["WATER_TRADER"].state = 2;
                ModHelper.SetModDataBool(ModEntry.ModId, "free_start_water_contact", true);
            }
            FreeStartEquipment.Give();
            FreeStartContacts.Give();
            FreeStartFence.GiveItems(selectedSmugglerStorage ?? ExperienceSettings.FreeStartSmugglerStorage,
                selectedContraband ?? ExperienceSettings.FreeStartContraband,
                selectedBlackLamps ?? ExperienceSettings.FreeStartBlackLamps);
            if (selectedExchange ?? ExperienceSettings.FreeStartExchange)
            {
                store.exchangeManager.isExchangeUnlocked = true;
                ModHelper.SetModDataBool(ModEntry.ModId, "free_start_exchange", true);
            }
        }

        private static void GiveBundle(bool enabled, string key, string[] ids)
        {
            if (!enabled || ModHelper.GetModDataBool(ModEntry.ModId, key, false)) return;
            foreach (string id in ids)
                GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, DirectoryMaster.Item(id));
            ModHelper.SetModDataBool(ModEntry.ModId, key, true);
        }

        // A crisp, single-color pixel icon matching the native circular profession badges.
        internal static Sprite Icon()
        {
            if (icon != null) return icon;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            var ink = new Color(0.16f, 0.14f, 0.21f, 1f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 15.5f, dy = y - 15.5f;
                    float distance = dx * dx + dy * dy;
                    if (distance >= 13.2f * 13.2f && distance <= 14.4f * 14.4f) pixels[y * size + x] = ink;
                }
            string[] question = { "01110", "11011", "00011", "00110", "01100", "00000", "01100" };
            for (int row = 0; row < question.Length; row++)
                for (int column = 0; column < question[row].Length; column++)
                    if (question[row][column] == '1')
                        for (int y = 0; y < 2; y++)
                            for (int x = 0; x < 2; x++) pixels[(22 - row * 2 - y) * size + 11 + column * 2 + x] = ink;
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            icon = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
            icon.name = "xice_free_start_question";
            return icon;
        }

        [HarmonyPatch(typeof(StartingScenario), "GetVisible")]
        internal static class Sort
        {
            internal static void Postfix(List<StartingScenario> __result)
            {
                if (!ModEntry.IsEnabled || __result == null) return;
                int index = __result.FindIndex(s => s.id == Id);
                if (index <= 0) return;
                StartingScenario scenario = __result[index];
                __result.RemoveAt(index); __result.Insert(0, scenario);
            }
        }
        [HarmonyPatch(typeof(NewGameSpecializationUI), "IndexOfCurrentSelection")]
        internal static class DefaultSelection
        {
            internal static void Postfix(List<StartingScenario> ___scenarios, ref int __result)
            {
                if (ModEntry.IsEnabled && __result < 0)
                    __result = ___scenarios.FindIndex(s => s.startType == NewGameData.StartType.APPRENTICE);
            }
        }
        [HarmonyPatch(typeof(IconProvider), "GetIcon", new[] { typeof(string) })]
        internal static class ScenarioIcon
        {
            static void Postfix(string scenarioId, ref Sprite __result)
            {
                if (ModEntry.IsEnabled && scenarioId == Id) __result = Icon();
            }
        }
        [HarmonyPatch(typeof(NewGameSpecializationUI), "ResolveIcon")]
        internal static class MenuIcon
        {
            static void Postfix(StartingScenario scenario, ref Sprite __result)
            {
                if (ModEntry.IsEnabled && scenario != null && scenario.id == Id) __result = Icon();
            }
        }
        [HarmonyPatch(typeof(NewGameSpecializationUI), "ShowScenario")]
        internal static class Details
        {
            static void Prefix() { FreeStartFundsPanel.HideCurrent(); }
            static void Postfix(NewGameSpecializationFrame ___frame, CustomUIWindow ___window)
            {
                if (!ModEntry.IsEnabled || ___frame == null || NewGameSpecializationUI.Selected?.id != Id) return;
                // Native ShowScenario has cleared the old children. Only this scenario replaces its bullet list.
                FreeStartFundsPanel.Build(___frame, ___window);
            }
        }
        [HarmonyPatch(typeof(MainMenuUIController), "OnScenarioContinue")]
        internal static class Capture
        {
            internal static void Prefix(StartingScenario scenario)
            {
                FreeStartFundsPanel.CommitCurrent();
                FreeStartExtras.Capture(scenario);
                if (scenario != null && scenario.id == Id)
                    scenario.skipIntro = ModEntry.IsEnabled && ExperienceSettings.FreeStartForceSkip;
                selectedCash = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (int?)ExperienceSettings.FreeStartCash : null;
                selectedStorage = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartStorage : null;
                selectedFood = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartFood : null;
                selectedHousehold = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartHousehold : null;
                selectedNoSkipSupplies = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartNoSkipSupplies : null;
                selectedWaterTools = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartWaterTools : null;
                selectedWaterContact = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartWaterContact : null;
                selectedSmugglerStorage = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartSmugglerStorage : null;
                selectedContraband = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartContraband : null;
                selectedBlackLamps = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartBlackLamps : null;
                selectedExchange = ModEntry.IsEnabled && scenario != null && scenario.id == Id ? (bool?)ExperienceSettings.FreeStartExchange : null;
            }
        }
        [HarmonyPatch(typeof(NewGameData), "HardReset")]
        internal static class Reset { static void Postfix() { FreeStartExtras.Reset(); selectedCash = null; selectedStorage = null; selectedFood = null; selectedHousehold = null; selectedNoSkipSupplies = null; selectedWaterTools = null; selectedWaterContact = null; selectedSmugglerStorage = null; selectedContraband = null; selectedBlackLamps = null; selectedExchange = null; } }
        [HarmonyPatch(typeof(PlayerStore), "InitialSave")]
        internal static class Apply
        {
            internal static void Prefix(PlayerStore __instance)
            {
                if (!ModEntry.IsEnabled || __instance.startType != NewGameData.StartType.MODDED || __instance.startScenarioId != Id) return;
                FreeStartExtras.ApplyReputation();
                int cash = FreeStartRules.Clamp(selectedCash ?? ExperienceSettings.FreeStartCash);
                __instance.playerCash = cash;
                ModHelper.SetModDataInt(ModEntry.ModId, "free_start_cash", cash);
                FreeStartExtras.Reset();
            selectedCash = null;
                selectedStorage = null; selectedFood = null; selectedHousehold = null; selectedNoSkipSupplies = null; selectedWaterTools = null; selectedWaterContact = null; selectedSmugglerStorage = null; selectedContraband = null; selectedBlackLamps = null; selectedExchange = null;
            }
        }
    }
}
