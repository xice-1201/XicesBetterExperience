using HarmonyLib;

namespace XicesBetterExperience
{
    internal static class AppraisalReceiptState
    {
        internal static bool CreatingReceipt;
        internal static bool EligibleAppraisal;
        internal static bool CreatingEventSlip;
        internal static bool CreatingInspectionSlip;
        internal static bool CreatingChemicalFeedbackSlip;
    }

    [HarmonyPatch(typeof(ContrabandHelper), "LeaveInspectionSlip", new[] { typeof(string) })]
    internal static class AutoDiscardInspectionSlipAroundCreation
    {
        public static void Prefix(out bool __state)
        {
            __state = AppraisalReceiptState.CreatingInspectionSlip;
            AppraisalReceiptState.CreatingInspectionSlip = ModEntry.IsEnabled &&
                ExperienceSettings.AutoDiscardAppraisalReceipt;
        }

        public static void Finalizer(bool __state)
        {
            AppraisalReceiptState.CreatingInspectionSlip = __state;
        }
    }

    [HarmonyPatch(typeof(StoreEvent), "CreateAndAddSlip", new[] { typeof(int) })]
    internal static class AutoDiscardEventSlipAroundCreation
    {
        public static void Prefix(StoreEvent __instance)
        {
            AppraisalReceiptState.CreatingEventSlip = ModEntry.IsEnabled &&
                ExperienceSettings.AutoDiscardAppraisalReceipt && __instance != null;
        }

        public static void Postfix()
        {
            AppraisalReceiptState.CreatingEventSlip = false;
        }
    }

    // Chemical complaints use a hand-note too, but are created through a
    // separate native path from event and inspection slips.
    [HarmonyPatch(typeof(ChemicalFeedbackHelper), "QueueHandnoteFeedback",
        new[] { typeof(System.Collections.Generic.List<string>), typeof(SoldItem), typeof(int) })]
    internal static class AutoDiscardChemicalFeedbackSlipAroundCreation
    {
        public static void Prefix(out bool __state)
        {
            __state = AppraisalReceiptState.CreatingChemicalFeedbackSlip;
            AppraisalReceiptState.CreatingChemicalFeedbackSlip = ModEntry.IsEnabled &&
                ExperienceSettings.AutoDiscardAppraisalReceipt;
        }

        public static void Finalizer(bool __state)
        {
            AppraisalReceiptState.CreatingChemicalFeedbackSlip = __state;
        }
    }

    [HarmonyPatch(typeof(AppraisalServiceHelper), "GetAcceptEstimateDialog", new[] { typeof(GameItem), typeof(int) })]
    internal static class AutoDiscardAppraisalReceiptForSupportedItems
    {
        public static void Prefix(GameItem __0)
        {
            AppraisalReceiptState.EligibleAppraisal = __0 != null &&
                (__0.IsTag("HAND_TAG") || __0.IsTag("RIBWICH_TAG") || __0.IsTag("PLASMA_CELL_TAG"));
        }

        public static void Postfix()
        {
            AppraisalReceiptState.EligibleAppraisal = false;
        }
    }

    [HarmonyPatch(typeof(AppraisalServiceHelper), "GiveAppraisalFee", new[] { typeof(int), typeof(int), typeof(int) })]
    internal static class AutoDiscardAppraisalReceiptAroundFee
    {
        public static void Prefix()
        {
            AppraisalReceiptState.CreatingReceipt = ModEntry.IsEnabled &&
                ExperienceSettings.AutoDiscardAppraisalReceipt && AppraisalReceiptState.EligibleAppraisal;
        }

        public static void Postfix()
        {
            AppraisalReceiptState.CreatingReceipt = false;
            AppraisalReceiptState.EligibleAppraisal = false;
        }
    }

    [HarmonyPatch(typeof(PlayerStore), "AddDirectSellingItemToTable",
        new[] { typeof(GameItem), typeof(bool), typeof(bool), typeof(bool), typeof(int) })]
    internal static class AutoDiscardAppraisalReceipt
    {
        public static void Postfix(GameItem __0)
        {
            if (!ModEntry.IsEnabled || !ExperienceSettings.AutoDiscardAppraisalReceipt || __0 == null) return;

            // Security reports include wanted captures, stolen/contraband reports
            // and wrongful reports. Several arrive from delayed arrest callbacks,
            // outside the original report method's execution scope.
            bool securityReceipt = __0.identifier == "sec_slip";
            if ((!AppraisalReceiptState.CreatingReceipt &&
                !AppraisalReceiptState.CreatingEventSlip &&
                !AppraisalReceiptState.CreatingInspectionSlip &&
                !AppraisalReceiptState.CreatingChemicalFeedbackSlip &&
                !securityReceipt)) return;

            GraphNodeStorage trash = EmporiumEntry.Instance == null ? null : EmporiumEntry.Instance.trashcan;
            if (trash == null)
            {
                return;
            }
            GraphUtils.TryAcceptAllOrDestroy(trash, __0, "automatic appraisal receipt cleanup");
        }
    }
}
