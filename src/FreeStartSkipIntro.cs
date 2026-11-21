using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace XicesBetterExperience
{
    // Open can reuse an existing difficulty window, so refresh the native lock before
    // either its cached-window path or its normal initialization refreshes the checkbox.
    [HarmonyPatch(typeof(NewGameDifficultyUI), "Open")]
    internal static class FreeStartDifficultySkip
    {
        static void Prefix(ref bool ___skipIntroForced)
        {
            StartingScenario scenario = StartingScenario.Get(StartingScenario.ResolveId(
                NewGameData.Instance.startType, NewGameData.Instance.startScenarioId));
            ___skipIntroForced = scenario != null && scenario.skipIntro;
            if (___skipIntroForced) NewGameData.Instance.skipIntro = true;
        }
    }

    [HarmonyPatch(typeof(PlayerStore), "HandleSkipIntro")]
    internal static class FreeStartSkipSupplies
    {
        // Keep all native tutorial/service/contact unlocks and starting events. Jump
        // over only the contiguous item-creation block, before any items are created.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var code = new List<CodeInstruction>(instructions);
            int begin = code.FindIndex(c => c.opcode.Equals(OpCodes.Ldstr) && Equals(c.operand, "tutorial_book"));
            int lastItem = code.FindIndex(c => c.opcode.Equals(OpCodes.Ldstr) && Equals(c.operand, "intel_sec"));
            int end = lastItem < 0 ? -1 : code.FindIndex(lastItem, c => c.Calls(AccessTools.PropertyGetter(typeof(StoreStation), "Instance")));
            if (begin < 0 || lastItem <= begin || end <= lastItem)
                throw new InvalidOperationException("Skip-intro supplies block no longer matches the supported game version.");
            Label target = generator.DefineLabel();
            code[end].labels.Add(target);
            var check = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FreeStartScenario), "SuppressSkipSupplies"));
            check.labels.AddRange(code[begin].labels);
            code[begin].labels.Clear();
            code.InsertRange(begin, new[] { check, new CodeInstruction(OpCodes.Brtrue, target) });
            return code;
        }
    }
}
