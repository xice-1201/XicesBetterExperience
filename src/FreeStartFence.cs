using HarmonyLib;

namespace XicesBetterExperience
{
    internal static class FreeStartFence
    {
        internal static void GiveItems(bool storage, bool contraband, bool lamps)
        {
            var inventory = EmporiumEntry.Instance.invElement;
            if (storage && !ModHelper.GetModDataBool(ModEntry.ModId, "free_start_smuggler_storage_granted", false))
            {
                GraphUtils.EmporiumTryAdd(inventory, ItemFeatureHelper.SetShabby(DirectoryMaster.Item("smuggler_bay")));
                ModHelper.SetModDataBool(ModEntry.ModId, "free_start_smuggler_storage_granted", true);
            }
            if (contraband && !ModHelper.GetModDataBool(ModEntry.ModId, "free_start_contraband_granted", false))
            {
                foreach (string id in new[] { "dream_dust", "dream_dust", "heavy_pistol_ammo", "heavy_pistol_ammo", "med_keycard", "eng_keycard", "sec_keycard" })
                    GraphUtils.EmporiumTryAdd(inventory, DirectoryMaster.Item(id));
                GraphUtils.EmporiumTryAdd(inventory, PreBuiltItemHelper.CreateHeavyHandmadeGun(ItemFeatureList.EquipmentCondition.Fair));
                ModHelper.SetModDataBool(ModEntry.ModId, "free_start_contraband_granted", true);
            }
            if (lamps && !ModHelper.GetModDataBool(ModEntry.ModId, "free_start_black_lamps_granted", false))
            {
                for (int i = 0; i < 2; i++)
                    GraphUtils.EmporiumTryAdd(inventory, ItemFeatureHelper.SetShabby(DirectoryMaster.Item("black_lamp")));
                ModHelper.SetModDataBool(ModEntry.ModId, "free_start_black_lamps_granted", true);
            }
        }

        [HarmonyPatch(typeof(StoreClientManager), "GenerateClient")]
        internal static class FirstCustomer
        {
            static void Postfix(StoreClientManager __instance)
            {
                if (!ModEntry.IsEnabled || !PlayerStore.IsInstanceExist() || StoreStation.Instance == null ||
                    StoreStation.Instance.dayCounter != 1 || PlayerStore.Instance.startType != NewGameData.StartType.MODDED ||
                    PlayerStore.Instance.startScenarioId != FreeStartScenario.Id ||
                    PlayerStore.Instance.storeClientData.isSawyerDead ||
                    !ModHelper.GetModDataBool(ModEntry.ModId, "free_start_exchange", false) ||
                    ModHelper.GetModDataBool(ModEntry.ModId, "free_start_exchange_intro_queued", false)) return;
                // Reuse the complete native fence conversation (including its trade
                // directive). Move it ahead of the tutorial regardless of intro mode.
                __instance.clientStack.RemoveAll(c => c != null &&
                    (c.identifier == "janitorUEIntroductionFence" || c.identifier == "janitorUEIntroduction"));
                __instance.AddNextClient(StoreClientListStory.CreateJanitorUndergroundExchangeIntroductionFence());
                ModHelper.SetModDataBool(ModEntry.ModId, "free_start_exchange_intro_queued", true);
            }
        }
    }
}
