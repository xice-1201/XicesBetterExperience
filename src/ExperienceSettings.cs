using System;

namespace XicesBetterExperience
{
    public static class ExperienceSettings
    {
        private static ConfigEntry<bool> autoOpenAppraisalGuide;
        private static ConfigEntry<bool> retainWindowLayout;
        private static ConfigEntry<string> tabletLayout;
        private static ConfigEntry<string> phoneLayout;
        internal const string PhoneLayoutKey = "internal_phone_layout_v1";
        internal static string PhoneLayout { get { return phoneLayout == null ? "" : phoneLayout.Value; } }
        internal static void SavePhoneLayout(string value) { if (phoneLayout != null) phoneLayout.Set(value); }
        internal const string TabletLayoutKey = "internal_tablet_layout_v1";
        public static bool RetainWindowLayout { get { return retainWindowLayout == null || retainWindowLayout.Value; } }
        internal static string TabletLayout { get { return tabletLayout == null ? "" : tabletLayout.Value; } }
        internal static void SaveTabletLayout(string value) { if (tabletLayout != null) tabletLayout.Set(value); }
        internal static bool IsInternalSetting(string key) { return key == TabletLayoutKey || key == PhoneLayoutKey; }
        private static ConfigEntry<bool> storageNaming;
        private static ConfigEntry<bool> storageCapacity;
        public static bool StorageCapacity { get { return storageCapacity == null || storageCapacity.Value; } }
        private static ConfigEntry<bool> retainDocumentPage;
        private static ConfigEntry<int> appraisalAutomation;
        private static ConfigEntry<int> ribwichAutomation;
        private static ConfigEntry<int> plasmaAutomation;
        private static ConfigEntry<int> injectorAutomation;
        private static ConfigEntry<bool> augIdentification;
        public static bool AugIdentification { get { return augIdentification != null && augIdentification.Value; } }
        public static int InjectorAutomation { get { return injectorAutomation == null ? 0 : Math.Max(0, Math.Min(2, injectorAutomation.Value)); } }
        private static ConfigEntry<bool> stolenMarker;
        private static ConfigEntry<bool> contrabandMarker;
        private static ConfigEntry<bool> singleCellValue;
        private static ConfigEntry<bool> exchangeAutoPickup;
        private static ConfigEntry<bool> autoDiscardAppraisalReceipt;
        private static ConfigEntry<bool> powerWarning;
        private static ConfigEntry<bool> overflowWarning;
        private static ConfigEntry<bool> marketChangeMarker;

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
            return key == "injector_inspection_automation" ? 2 : key == "appraisal_automation_ribwich_v2" ? 3 : key == "appraisal_automation_plasma_v2" ? 2 : 4;
        }

        internal static int AutomationLevel(string key, int index)
        {
            index = Math.Max(0, Math.Min(AutomationMax(key), index));
            return index == 0 ? 0 : index + (4 - AutomationMax(key));
        }

        internal static bool IsAutomationSetting(string key)
        {
            return key == "injector_inspection_automation" || key == "appraisal_automation" || key == "appraisal_automation_ribwich_v2" || key == "appraisal_automation_plasma_v2";
        }

        internal static string SettingsTab(string key)
        {
            if (key == "aug_identification" || IsAutomationSetting(key)) return "automation";
            if (key == "overflow_warning") return "icons";
            if (key == "storage_naming" || key == "storage_capacity" || key == "stolen_marker" || key == "contraband_marker" ||
                key == "power_warning" || key == "market_change_marker") return "icons";
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
            autoOpenAppraisalGuide = ModConfig.For(ModEntry.ModId).Define(
                "auto_open_appraisal_guide", true, "", Text("setting_auto_open_guide"),
                Text("setting_auto_open_guide_tip"));
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
            powerWarning = ModConfig.For(ModEntry.ModId).Define(
                "power_warning", true, "", Text("setting_power_warning"), Text("setting_power_warning_tip"));
            overflowWarning = ModConfig.For(ModEntry.ModId).Define(
                "overflow_warning", true, "", Text("setting_overflow_warning"), Text("setting_overflow_warning_tip"));
            marketChangeMarker = ModConfig.For(ModEntry.ModId).Define(
                "market_change_marker", true, "", Text("setting_market_change_marker"), Text("setting_market_change_marker_tip"));
            tabletLayout = ModConfig.For(ModEntry.ModId).Define(TabletLayoutKey, "", "Internal", () => TabletLayoutKey);
            phoneLayout = ModConfig.For(ModEntry.ModId).Define(PhoneLayoutKey, "", "Internal", () => PhoneLayoutKey);
        }

        private static Func<string> Text(string key)
        {
            return () => ModHelper.GetLocalized(ModEntry.ModId, key);
        }
    }
}
