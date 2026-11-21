using HarmonyLib;

namespace XicesBetterExperience
{
    internal static class FreeStartContacts
    {
        private static bool[] selected;
        private static bool Choice(int index)
        {
            switch (index)
            {
                case 0: return ExperienceSettings.FreeStartFarmerContact;
                case 1: return ExperienceSettings.FreeStartRancherContact;
                case 2: return ExperienceSettings.FreeStartAquafarmerContact;
                case 3: return ExperienceSettings.FreeStartChemistContact;
                case 4: return ExperienceSettings.FreeStartGunsmithContact;
                default: return ExperienceSettings.FreeStartGunPermit;
            }
        }
        internal static void Capture(StartingScenario scenario)
        {
            selected = null;
            if (!ModEntry.IsEnabled || scenario == null || scenario.id != FreeStartScenario.Id) return;
            selected = new bool[6];
            for (int i = 0; i < selected.Length; i++) selected[i] = Choice(i);
        }
        internal static void Reset() { selected = null; }
        internal static void Give()
        {
            string[] upgrades = { "RETIRED_FARMER", "RETIRED_RANCHER", "AQUACULTURE", "RETIRED_CHEMIST", "RETIRED_GUNSMITH", "GUN_PERMIT_I" };
            for (int i = 0; i < upgrades.Length; i++)
            {
                string key = "free_start_contact_" + upgrades[i];
                if (!(selected == null ? Choice(i) : selected[i]) || ModHelper.GetModDataBool(ModEntry.ModId, key, false)) continue;
                PlayerStore.Instance.networkUpgrade[upgrades[i]].state = 2;
                if (i == 5)
                {
                    var permit = DirectoryMaster.Item("permit_gun_1");
                    GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.showcaseElement, permit);
                    PermitHelper.GrantedFromPermit(permit);
                }
                ModHelper.SetModDataBool(ModEntry.ModId, key, true);
            }
        }

        // Weekly visitors already use the native upgrade states above. The three
        // early gun visitors are normally restricted to the gunsmith profession.
        [HarmonyPatch(typeof(StoreClientManager), "HandleSkipClient")]
        internal static class EarlyGunClients
        {
            static void Postfix(StoreClientManager __instance)
            {
                if (!FreeStartEvents.IsCustom() || StoreStation.Instance == null ||
                    !ModHelper.GetModDataBool(ModEntry.ModId, "free_start_contact_GUN_PERMIT_I", false)) return;
                int day = StoreStation.Instance.dayCounter;
                if (day != 1 && day != 2 && day != 4) return;
                string key = "free_start_gun_visit_day_" + day;
                if (ModHelper.GetModDataBool(ModEntry.ModId, key, false)) return;
                __instance.AddClient(day == 2 ? StoreClientListGun.CreateLLGunSeller() : StoreClientListGun.CreateLLGunBuyer());
                ModHelper.SetModDataBool(ModEntry.ModId, key, true);
            }
        }
    }
}
