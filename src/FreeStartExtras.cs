using System;
using System.Collections.Generic;
using HarmonyLib;

namespace XicesBetterExperience
{
    internal static class FreeStartExtras
    {
        private static int[] reputation;
        private static bool? music;
        private static int? rent;
        internal static void Capture(StartingScenario scenario)
        {
            Reset();
            FreeStartEvents.Capture(scenario);
            FreeStartEquipment.Capture(scenario);
            FreeStartContacts.Capture(scenario);
            if (!ModEntry.IsEnabled || scenario == null || scenario.id != FreeStartScenario.Id) return;
            music = ExperienceSettings.FreeStartMusic;
            rent = ExperienceSettings.FreeStartRent;
            reputation = new int[5];
            for (int i = 0; i < reputation.Length; i++) reputation[i] = ExperienceSettings.FreeStartReputation(i);
        }
        internal static void Reset() { reputation = null; music = null; rent = null; FreeStartEvents.Reset(); FreeStartEquipment.Reset(); FreeStartContacts.Reset(); }
        private static bool IsCustom()
        {
            return ModEntry.IsEnabled && PlayerStore.IsInstanceExist() &&
                PlayerStore.Instance.startType == NewGameData.StartType.MODDED &&
                PlayerStore.Instance.startScenarioId == FreeStartScenario.Id;
        }
        internal static void ApplyReputation()
        {
            if (IsCustom())
            {
                FreeStartEvents.SaveChoice();
                if (!ModHelper.GetModDataBool(ModEntry.ModId, "free_start_rent_applied", false))
                {
                    PlayerStore.Instance.startingRent = rent ?? ExperienceSettings.FreeStartRent;
                    PlayerStore.Instance.rentValue = PlayerStore.Instance.startingRent;
                    ModHelper.SetModDataBool(ModEntry.ModId, "free_start_rent_applied", true);
                }
            }
            if (!IsCustom() || ModHelper.GetModDataBool(ModEntry.ModId, "free_start_reputation_applied", false)) return;
            string[] factions = { "FACTION_LOWER_LEVEL", "FACTION_UPPER_LEVEL", "FACTION_SECURITY", "FACTION_BLACK_MARKET", "FACTION_REVOLUTION" };
            for (int i = 0; i < factions.Length; i++)
            {
                int value = reputation == null ? ExperienceSettings.FreeStartReputation(i) : reputation[i];
                if (value != 0) StoreReputation.ModReputation(factions[i], value);
            }
            ModHelper.SetModDataBool(ModEntry.ModId, "free_start_reputation_applied", true);
        }
        internal static bool SkipMusic()
        {
            return StartingPerk.IsTapehead() || (IsCustom() && !(music ?? ExperienceSettings.FreeStartMusic));
        }
        // Replace only the native music prerequisite inside initial item generation;
        // leave the mentor contract and all other starting items untouched.
        [HarmonyPatch(typeof(NewGameData), "HandleInitialItem")]
        internal static class MusicPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var original = AccessTools.Method(typeof(StartingPerk), "IsTapehead");
                var replacement = AccessTools.Method(typeof(FreeStartExtras), "SkipMusic");
                int matches = 0;
                foreach (var instruction in instructions)
                {
                    if (instruction.Calls(original)) { instruction.operand = replacement; matches++; }
                    yield return instruction;
                }
                if (matches != 1) throw new InvalidOperationException("Starting music prerequisite no longer matches supported game version.");
            }
        }
    }
}
