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

        var unrelated = new GameItem();
        source.TryAcceptAll(unrelated);
        AutoDiscardAppraisalReceiptForSupportedItems.Prefix(new GameItem());
        AutoDiscardAppraisalReceiptAroundFee.Postfix();
        AutoDiscardAppraisalReceipt.Postfix(unrelated);
        Check(unrelated.parentInventory == source, "Unrelated item remains untouched");

        ModConfig.Instance.AutoDiscardReceiptEntry.Set(false);
        AutoDiscardAppraisalReceiptAroundFee.Prefix();
        AutoDiscardAppraisalReceipt.Postfix(unrelated);
        Check(unrelated.parentInventory == source, "Disabled receipt cleanup leaves items untouched");
        AutoDiscardAppraisalReceiptAroundFee.Postfix();
        Console.WriteLine("PASS: appraisal receipt cleanup defaults on, honors its toggle and routes receipts to trash.");
    }
}
