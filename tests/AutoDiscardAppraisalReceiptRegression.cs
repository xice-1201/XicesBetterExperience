using System;
using XicesBetterExperience;

public static class AutoDiscardAppraisalReceiptRegression
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    public static void Run(ModEntry entry)
    {
        Check(ExperienceSettings.AutoDiscardAppraisalReceipt && ModConfig.Instance.AutoDiscardReceiptEntry.Value,
            "Receipt cleanup defaults on");
        var trash = new GameItem();
        EmporiumEntry.Instance = new EmporiumEntry { trashcan = trash };
        var source = new GameGridInventory();
        var receipt = new GameItem();
        source.TryAcceptAll(receipt);

        var inspected = new GameItem(); inspected.Tags.Add("HAND_TAG");
        AutoDiscardAppraisalReceiptForSupportedItems.Prefix(inspected);
        AutoDiscardAppraisalReceiptAroundFee.Prefix();
        AutoDiscardAppraisalReceipt.Postfix(receipt);
        Check(!source.childItems.Contains(receipt), "Receipt leaves its source inventory");
        Check(GraphUtils.LastTarget == trash, "Receipt is sent to the native trashcan");
        Check(trash.children.Contains(receipt), "Receipt becomes a trashcan child");
        AutoDiscardAppraisalReceiptAroundFee.Postfix();

        bool previousInspectionState;
        AutoDiscardInspectionSlipAroundCreation.Prefix(out previousInspectionState);
        var inspectionSlip = new GameItem();
        source.TryAcceptAll(inspectionSlip);
        AutoDiscardAppraisalReceipt.Postfix(inspectionSlip);
        Check(trash.children.Contains(inspectionSlip), "Security confiscation receipt is sent to trash");
        AutoDiscardInspectionSlipAroundCreation.Finalizer(previousInspectionState);
        Check(!AppraisalReceiptState.CreatingInspectionSlip, "Inspection scope is cleared after creation");

        AutoDiscardInspectionSlipAroundCreation.Prefix(out previousInspectionState);
        bool nestedInspectionState;
        AutoDiscardInspectionSlipAroundCreation.Prefix(out nestedInspectionState);
        AutoDiscardInspectionSlipAroundCreation.Finalizer(nestedInspectionState);
        Check(AppraisalReceiptState.CreatingInspectionSlip, "Nested creation restores outer scope");
        // Harmony runs the finalizer even when the original method throws.
        AutoDiscardInspectionSlipAroundCreation.Finalizer(previousInspectionState);
        Check(!AppraisalReceiptState.CreatingInspectionSlip, "Exception cleanup restores inactive scope");

        EmporiumEntry.Instance.trashcan = null;
        AutoDiscardInspectionSlipAroundCreation.Prefix(out previousInspectionState);
        var retainedSlip = new GameItem();
        source.TryAcceptAll(retainedSlip);
        AutoDiscardAppraisalReceipt.Postfix(retainedSlip);
        AutoDiscardAppraisalReceipt.Postfix(null);
        AutoDiscardInspectionSlipAroundCreation.Finalizer(previousInspectionState);
        Check(retainedSlip.parentInventory == source, "Missing trashcan preserves inspection receipt");
        EmporiumEntry.Instance.trashcan = trash;

        var information = new StoreEvent { identifier = "water_treatment_breakdown" };
        AutoDiscardEventSlipAroundCreation.Prefix(information);
        var slip = new GameItem();
        AutoDiscardAppraisalReceipt.Postfix(slip);
        AutoDiscardEventSlipAroundCreation.Postfix();
        Check(GraphUtils.LastTarget == trash, "Information slip is sent to the native trashcan");
        Check(trash.children.Contains(slip), "Information slip becomes a trashcan child");

        var unrelatedEvent = new StoreEvent { identifier = "construction_project" };
        AutoDiscardEventSlipAroundCreation.Prefix(unrelatedEvent);
        var unrelatedSlip = new GameItem();
        AutoDiscardAppraisalReceipt.Postfix(unrelatedSlip);
        AutoDiscardEventSlipAroundCreation.Postfix();
        Check(GraphUtils.LastTarget == trash, "Ordinary rumor slip is sent to the native trashcan");
        Check(trash.children.Contains(unrelatedSlip), "Ordinary rumor slip becomes a trashcan child");

        AutoDiscardChemicalFeedbackSlipAroundCreation.Prefix(out previousInspectionState);
        var chemicalSlip = new GameItem();
        source.TryAcceptAll(chemicalSlip);
        AutoDiscardAppraisalReceipt.Postfix(chemicalSlip);
        Check(trash.children.Contains(chemicalSlip), "Chemical feedback slip is sent to trash");
        AutoDiscardChemicalFeedbackSlipAroundCreation.Finalizer(previousInspectionState);
        Check(!AppraisalReceiptState.CreatingChemicalFeedbackSlip, "Chemical feedback scope is cleared after creation");

        var unrelated = new GameItem();
        source.TryAcceptAll(unrelated);
        AutoDiscardAppraisalReceiptForSupportedItems.Prefix(new GameItem());
        AutoDiscardAppraisalReceiptAroundFee.Postfix();
        AutoDiscardAppraisalReceipt.Postfix(unrelated);
        Check(unrelated.parentInventory == source, "Unrelated item remains untouched");

        // All five native report outcomes reach this sink as sec_slip, even
        // when generated later by the arrest sequence rather than on report.
        foreach (string outcome in new[] { "wanted report", "wanted shot", "thief report", "contraband report", "wrongful report" })
        {
            var securityReceipt = new GameItem { identifier = "sec_slip" };
            source.TryAcceptAll(securityReceipt);
            AutoDiscardAppraisalReceipt.Postfix(securityReceipt);
            Check(trash.children.Contains(securityReceipt) && !source.childItems.Contains(securityReceipt), outcome + " receipt goes to trash without creation scope");
        }
        var reportWithoutTrash = new GameItem { identifier = "sec_slip" };
        source.TryAcceptAll(reportWithoutTrash);
        EmporiumEntry.Instance.trashcan = null;
        AutoDiscardAppraisalReceipt.Postfix(reportWithoutTrash);
        Check(reportWithoutTrash.parentInventory == source, "Missing trash preserves report receipt");
        EmporiumEntry.Instance.trashcan = trash;

        entry.OnDisable();
        AutoDiscardAppraisalReceipt.Postfix(reportWithoutTrash);
        Check(reportWithoutTrash.parentInventory == source, "Disabled mod preserves report receipt");
        entry.OnEnable();

        ModConfig.Instance.AutoDiscardReceiptEntry.Set(false);
        AutoDiscardAppraisalReceipt.Postfix(reportWithoutTrash);
        Check(reportWithoutTrash.parentInventory == source, "Disabled setting preserves report receipt");
        AutoDiscardChemicalFeedbackSlipAroundCreation.Prefix(out previousInspectionState);
        AutoDiscardAppraisalReceipt.Postfix(unrelated);
        AutoDiscardChemicalFeedbackSlipAroundCreation.Finalizer(previousInspectionState);
        Check(unrelated.parentInventory == source, "Disabled cleanup preserves chemical feedback notes");
        AutoDiscardInspectionSlipAroundCreation.Prefix(out previousInspectionState);
        AutoDiscardAppraisalReceipt.Postfix(unrelated);
        AutoDiscardInspectionSlipAroundCreation.Finalizer(previousInspectionState);
        Check(unrelated.parentInventory == source, "Disabled cleanup preserves inspection receipt");
        AutoDiscardAppraisalReceiptAroundFee.Prefix();
        AutoDiscardAppraisalReceipt.Postfix(unrelated);
        Check(unrelated.parentInventory == source, "Disabled receipt cleanup leaves items untouched");
        AutoDiscardAppraisalReceiptAroundFee.Postfix();
        Console.WriteLine("PASS: appraisal receipt cleanup defaults on, honors its toggle and routes receipts to trash.");
    }
}
