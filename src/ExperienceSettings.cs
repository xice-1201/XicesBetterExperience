using System;

namespace XicesBetterExperience
{
    public static class ExperienceSettings
    {
        private static ConfigEntry<bool> autoOpenAppraisalGuide;
        private static ConfigEntry<bool> storageNaming;
        private static ConfigEntry<bool> retainDocumentPage;
        private static ConfigEntry<int> appraisalAutomation;
        private static ConfigEntry<bool> stolenMarker;
        private static ConfigEntry<bool> contrabandMarker;
        private static ConfigEntry<bool> singleCellValue;
        private static ConfigEntry<bool> exchangeAutoPickup;
        private static ConfigEntry<bool> autoDiscardAppraisalReceipt;
        private static ConfigEntry<bool> powerWarning;
        private static ConfigEntry<bool> marketChangeMarker;

        public static int AppraisalAutomation
        {
            get { return appraisalAutomation == null ? 4 : Math.Max(0, Math.Min(4, appraisalAutomation.Value)); }
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

        public static void Register()
        {
            if (autoOpenAppraisalGuide != null) return;
            autoOpenAppraisalGuide = ModConfig.For(ModEntry.ModId).Define(
                "auto_open_appraisal_guide", true, "", Text("setting_auto_open_guide"),
                Text("setting_auto_open_guide_tip"));
            storageNaming = ModConfig.For(ModEntry.ModId).Define(
                "storage_naming", true, "", Text("setting_storage_naming"), Text("setting_storage_naming_tip"));
            retainDocumentPage = ModConfig.For(ModEntry.ModId).Define(
                "retain_document_page", true, "", Text("setting_retain_document_page"), Text("setting_retain_document_page_tip"));
            appraisalAutomation = ModConfig.For(ModEntry.ModId).Define(
                "appraisal_automation", 4, "", Text("setting_appraisal_automation"), 0, 4,
                Text("setting_appraisal_automation_tip"));
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
            marketChangeMarker = ModConfig.For(ModEntry.ModId).Define(
                "market_change_marker", true, "", Text("setting_market_change_marker"), Text("setting_market_change_marker_tip"));
        }

        private static Func<string> Text(string key)
        {
            return () => ModHelper.GetLocalized(ModEntry.ModId, key);
        }
    }
}
