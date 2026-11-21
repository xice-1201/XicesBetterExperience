using System;

namespace XicesBetterExperience
{
    public static class ExperienceSettings
    {
        private static ConfigEntry<string> freeStartSmugglerStorage;
        internal const string FreeStartSmugglerStorageKey = "internal_free_start_smuggler_storage_v1";
        internal static bool FreeStartSmugglerStorage { get { return freeStartSmugglerStorage != null && freeStartSmugglerStorage.Value == "true"; } }
        internal static void SaveFreeStartSmugglerStorage(bool value) { if (freeStartSmugglerStorage != null) freeStartSmugglerStorage.Set(value ? "true" : "false" ); }
        private static ConfigEntry<string> freeStartContraband;
        internal const string FreeStartContrabandKey = "internal_free_start_contraband_v1";
        internal static bool FreeStartContraband { get { return freeStartContraband != null && freeStartContraband.Value == "true"; } }
        internal static void SaveFreeStartContraband(bool value) { if (freeStartContraband != null) freeStartContraband.Set(value ? "true" : "false" ); }
        private static ConfigEntry<string> freeStartBlackLamps;
        internal const string FreeStartBlackLampsKey = "internal_free_start_black_lamps_v1";
        internal static bool FreeStartBlackLamps { get { return freeStartBlackLamps != null && freeStartBlackLamps.Value == "true"; } }
        internal static void SaveFreeStartBlackLamps(bool value) { if (freeStartBlackLamps != null) freeStartBlackLamps.Set(value ? "true" : "false" ); }
        private static ConfigEntry<string> freeStartExchange;
        internal const string FreeStartExchangeKey = "internal_free_start_exchange_v1";
        internal static bool FreeStartExchange { get { return freeStartExchange != null && freeStartExchange.Value == "true"; } }
        internal static void SaveFreeStartExchange(bool value) { if (freeStartExchange != null) freeStartExchange.Set(value ? "true" : "false" ); }
        private static ConfigEntry<string> freeStartMusic;
        internal const string FreeStartMusicKey = "internal_free_start_music_v1";
        internal static bool FreeStartMusic { get { return freeStartMusic != null && freeStartMusic.Value == "true"; } }
        internal static void SaveFreeStartMusic(bool value) { if (freeStartMusic != null) freeStartMusic.Set(value ? "true" : "false"); }
        private static readonly ConfigEntry<string>[] freeStartReputation = new ConfigEntry<string>[5];
        internal static string FreeStartReputationKey(int index) { return "internal_free_start_reputation_" + index + "_v1"; }
        internal static int FreeStartReputation(int index)
        {
            int value;
            return int.TryParse(freeStartReputation[index] == null ? "0" : freeStartReputation[index].Value, out value) ? System.Math.Max(-200, System.Math.Min(200, value)) : 0;
        }
        internal static void SaveFreeStartReputation(int index, int value)
        {
            if (freeStartReputation[index] != null) freeStartReputation[index].Set(System.Math.Max(-200, System.Math.Min(200, value)).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        private static ConfigEntry<bool> disableDisplayCaseTip;
        public static bool DisableDisplayCaseTip { get { return disableDisplayCaseTip == null || disableDisplayCaseTip.Value; } }
        private static ConfigEntry<string> freeStartBailouts;
        internal const string FreeStartBailoutsKey = "internal_free_start_bailouts_v1";
        private static ConfigEntry<string> freeStartRent;
        private static ConfigEntry<string> freeStartRentGrowth, freeStartRetailMarkup, freeStartLandlordGrace;
        private static ConfigEntry<string> freeStartPowerMultiplier, freeStartInsuranceMultiplier;
        private static ConfigEntry<string> freeStartVendorPermitFee, freeStartAppraisalJoinMultiplier;
        private static ConfigEntry<string> freeStartRunnerMarkup, freeStartHypertechMarkup, freeStartDoctorMarkup, freeStartRetiredMarkup;
        internal const string FreeStartRunnerMarkupKey = "internal_free_start_runner_markup_v1";
        internal const string FreeStartHypertechMarkupKey = "internal_free_start_hypertech_markup_v1";
        internal const string FreeStartDoctorMarkupKey = "internal_free_start_doctor_markup_v1";
        internal const string FreeStartRetiredMarkupKey = "internal_free_start_retired_markup_v1";
        internal static int FreeStartRunnerMarkup { get { return FreeStartRules.ParseRange(freeStartRunnerMarkup == null ? "15" : freeStartRunnerMarkup.Value, 15, 0, 1000); } }
        internal static int FreeStartHypertechMarkup { get { return FreeStartRules.ParseRange(freeStartHypertechMarkup == null ? "50" : freeStartHypertechMarkup.Value, 50, 0, 1000); } }
        internal static int FreeStartDoctorMarkup { get { return FreeStartRules.ParseRange(freeStartDoctorMarkup == null ? "15" : freeStartDoctorMarkup.Value, 15, 0, 1000); } }
        internal static int FreeStartRetiredMarkup { get { return FreeStartRules.ParseRange(freeStartRetiredMarkup == null ? "15" : freeStartRetiredMarkup.Value, 15, 0, 1000); } }
        internal static void SaveFreeStartRunnerMarkup(int value) { if (freeStartRunnerMarkup != null) freeStartRunnerMarkup.Set(Math.Max(0, Math.Min(1000, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal static void SaveFreeStartHypertechMarkup(int value) { if (freeStartHypertechMarkup != null) freeStartHypertechMarkup.Set(Math.Max(0, Math.Min(1000, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal static void SaveFreeStartDoctorMarkup(int value) { if (freeStartDoctorMarkup != null) freeStartDoctorMarkup.Set(Math.Max(0, Math.Min(1000, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal static void SaveFreeStartRetiredMarkup(int value) { if (freeStartRetiredMarkup != null) freeStartRetiredMarkup.Set(Math.Max(0, Math.Min(1000, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal const string FreeStartVendorPermitFeeKey = "internal_free_start_vendor_permit_fee_v1";
        internal const string FreeStartAppraisalJoinMultiplierKey = "internal_free_start_appraisal_join_multiplier_v1";
        internal static int FreeStartVendorPermitFee { get { return FreeStartRules.ParseRange(freeStartVendorPermitFee == null ? "1000" : freeStartVendorPermitFee.Value, 1000, 0, 10000); } }
        internal static int FreeStartAppraisalJoinMultiplier { get { return FreeStartRules.ParseRange(freeStartAppraisalJoinMultiplier == null ? "1" : freeStartAppraisalJoinMultiplier.Value, 1, 0, 10); } }
        internal static void SaveFreeStartVendorPermitFee(int value) { if (freeStartVendorPermitFee != null) freeStartVendorPermitFee.Set(Math.Max(0, Math.Min(10000, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal static void SaveFreeStartAppraisalJoinMultiplier(int value) { if (freeStartAppraisalJoinMultiplier != null) freeStartAppraisalJoinMultiplier.Set(Math.Max(0, Math.Min(10, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal const string FreeStartPowerMultiplierKey = "internal_free_start_power_multiplier_v1";
        internal const string FreeStartInsuranceMultiplierKey = "internal_free_start_insurance_multiplier_v1";
        internal static int FreeStartPowerMultiplier { get { return FreeStartRules.ParseRange(freeStartPowerMultiplier == null ? "1" : freeStartPowerMultiplier.Value, 1, 0, 10); } }
        internal static int FreeStartInsuranceMultiplier { get { return FreeStartRules.ParseRange(freeStartInsuranceMultiplier == null ? "1" : freeStartInsuranceMultiplier.Value, 1, 0, 10); } }
        internal static void SaveFreeStartPowerMultiplier(int value) { if (freeStartPowerMultiplier != null) freeStartPowerMultiplier.Set(Math.Max(0, Math.Min(10, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal static void SaveFreeStartInsuranceMultiplier(int value) { if (freeStartInsuranceMultiplier != null) freeStartInsuranceMultiplier.Set(Math.Max(0, Math.Min(10, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal const string FreeStartRentGrowthKey = "internal_free_start_rent_growth_v1";
        internal const string FreeStartRetailMarkupKey = "internal_free_start_retail_markup_v1";
        internal const string FreeStartLandlordGraceKey = "internal_free_start_landlord_grace_v1";
        internal static int FreeStartRentGrowth { get { return FreeStartRules.ParseRange(freeStartRentGrowth == null ? "5" : freeStartRentGrowth.Value, 5, 0, 100); } }
        internal static int FreeStartRetailMarkup { get { return FreeStartRules.ParseRange(freeStartRetailMarkup == null ? "15" : freeStartRetailMarkup.Value, 15, 0, 100); } }
        internal static bool FreeStartLandlordGrace { get { return freeStartLandlordGrace != null && freeStartLandlordGrace.Value == "true"; } }
        internal static void SaveFreeStartRentGrowth(int value) { if (freeStartRentGrowth != null) freeStartRentGrowth.Set(Math.Max(0, Math.Min(100, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal static void SaveFreeStartRetailMarkup(int value) { if (freeStartRetailMarkup != null) freeStartRetailMarkup.Set(Math.Max(0, Math.Min(100, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal static void SaveFreeStartLandlordGrace(bool value) { if (freeStartLandlordGrace != null) freeStartLandlordGrace.Set(value ? "true" : "false"); }
        internal const string FreeStartRentKey = "internal_free_start_rent_v1";
        internal static int FreeStartRent { get { return FreeStartRules.ParseRange(freeStartRent == null ? "200" : freeStartRent.Value, 200, 10, 2000); } }
        internal static void SaveFreeStartRent(int value) { if (freeStartRent != null) freeStartRent.Set(Math.Max(10, Math.Min(2000, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        internal static int FreeStartBailouts { get { int value; return int.TryParse(freeStartBailouts == null ? "0" : freeStartBailouts.Value, out value) ? Math.Max(0, Math.Min(10, value)) : 0; } }
        internal static void SaveFreeStartBailouts(int value) { if (freeStartBailouts != null) freeStartBailouts.Set(Math.Max(0, Math.Min(10, value)).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        private static ConfigEntry<string> freeStartHydroponics;
        internal const string FreeStartHydroponicsKey = "internal_free_start_hydroponics_v1";
        internal static bool FreeStartHydroponics { get { return freeStartHydroponics != null && freeStartHydroponics.Value == "true"; } }
        internal static void SaveFreeStartHydroponics(bool value) { if (freeStartHydroponics != null) freeStartHydroponics.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartRanching;
        internal const string FreeStartRanchingKey = "internal_free_start_ranching_v1";
        internal static bool FreeStartRanching { get { return freeStartRanching != null && freeStartRanching.Value == "true"; } }
        internal static void SaveFreeStartRanching(bool value) { if (freeStartRanching != null) freeStartRanching.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartAquaculture;
        internal const string FreeStartAquacultureKey = "internal_free_start_aquaculture_v1";
        internal static bool FreeStartAquaculture { get { return freeStartAquaculture != null && freeStartAquaculture.Value == "true"; } }
        internal static void SaveFreeStartAquaculture(bool value) { if (freeStartAquaculture != null) freeStartAquaculture.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartChemistry;
        internal const string FreeStartChemistryKey = "internal_free_start_chemistry_v1";
        internal static bool FreeStartChemistry { get { return freeStartChemistry != null && freeStartChemistry.Value == "true"; } }
        internal static void SaveFreeStartChemistry(bool value) { if (freeStartChemistry != null) freeStartChemistry.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartFarmerContact;
        internal const string FreeStartFarmerContactKey = "internal_free_start_farmercontact_v1";
        internal static bool FreeStartFarmerContact { get { return freeStartFarmerContact != null && freeStartFarmerContact.Value == "true"; } }
        internal static void SaveFreeStartFarmerContact(bool value) { if (freeStartFarmerContact != null) freeStartFarmerContact.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartRancherContact;
        internal const string FreeStartRancherContactKey = "internal_free_start_ranchercontact_v1";
        internal static bool FreeStartRancherContact { get { return freeStartRancherContact != null && freeStartRancherContact.Value == "true"; } }
        internal static void SaveFreeStartRancherContact(bool value) { if (freeStartRancherContact != null) freeStartRancherContact.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartAquafarmerContact;
        internal const string FreeStartAquafarmerContactKey = "internal_free_start_aquafarmercontact_v1";
        internal static bool FreeStartAquafarmerContact { get { return freeStartAquafarmerContact != null && freeStartAquafarmerContact.Value == "true"; } }
        internal static void SaveFreeStartAquafarmerContact(bool value) { if (freeStartAquafarmerContact != null) freeStartAquafarmerContact.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartChemistContact;
        internal const string FreeStartChemistContactKey = "internal_free_start_chemistcontact_v1";
        internal static bool FreeStartChemistContact { get { return freeStartChemistContact != null && freeStartChemistContact.Value == "true"; } }
        internal static void SaveFreeStartChemistContact(bool value) { if (freeStartChemistContact != null) freeStartChemistContact.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartGunsmithContact;
        internal const string FreeStartGunsmithContactKey = "internal_free_start_gunsmithcontact_v1";
        internal static bool FreeStartGunsmithContact { get { return freeStartGunsmithContact != null && freeStartGunsmithContact.Value == "true"; } }
        internal static void SaveFreeStartGunsmithContact(bool value) { if (freeStartGunsmithContact != null) freeStartGunsmithContact.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartGunPermit;
        internal const string FreeStartGunPermitKey = "internal_free_start_gunpermit_v1";
        internal static bool FreeStartGunPermit { get { return freeStartGunPermit != null && freeStartGunPermit.Value == "true"; } }
        internal static void SaveFreeStartGunPermit(bool value) { if (freeStartGunPermit != null) freeStartGunPermit.Set(value ? "true" : "false"); }
        private static ConfigEntry<bool> autoOpenAppraisalGuide;
        private static ConfigEntry<string> freeStartCash;
        internal const string FreeStartCashKey = "internal_free_start_cash_v1";
        private static ConfigEntry<string> freeStartStorage;
        internal const string FreeStartStorageKey = "internal_free_start_storage_v1";
        internal static bool FreeStartStorage { get { return freeStartStorage != null && freeStartStorage.Value == "true"; } }
        internal static void SaveFreeStartStorage(bool value) { if (freeStartStorage != null) freeStartStorage.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartWaterTools, freeStartWaterContact;
        internal const string FreeStartWaterToolsKey = "internal_free_start_water_tools_v1";
        internal const string FreeStartWaterContactKey = "internal_free_start_water_contact_v1";
        internal static bool FreeStartWaterTools { get { return freeStartWaterTools != null && freeStartWaterTools.Value == "true"; } }
        internal static bool FreeStartWaterContact { get { return freeStartWaterContact != null && freeStartWaterContact.Value == "true"; } }
        internal static void SaveFreeStartWaterTools(bool value) { if (freeStartWaterTools != null) freeStartWaterTools.Set(value ? "true" : "false"); }
        internal static void SaveFreeStartWaterContact(bool value) { if (freeStartWaterContact != null) freeStartWaterContact.Set(value ? "true" : "false"); }
        private static ConfigEntry<string> freeStartFood, freeStartHousehold, freeStartForceSkip, freeStartNoSkipSupplies;
        internal const string FreeStartForceSkipKey = "internal_free_start_force_skip_v1";
        internal const string FreeStartNoSkipSuppliesKey = "internal_free_start_no_skip_supplies_v1";
        internal static bool FreeStartForceSkip { get { return freeStartForceSkip != null && freeStartForceSkip.Value == "true"; } }
        internal static bool FreeStartNoSkipSupplies { get { return freeStartNoSkipSupplies != null && freeStartNoSkipSupplies.Value == "true"; } }
        internal static void SaveFreeStartForceSkip(bool value) { if (freeStartForceSkip != null) freeStartForceSkip.Set(value ? "true" : "false"); }
        internal static void SaveFreeStartNoSkipSupplies(bool value) { if (freeStartNoSkipSupplies != null) freeStartNoSkipSupplies.Set(value ? "true" : "false"); }
        internal const string FreeStartFoodKey = "internal_free_start_food_v1";
        internal const string FreeStartHouseholdKey = "internal_free_start_household_v1";
        internal static bool FreeStartFood { get { return freeStartFood != null && freeStartFood.Value == "true"; } }
        internal static bool FreeStartHousehold { get { return freeStartHousehold != null && freeStartHousehold.Value == "true"; } }
        internal static void SaveFreeStartFood(bool value) { if (freeStartFood != null) freeStartFood.Set(value ? "true" : "false"); }
        internal static void SaveFreeStartHousehold(bool value) { if (freeStartHousehold != null) freeStartHousehold.Set(value ? "true" : "false"); }
        internal static int FreeStartCash { get { return FreeStartRules.Parse(freeStartCash == null ? "600" : freeStartCash.Value, 600); } }
        internal static void SaveFreeStartCash(int value) { if (freeStartCash != null) freeStartCash.Set(FreeStartRules.Clamp(value).ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        private static ConfigEntry<bool> autoRenew;
        public static bool AutoRenew { get { return autoRenew == null || autoRenew.Value; } }
        private static ConfigEntry<bool> retainWindowLayout;
        private static ConfigEntry<string> tabletLayout;
        private static ConfigEntry<string> inspectionLayouts;
        internal const string InspectionLayoutsKey = "internal_inspection_layouts_v1";
        internal static string InspectionLayouts { get { return inspectionLayouts == null ? "" : inspectionLayouts.Value; } }
        internal static void SaveInspectionLayouts(string value) { if (inspectionLayouts != null) inspectionLayouts.Set(value); }
        private static ConfigEntry<string> phoneLayout;
        private static ConfigEntry<string> vendingFountainLayout;
        private static ConfigEntry<string> vendingMachineLayout;
        internal const string VendingMachineLayoutKey = "internal_vending_machine_layout_v1";
        internal static string VendingMachineLayout { get { return vendingMachineLayout == null ? "" : vendingMachineLayout.Value; } }
        internal static void SaveVendingMachineLayout(string value) { if (vendingMachineLayout != null) vendingMachineLayout.Set(value); }
        internal const string VendingFountainLayoutKey = "internal_vending_fountain_layout_v1";
        internal static string VendingFountainLayout { get { return vendingFountainLayout == null ? "" : vendingFountainLayout.Value; } }
        internal static void SaveVendingFountainLayout(string value) { if (vendingFountainLayout != null) vendingFountainLayout.Set(value); }
        private static ConfigEntry<string> waterScannerLayout;
        internal const string WaterScannerLayoutKey = "internal_water_scanner_layout_v1";
        internal static string WaterScannerLayout { get { return waterScannerLayout == null ? "" : waterScannerLayout.Value; } }
        internal static void SaveWaterScannerLayout(string value) { if (waterScannerLayout != null) waterScannerLayout.Set(value); }
        private static ConfigEntry<string> archiveLayout;
        private static ConfigEntry<string> trashLayout;
        internal const string ArchiveLayoutKey = "internal_archive_layout_v1";
        internal const string TrashLayoutKey = "internal_trash_layout_v1";
        internal static string ArchiveLayout { get { return archiveLayout == null ? "" : archiveLayout.Value; } }
        internal static string TrashLayout { get { return trashLayout == null ? "" : trashLayout.Value; } }
        internal static void SaveArchiveLayout(string value) { if (archiveLayout != null) archiveLayout.Set(value); }
        internal static void SaveTrashLayout(string value) { if (trashLayout != null) trashLayout.Set(value); }
        internal const string PhoneLayoutKey = "internal_phone_layout_v1";
        internal static string PhoneLayout { get { return phoneLayout == null ? "" : phoneLayout.Value; } }
        internal static void SavePhoneLayout(string value) { if (phoneLayout != null) phoneLayout.Set(value); }
        internal const string TabletLayoutKey = "internal_tablet_layout_v1";
        public static bool RetainWindowLayout { get { return retainWindowLayout == null || retainWindowLayout.Value; } }
        internal static string TabletLayout { get { return tabletLayout == null ? "" : tabletLayout.Value; } }
        internal static void SaveTabletLayout(string value) { if (tabletLayout != null) tabletLayout.Set(value); }
        internal static bool IsInternalSetting(string key) { return key == FreeStartRunnerMarkupKey || key == FreeStartHypertechMarkupKey || key == FreeStartDoctorMarkupKey || key == FreeStartRetiredMarkupKey || key == FreeStartVendorPermitFeeKey || key == FreeStartAppraisalJoinMultiplierKey || key == FreeStartPowerMultiplierKey || key == FreeStartInsuranceMultiplierKey || key == FreeStartRentGrowthKey || key == FreeStartRetailMarkupKey || key == FreeStartLandlordGraceKey || key == FreeStartFarmerContactKey || key == FreeStartRancherContactKey || key == FreeStartAquafarmerContactKey || key == FreeStartChemistContactKey || key == FreeStartGunsmithContactKey || key == FreeStartGunPermitKey || key == FreeStartHydroponicsKey || key == FreeStartRanchingKey || key == FreeStartAquacultureKey || key == FreeStartChemistryKey || key == FreeStartRentKey || key == FreeStartBailoutsKey || key == FreeStartMusicKey || key == FreeStartReputationKey(0) || key == FreeStartReputationKey(1) || key == FreeStartReputationKey(2) || key == FreeStartReputationKey(3) || key == FreeStartReputationKey(4) || key == FreeStartExchangeKey || key == FreeStartBlackLampsKey || key == FreeStartContrabandKey || key == FreeStartSmugglerStorageKey || key == FreeStartWaterToolsKey || key == FreeStartWaterContactKey || key == FreeStartForceSkipKey || key == FreeStartNoSkipSuppliesKey || key == FreeStartFoodKey || key == FreeStartHouseholdKey || key == FreeStartStorageKey || key == FreeStartCashKey || key == InspectionLayoutsKey || key == TabletLayoutKey || key == PhoneLayoutKey || key == ArchiveLayoutKey || key == TrashLayoutKey || key == WaterScannerLayoutKey || key == VendingFountainLayoutKey || key == VendingMachineLayoutKey; }
        private static ConfigEntry<bool> storageNaming;
        private static ConfigEntry<bool> storageCapacity;
        public static bool StorageCapacity { get { return storageCapacity == null || storageCapacity.Value; } }
        private static ConfigEntry<bool> retainDocumentPage;
        private static ConfigEntry<int> appraisalAutomation;
        private static ConfigEntry<int> ribwichAutomation;
        private static ConfigEntry<int> plasmaAutomation;
        private static ConfigEntry<int> cigaretteAutomation;
        public static int CigaretteAutomation { get { return cigaretteAutomation == null ? 0 : Math.Max(0, Math.Min(3, cigaretteAutomation.Value)); } }
        private static ConfigEntry<int> injectorAutomation;
        private static ConfigEntry<bool> augIdentification;
        public static bool AugIdentification { get { return augIdentification != null && augIdentification.Value; } }
        public static int InjectorAutomation { get { return injectorAutomation == null ? 0 : Math.Max(0, Math.Min(2, injectorAutomation.Value)); } }
        private static ConfigEntry<bool> stolenMarker;
        private static ConfigEntry<bool> contrabandMarker;
        private static ConfigEntry<bool> singleCellValue;
        private static ConfigEntry<bool> exchangeAutoPickup;
        private static ConfigEntry<bool> autoDiscardAppraisalReceipt;
        private static ConfigEntry<bool> nightServiceInformation;
        public static bool NightServiceInformation { get { return nightServiceInformation == null || nightServiceInformation.Value; } }
        private static ConfigEntry<bool> powerWarning;
        private static ConfigEntry<bool> overflowWarning;
        private static ConfigEntry<bool> marketChangeMarker;
        private static ConfigEntry<bool> moduleAffixMarker;
        public static bool ModuleAffixMarker { get { return moduleAffixMarker == null || moduleAffixMarker.Value; } }

        public static int AppraisalAutomationFor(GameItem item)
        {
            if (item == null) return 0;
            ConfigEntry<int> entry;
            string key;
            if (item.IsTag("HAND_TAG")) { entry = appraisalAutomation; key = "appraisal_automation"; }
            else if (item.IsTag("RIBWICH_TAG")) { entry = ribwichAutomation; key = "appraisal_automation_ribwich_v2"; }
            else if (item.IsTag("PLASMA_CELL_TAG")) { entry = plasmaAutomation; key = "appraisal_automation_plasma_v2"; }
            else return 0;
            return AutomationLevel(key, entry == null ? 0 : entry.Value);
        }

        internal static int AutomationMax(string key)
        {
            return key == "cigarette_inspection_automation" ? 3 : key == "injector_inspection_automation" ? 2 : key == "appraisal_automation_ribwich_v2" ? 3 : key == "appraisal_automation_plasma_v2" ? 2 : 4;
        }

        internal static int AutomationLevel(string key, int index)
        {
            index = Math.Max(0, Math.Min(AutomationMax(key), index));
            return index == 0 ? 0 : index + (4 - AutomationMax(key));
        }

        internal static bool IsAutomationSetting(string key)
        {
            return key == "cigarette_inspection_automation" || key == "injector_inspection_automation" || key == "appraisal_automation" || key == "appraisal_automation_ribwich_v2" || key == "appraisal_automation_plasma_v2";
        }

        internal static string SettingsTab(string key)
        {
            if (key == "aug_identification" || IsAutomationSetting(key)) return "automation";
            if (key == "overflow_warning") return "icons";
            if (key == "storage_naming" || key == "storage_capacity" || key == "stolen_marker" || key == "contraband_marker" ||
                key == "power_warning" || key == "market_change_marker" || key == "module_affix_marker") return "icons";
            return "general";
        }

        public static bool RetainDocumentPage
        {
            get { return retainDocumentPage == null || retainDocumentPage.Value; }
        }

        public static bool StorageNaming
        {
            get { return storageNaming == null || storageNaming.Value; }
        }

        public static bool AutoOpenAppraisalGuide
        {
            get { return autoOpenAppraisalGuide == null || autoOpenAppraisalGuide.Value; }
        }

        public static bool StolenMarker
        {
            get { return stolenMarker == null || stolenMarker.Value; }
        }

        public static bool ContrabandMarker
        {
            get { return contrabandMarker == null || contrabandMarker.Value; }
        }

        public static bool SingleCellValue
        {
            get { return singleCellValue == null || singleCellValue.Value; }
        }

        public static bool ExchangeAutoPickup
        {
            get { return exchangeAutoPickup == null || exchangeAutoPickup.Value; }
        }

        public static bool AutoDiscardAppraisalReceipt
        {
            get { return autoDiscardAppraisalReceipt == null || autoDiscardAppraisalReceipt.Value; }
        }

        public static bool PowerWarning
        {
            get { return powerWarning == null || powerWarning.Value; }
        }

        public static bool MarketChangeMarker
        {
            get { return marketChangeMarker == null || marketChangeMarker.Value; }
        }

        public static bool OverflowWarning { get { return overflowWarning == null || overflowWarning.Value; } }

        public static void Register()
        {
            if (autoOpenAppraisalGuide != null) return;
            disableDisplayCaseTip = ModConfig.For(ModEntry.ModId).Define("disable_display_case_tip", true, "", Text("setting_disable_display_case_tip"), Text("setting_disable_display_case_tip_tip"));
            autoOpenAppraisalGuide = ModConfig.For(ModEntry.ModId).Define(
                "auto_open_appraisal_guide", true, "", Text("setting_auto_open_guide"),
                Text("setting_auto_open_guide_tip"));
            autoRenew = ModConfig.For(ModEntry.ModId).Define(
                "auto_renew", true, "", Text("setting_auto_renew"), Text("setting_auto_renew_tip"));
            retainWindowLayout = ModConfig.For(ModEntry.ModId).Define(
                "retain_window_layout", true, "", Text("setting_retain_window_layout"), Text("setting_retain_window_layout_tip"));
            storageNaming = ModConfig.For(ModEntry.ModId).Define(
                "storage_naming", true, "", Text("setting_storage_naming"), Text("setting_storage_naming_tip"));
            storageCapacity = ModConfig.For(ModEntry.ModId).Define(
                "storage_capacity", true, "", Text("setting_storage_capacity"), Text("setting_storage_capacity_tip"));
            retainDocumentPage = ModConfig.For(ModEntry.ModId).Define(
                "retain_document_page", true, "", Text("setting_retain_document_page"), Text("setting_retain_document_page_tip"));
            appraisalAutomation = ModConfig.For(ModEntry.ModId).Define(
                "appraisal_automation", 0, "", Text("setting_appraisal_automation_hand"), 0, 4,
                Text("setting_appraisal_automation_tip"));
            ribwichAutomation = ModConfig.For(ModEntry.ModId).Define(
                "appraisal_automation_ribwich_v2", 0, "", Text("setting_appraisal_automation_ribwich"), 0, 3,
                Text("setting_appraisal_automation_tip"));
            plasmaAutomation = ModConfig.For(ModEntry.ModId).Define(
                "appraisal_automation_plasma_v2", 0, "", Text("setting_appraisal_automation_plasma"), 0, 2,
                Text("setting_appraisal_automation_tip"));
            cigaretteAutomation = ModConfig.For(ModEntry.ModId).Define(
                "cigarette_inspection_automation", 0, "", Text("setting_cigarette_automation"), 0, 3,
                Text("setting_cigarette_automation_tip"));
            injectorAutomation = ModConfig.For(ModEntry.ModId).Define(
                "injector_inspection_automation", 0, "", Text("setting_injector_automation"), 0, 2,
                Text("setting_injector_automation_tip"));
            augIdentification = ModConfig.For(ModEntry.ModId).Define(
                "aug_identification", false, "", Text("setting_aug_identification"), Text("setting_aug_identification_tip"));
            stolenMarker = ModConfig.For(ModEntry.ModId).Define(
                "stolen_marker", true, "", Text("setting_stolen_marker"), Text("setting_stolen_marker_tip"));
            contrabandMarker = ModConfig.For(ModEntry.ModId).Define(
                "contraband_marker", true, "", Text("setting_contraband_marker"), Text("setting_contraband_marker_tip"));
            singleCellValue = ModConfig.For(ModEntry.ModId).Define(
                "single_cell_value", true, "", Text("setting_single_cell_value"), Text("setting_single_cell_value_tip"));
            exchangeAutoPickup = ModConfig.For(ModEntry.ModId).Define(
                "exchange_auto_pickup", true, "", Text("setting_exchange_auto_pickup"),
                Text("setting_exchange_auto_pickup_tip"));
            autoDiscardAppraisalReceipt = ModConfig.For(ModEntry.ModId).Define(
                "auto_discard_appraisal_receipt", true, "", Text("setting_auto_discard_receipt"),
                Text("setting_auto_discard_receipt_tip"));
            nightServiceInformation = ModConfig.For(ModEntry.ModId).Define(
                "night_service_information", true, "", Text("setting_night_service_information"),
                Text("setting_night_service_information_tip"));
            powerWarning = ModConfig.For(ModEntry.ModId).Define(
                "power_warning", true, "", Text("setting_power_warning"), Text("setting_power_warning_tip"));
            overflowWarning = ModConfig.For(ModEntry.ModId).Define(
                "overflow_warning", true, "", Text("setting_overflow_warning"), Text("setting_overflow_warning_tip"));
            marketChangeMarker = ModConfig.For(ModEntry.ModId).Define(
                "market_change_marker", true, "", Text("setting_market_change_marker"), Text("setting_market_change_marker_tip"));
            moduleAffixMarker = ModConfig.For(ModEntry.ModId).Define(
                "module_affix_marker", true, "", Text("setting_module_affix_marker"), Text("setting_module_affix_marker_tip"));
            inspectionLayouts = ModConfig.For(ModEntry.ModId).Define(InspectionLayoutsKey, "", "Internal", () => InspectionLayoutsKey);
            tabletLayout = ModConfig.For(ModEntry.ModId).Define(TabletLayoutKey, "", "Internal", () => TabletLayoutKey);
            archiveLayout = ModConfig.For(ModEntry.ModId).Define(ArchiveLayoutKey, "", "Internal", () => ArchiveLayoutKey);
            trashLayout = ModConfig.For(ModEntry.ModId).Define(TrashLayoutKey, "", "Internal", () => TrashLayoutKey);
            vendingFountainLayout = ModConfig.For(ModEntry.ModId).Define(VendingFountainLayoutKey, "", "Internal", () => VendingFountainLayoutKey);
            vendingMachineLayout = ModConfig.For(ModEntry.ModId).Define(VendingMachineLayoutKey, "", "Internal", () => VendingMachineLayoutKey);
            waterScannerLayout = ModConfig.For(ModEntry.ModId).Define(WaterScannerLayoutKey, "", "Internal", () => WaterScannerLayoutKey);
            phoneLayout = ModConfig.For(ModEntry.ModId).Define(PhoneLayoutKey, "", "Internal", () => PhoneLayoutKey);
            freeStartSmugglerStorage = ModConfig.For(ModEntry.ModId).Define(FreeStartSmugglerStorageKey, "false", "Internal", () => FreeStartSmugglerStorageKey);
            freeStartContraband = ModConfig.For(ModEntry.ModId).Define(FreeStartContrabandKey, "false", "Internal", () => FreeStartContrabandKey);
            freeStartBlackLamps = ModConfig.For(ModEntry.ModId).Define(FreeStartBlackLampsKey, "false", "Internal", () => FreeStartBlackLampsKey);
            freeStartExchange = ModConfig.For(ModEntry.ModId).Define(FreeStartExchangeKey, "false", "Internal", () => FreeStartExchangeKey);
            freeStartMusic = ModConfig.For(ModEntry.ModId).Define(FreeStartMusicKey, "false", "Internal", () => FreeStartMusicKey);
            for (int i = 0; i < freeStartReputation.Length; i++)
            {
                string key = FreeStartReputationKey(i);
                freeStartReputation[i] = ModConfig.For(ModEntry.ModId).Define(key, "0", "Internal", () => key);
            }
            freeStartBailouts = ModConfig.For(ModEntry.ModId).Define(FreeStartBailoutsKey, "0", "Internal", () => FreeStartBailoutsKey);
            freeStartHydroponics = ModConfig.For(ModEntry.ModId).Define(FreeStartHydroponicsKey, "false", "Internal", () => FreeStartHydroponicsKey);
            freeStartRanching = ModConfig.For(ModEntry.ModId).Define(FreeStartRanchingKey, "false", "Internal", () => FreeStartRanchingKey);
            freeStartAquaculture = ModConfig.For(ModEntry.ModId).Define(FreeStartAquacultureKey, "false", "Internal", () => FreeStartAquacultureKey);
            freeStartChemistry = ModConfig.For(ModEntry.ModId).Define(FreeStartChemistryKey, "false", "Internal", () => FreeStartChemistryKey);
            freeStartFarmerContact = ModConfig.For(ModEntry.ModId).Define(FreeStartFarmerContactKey, "false", "Internal", () => FreeStartFarmerContactKey);
            freeStartRancherContact = ModConfig.For(ModEntry.ModId).Define(FreeStartRancherContactKey, "false", "Internal", () => FreeStartRancherContactKey);
            freeStartAquafarmerContact = ModConfig.For(ModEntry.ModId).Define(FreeStartAquafarmerContactKey, "false", "Internal", () => FreeStartAquafarmerContactKey);
            freeStartChemistContact = ModConfig.For(ModEntry.ModId).Define(FreeStartChemistContactKey, "false", "Internal", () => FreeStartChemistContactKey);
            freeStartGunsmithContact = ModConfig.For(ModEntry.ModId).Define(FreeStartGunsmithContactKey, "false", "Internal", () => FreeStartGunsmithContactKey);
            freeStartGunPermit = ModConfig.For(ModEntry.ModId).Define(FreeStartGunPermitKey, "false", "Internal", () => FreeStartGunPermitKey);
            freeStartRent = ModConfig.For(ModEntry.ModId).Define(FreeStartRentKey, "200", "Internal", () => FreeStartRentKey);
            freeStartRentGrowth = ModConfig.For(ModEntry.ModId).Define(FreeStartRentGrowthKey, "5", "Internal", () => FreeStartRentGrowthKey);
            freeStartPowerMultiplier = ModConfig.For(ModEntry.ModId).Define(FreeStartPowerMultiplierKey, "1", "Internal", () => FreeStartPowerMultiplierKey);
            freeStartVendorPermitFee = ModConfig.For(ModEntry.ModId).Define(FreeStartVendorPermitFeeKey, "1000", "Internal", () => FreeStartVendorPermitFeeKey);
            freeStartRunnerMarkup = ModConfig.For(ModEntry.ModId).Define(FreeStartRunnerMarkupKey, "15", "Internal", () => FreeStartRunnerMarkupKey);
            freeStartHypertechMarkup = ModConfig.For(ModEntry.ModId).Define(FreeStartHypertechMarkupKey, "50", "Internal", () => FreeStartHypertechMarkupKey);
            freeStartDoctorMarkup = ModConfig.For(ModEntry.ModId).Define(FreeStartDoctorMarkupKey, "15", "Internal", () => FreeStartDoctorMarkupKey);
            freeStartRetiredMarkup = ModConfig.For(ModEntry.ModId).Define(FreeStartRetiredMarkupKey, "15", "Internal", () => FreeStartRetiredMarkupKey);
            freeStartAppraisalJoinMultiplier = ModConfig.For(ModEntry.ModId).Define(FreeStartAppraisalJoinMultiplierKey, "1", "Internal", () => FreeStartAppraisalJoinMultiplierKey);
            freeStartInsuranceMultiplier = ModConfig.For(ModEntry.ModId).Define(FreeStartInsuranceMultiplierKey, "1", "Internal", () => FreeStartInsuranceMultiplierKey);
            freeStartRetailMarkup = ModConfig.For(ModEntry.ModId).Define(FreeStartRetailMarkupKey, "15", "Internal", () => FreeStartRetailMarkupKey);
            freeStartLandlordGrace = ModConfig.For(ModEntry.ModId).Define(FreeStartLandlordGraceKey, "false", "Internal", () => FreeStartLandlordGraceKey);
            freeStartCash = ModConfig.For(ModEntry.ModId).Define(FreeStartCashKey, "600", "Internal", () => FreeStartCashKey);
            freeStartWaterTools = ModConfig.For(ModEntry.ModId).Define(FreeStartWaterToolsKey, "false", "Internal", () => FreeStartWaterToolsKey);
            freeStartWaterContact = ModConfig.For(ModEntry.ModId).Define(FreeStartWaterContactKey, "false", "Internal", () => FreeStartWaterContactKey);
            freeStartForceSkip = ModConfig.For(ModEntry.ModId).Define(FreeStartForceSkipKey, "false", "Internal", () => FreeStartForceSkipKey);
            freeStartNoSkipSupplies = ModConfig.For(ModEntry.ModId).Define(FreeStartNoSkipSuppliesKey, "false", "Internal", () => FreeStartNoSkipSuppliesKey);
            freeStartStorage = ModConfig.For(ModEntry.ModId).Define(FreeStartStorageKey, "false", "Internal", () => FreeStartStorageKey);
            freeStartFood = ModConfig.For(ModEntry.ModId).Define(FreeStartFoodKey, "false", "Internal", () => FreeStartFoodKey);
            freeStartHousehold = ModConfig.For(ModEntry.ModId).Define(FreeStartHouseholdKey, "false", "Internal", () => FreeStartHouseholdKey);
        }

        private static Func<string> Text(string key)
        {
            return () => ModHelper.GetLocalized(ModEntry.ModId, key);
        }
    }
}
