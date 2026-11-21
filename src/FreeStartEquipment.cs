using System;
namespace XicesBetterExperience
{
    internal static class FreeStartEquipment
    {
        private static bool[] selected;
        private static bool Choice(int index)
        {
            switch(index) {
                case 0: return ExperienceSettings.FreeStartHydroponics;
                case 1: return ExperienceSettings.FreeStartRanching;
                case 2: return ExperienceSettings.FreeStartAquaculture;
                default: return ExperienceSettings.FreeStartChemistry;
            }
        }
        internal static void Capture(StartingScenario scenario)
        {
            selected = null;
            if (!ModEntry.IsEnabled || scenario == null || scenario.id != FreeStartScenario.Id) return;
            selected = new bool[4];
            for (int i = 0; i < 4; i++) selected[i] = Choice(i);
        }
        internal static void Reset() { selected = null; }
        internal static void Give()
        {
            Action[] grants = { Hydroponics, Ranching, Aquaculture, Chemistry };
            for (int i = 0; i < grants.Length; i++) {
                string key = "free_start_equipment_" + i + "_granted";
                if (!(selected == null ? Choice(i) : selected[i]) || ModHelper.GetModDataBool(ModEntry.ModId, key, false)) continue;
                grants[i]();
                ModHelper.SetModDataBool(ModEntry.ModId, key, true);
            }
        }
        private static void Hydroponics()
        {
            var invElement = EmporiumEntry.Instance.invElement;

            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, DirectoryMaster.Item("hydroponic_guide"));
            GraphUtils.EmporiumTryAdd(invElement, ItemFeatureHelper.SetShabby(PreBuiltItemHelper.CreateOldHydroponic1()));
            GameItem gameItem6 = ItemFeatureHelper.SetShabby(PreBuiltItemHelper.CreateHydroponic());
            GraphUtils.TryAcceptAll(gameItem6, PreBuiltItemHelper.EnergyCredit(100));
            gameItem6.name = "Decrepit Hydroponic System";
            GraphUtils.EmporiumTryAdd(invElement, gameItem6);
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, DirectoryMaster.Item("water_ration_small"));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, DirectoryMaster.Item("water_ration_small"));
            GameItem gameItem7 = DirectoryMaster.Item("large_bottled_water");
            WaterHelper.FillWithGhostwater(gameItem7);
            GraphUtils.TryAcceptAll(gameItem6, gameItem7);
            GameItem gameItem8 = DirectoryMaster.Item("system_module_eco");
            GraphUtils.TryAcceptAll(gameItem6, gameItem8);
            ModuleHelper.SetModuleStuck(gameItem8);
            GameItem gameItem9 = DirectoryMaster.Item("system_module_eco");
            GraphUtils.TryAcceptAll(gameItem6, gameItem9);
            ModuleHelper.SetModuleStuck(gameItem9);
            GameItem gameItem10 = DirectoryMaster.Item("system_module_ruined");
            GraphUtils.TryAcceptAll(gameItem6, gameItem10);
            ModuleHelper.SetModuleStuck(gameItem10);
            GameItem gameItem11 = DirectoryMaster.Item("system_module_ruined");
            GraphUtils.TryAcceptAll(gameItem6, gameItem11);
            ModuleHelper.SetModuleStuck(gameItem11);
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("nutrifruit_seed"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("nutrifruit_seed"));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.FullNutrientTabletBox());
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, DirectoryMaster.Item("fillerweed_seed"));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, DirectoryMaster.Item("fillerweed_seed"));

        }

        private static void Ranching()
        {
            var invElement = EmporiumEntry.Instance.invElement;

            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, AnimalCage.SpawnCageWithDispenser(shabby: true));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateRat("MALE", 5, defaultStats: true));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateRat("FEMALE", 5, defaultStats: true));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateRat("MALE", 3, defaultStats: true));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateRat("FEMALE", 3, defaultStats: true));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateRat("MALE", 1, defaultStats: true));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateRat("FEMALE", 1, defaultStats: true));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, ItemSpawner.Spawn("kitchen_cleaver"));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, ItemSpawner.Spawn("labeler_blue"));
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, ItemSpawner.Spawn("marking_pen"));
            GameItem item4 = DirectoryMaster.Item("serum_green");
            UseCountHelper.SetCurrentUseCount(item4, 2);
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, item4);
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateNutrifruitSmall());
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateNutrifruitSmall());
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateNutrifruitSmall());
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateNutrifruitSmall());
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateNutrifruitSmall());
            GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement, PreBuiltItemHelper.CreateNutrifruitSmall());

        }

        private static void Aquaculture()
        {
            var invElement = EmporiumEntry.Instance.invElement;

            for (int j = 0; j < 2; j++)
            {
                GameItem gameItem12 = ItemFeatureHelper.SetShabby(ItemSpawner.Spawn("ras"));
                GraphUtils.TryAcceptAll(gameItem12, ItemSpawner.Spawn("ras_water_tank"));
                GraphUtils.TryAcceptAll(gameItem12, PreBuiltItemHelper.EnergyCredit(50));
                GraphUtils.EmporiumTryAdd(invElement, gameItem12);
            }
            int age = FishSpecies.Tilapia.stageStartDays[4];
            GraphUtils.EmporiumTryAdd(invElement, AquacultureItems.SpawnFish(FishSpecies.Tilapia, "MALE", age));
            GraphUtils.EmporiumTryAdd(invElement, AquacultureItems.SpawnFish(FishSpecies.Tilapia, "FEMALE", age));
            GraphUtils.EmporiumTryAdd(invElement, AquacultureItems.SpawnFertilizedRoe(FishSpecies.Tilapia));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("industrial_fish_feed"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("industrial_fish_feed"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("scrap_metal"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("common_electronic"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("kitchen_knife"));
            GraphUtils.EmporiumTryAdd(invElement, ItemSpawner.Spawn("marking_pen"));
            for (int k = 0; k < 2; k++)
            {
                GameItem gameItem13 = DirectoryMaster.Item("water_jug");
                WaterHelper.FillWithGhostwater(gameItem13);
                GraphUtils.EmporiumTryAdd(invElement, gameItem13);
            }

        }

        private static void Chemistry()
        {
            var invElement = EmporiumEntry.Instance.invElement;

            GraphUtils.EmporiumTryAdd(invElement, ItemFeatureHelper.SetShabby(DirectoryMaster.Item("blender")));
            GraphUtils.EmporiumTryAdd(invElement, ItemFeatureHelper.SetShabby(DirectoryMaster.Item("heating_plate")));
            GraphUtils.EmporiumTryAdd(invElement, ItemFeatureHelper.SetShabby(PreBuiltItemHelper.CreateEvaporator(40)));
            GraphUtils.EmporiumTryAdd(invElement, ItemFeatureHelper.SetShabby(DirectoryMaster.Item("chem_finisher")));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("1ml_dropper"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("5ml_dropper"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("25ml_cylinder"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("50ml_cylinder"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("100ml_cylinder"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("bottled_water"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("bottled_water"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("coffee_bean"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("water_pitcher"));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("kotton_fabric"));
            GraphUtils.EmporiumTryAdd(invElement, ItemFeatureHelper.SetShabby(DirectoryMaster.Item("chem_scanner")));
            GraphUtils.EmporiumTryAdd(invElement, ItemFeatureHelper.SetShabby(DirectoryMaster.Item("smuggler_bay_mini")));
            GraphUtils.EmporiumTryAdd(invElement, DirectoryMaster.Item("chemistry_guide"));

        }
    }
}

