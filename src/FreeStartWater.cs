using HarmonyLib;

namespace XicesBetterExperience
{
    internal static class FreeStartWater
    {
        // Match the native water-merchant equipment, without its cash deduction or
        // network unlock: those are independent choices in the custom start.
        internal static void GiveTools()
        {
            var inventory = EmporiumEntry.Instance.invElement;
            GraphUtils.EmporiumTryAdd(inventory, DirectoryMaster.Item("water_guide"));
            GraphUtils.EmporiumTryAdd(inventory, PreBuiltItemHelper.FullWaterTestStripBox());
            GraphUtils.EmporiumTryAdd(inventory, PreBuiltItemHelper.FullWaterPurificationTabletBox());
            var cleanWater = DirectoryMaster.Item("large_bottled_water");
            WaterHelper.FillWithGhostwater(cleanWater);
            GraphUtils.EmporiumTryAdd(inventory, cleanWater);
            var rustyWater = DirectoryMaster.Item("water_jug");
            WaterHelper.FillWithRustWaterLowEnd(rustyWater);
            GraphUtils.EmporiumTryAdd(inventory, rustyWater);
            foreach (string id in new[] { "large_bottled_water", "bottled_water", "small_bottled_water", "water_pitcher" })
                GraphUtils.EmporiumTryAdd(inventory, DirectoryMaster.Item(id));
            GraphUtils.EmporiumTryAdd(inventory, PreBuiltItemHelper.WaterFilter());
            if (PlayerStore.Instance.skipIntro && !FreeStartScenario.SuppressSkipSupplies())
                GraphUtils.EmporiumTryAdd(inventory, DirectoryMaster.Item("sign_water"));
            var purifier = ItemFeatureHelper.SetShabby(DirectoryMaster.Item("portable_water_purifier"));
            DurabilityHelper.SetCurrentDurability(purifier, 75);
            GraphUtils.EmporiumTryAdd(inventory, purifier);
            var printer = ItemFeatureHelper.SetShabby(DirectoryMaster.Item("bottle_printer"));
            GraphUtils.TryAcceptAll(printer, PreBuiltItemHelper.EnergyCredit(25));
            GraphUtils.EmporiumTryAdd(inventory, printer);
        }

        [HarmonyPatch(typeof(BarterOfferList), "CreateBarterFoodTutorial")]
        internal static class FoodTutorial
        {
            static void Postfix(BarterOffer __result)
            {
                if (__result == null) return;
                var original = __result.onInstance;
                __result.onInstance = () =>
                {
                    if (!ModEntry.IsEnabled || !PlayerStore.IsInstanceExist() ||
                        PlayerStore.Instance.startType != NewGameData.StartType.MODDED ||
                        PlayerStore.Instance.startScenarioId != FreeStartScenario.Id ||
                        !ModHelper.GetModDataBool(ModEntry.ModId, "free_start_water_contact", false))
                    {
                        if (original != null) original();
                        return;
                    }
                    TutorialUIManager.Instance.OnBarterTutorialStart();
                    PlayerStore.Instance.AddDirectSellingItemToTable(PreBuiltItemHelper.FullWaterPurificationTabletBox());
                };
            }
        }
    }
}
