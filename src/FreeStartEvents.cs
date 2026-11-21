using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace XicesBetterExperience
{
    internal static class FreeStartEvents
    {
        private static int? selectedLimit;
        internal static void Capture(StartingScenario scenario)
        {
            selectedLimit = ModEntry.IsEnabled && scenario != null && scenario.id == FreeStartScenario.Id
                ? (int?)ExperienceSettings.FreeStartBailouts : null;
        }
        internal static void Reset() { selectedLimit = null; }
        internal static bool IsCustom()
        {
            return ModEntry.IsEnabled && PlayerStore.IsInstanceExist() &&
                PlayerStore.Instance.startType == NewGameData.StartType.MODDED &&
                PlayerStore.Instance.startScenarioId == FreeStartScenario.Id;
        }
        internal static void SaveChoice()
        {
            if (ModHelper.GetModDataBool(ModEntry.ModId, "free_start_events_saved", false)) return;
            ModHelper.SetModDataInt(ModEntry.ModId, "free_start_bailout_limit", selectedLimit ?? ExperienceSettings.FreeStartBailouts);
            ModHelper.SetModDataBool(ModEntry.ModId, "free_start_events_saved", true);
        }
        private static int Limit() { return ModHelper.GetModDataInt(ModEntry.ModId, "free_start_bailout_limit", 0); }

        [HarmonyPatch(typeof(StoreClientManager), "HandleWildBailout")]
        internal static class Bailout
        {
            static bool Prefix(StoreClientManager __instance, out bool __state)
            {
                __state = false;
                if (!IsCustom()) return true;
                int count = ModHelper.GetModDataInt(ModEntry.ModId, "free_start_bailout_count", 0);
                int lastDay = ModHelper.GetModDataInt(ModEntry.ModId, "free_start_bailout_day", -10);
                if (count >= Limit() || (count > 0 && StoreStation.Instance.dayCounter - lastDay < 10)) return false;
                __state = true;
                __instance.wildBailoutGiven = false;
                return true;
            }
            static void Postfix(StoreClientManager __instance, bool __state)
            {
                if (!__state || !__instance.wildBailoutGiven) return;
                ModHelper.SetModDataInt(ModEntry.ModId, "free_start_bailout_count",
                    ModHelper.GetModDataInt(ModEntry.ModId, "free_start_bailout_count", 0) + 1);
                ModHelper.SetModDataInt(ModEntry.ModId, "free_start_bailout_day", StoreStation.Instance.dayCounter);
            }
        }

        internal static bool Reminders(DifficultyRules rules)
        {
            return IsCustom() ? Limit() > 0 : rules.wildReminders;
        }
        [HarmonyPatch(typeof(StoreClientManager), "HandleContentUnlockClient")]
        internal static class ReminderPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var field = AccessTools.Field(typeof(DifficultyRules), "wildReminders");
                foreach (var instruction in instructions)
                {
                    if (instruction.opcode.Equals(OpCodes.Ldfld) && Equals(instruction.operand, field))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(FreeStartEvents), "Reminders");
                    }
                    yield return instruction;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerStore), "ShowDisplayCaseTimingTipOnce")]
    internal static class DisableDisplayCaseTimingTip
    {
        static bool Prefix() { return !ModEntry.IsEnabled || !ExperienceSettings.DisableDisplayCaseTip; }
    }
}
