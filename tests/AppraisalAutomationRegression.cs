using System;
using System.Collections.Generic;
using XicesBetterExperience;

public class StoreClient
{
    public enum ClientIntent { APPRAISAL_SERVICE, TRADE }
    public ClientIntent clientIntent = ClientIntent.APPRAISAL_SERVICE;
}
public class TestClient
{
    public StoreClient Blueprint = new StoreClient();
    public StoreClient GetClientBlueprint() { return Blueprint; }
}
public static class AppraisalRankHelper
{
    public static int GetRank() { return PlayerStore.Instance.appraiserLevel; }
    public static int GetCertificateRank(GameItem item)
    {
        switch (item.identifier) { case "cert1": return 0; case "cert2": return 1; case "cert3": return 2; case "cert4": return 3; default: return -1; }
    }
}
public static class AppraisalHelper
{
    public static int Value = 400;
    public static bool Fail;
    public static int GetCorrectBaseValue(GameItem item) { if (Fail) throw new Exception("value failure"); return Value; }
}
public static class AppraisalServiceHelper
{
    public static int Calls, Value;
    public static bool Fail;
    public static object GetAcceptEstimateDialog(GameItem item, int value) { return null; }
    public static void OnEstimate(GameItem item, int value)
    {
        Calls++; Value = value;
        if (Fail) throw new Exception("settlement failure");
        ((GameItemElement)item).Destroy();
    }
}
public static class ModConfigWindow { }
public class CustomUIElement
{
    public string Text;
    public bool IsAlive = true;
    public Action<float> Callback;
    public CustomUIElement SetText(string text) { Text = text; return this; }
    public CustomUIElement OnSlider(Action<float> callback) { Callback = callback; return this; }
}
public class CustomUIWindow
{
    public readonly CustomUIElement Slider = new CustomUIElement();
    public CustomUIElement Get(string key) { return Slider; }
}
public class CustomUIBuilder
{
    public CustomUIWindow Handle = new CustomUIWindow();
    public CustomUIElement Last;
    public void BeginRow() { }
    public void AddLabel(string text) { Last = new CustomUIElement { Text = text }; }
    public void End() { }
}
public static class AppraisalAutomationRegression
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static GameItemElement Target()
    {
        var item = new GameItemElement(); item.Tags.Add("APPRAISAL_ITEM"); return item;
    }
    private static void Reset()
    {
        PlayerStore.Instance = new PlayerStore { currentClientInstance = new TestClient() };
        EmporiumEntry.Instance = new EmporiumEntry();
        AppraisalServiceHelper.Calls = 0; AppraisalServiceHelper.Fail = false;
        AppraisalHelper.Value = 789; AppraisalHelper.Fail = false;
        UnityEngine.Debug.Errors = 0;
    }
    public static void Run(ModEntry entry)
    {
        entry.OnEnable();
        Check(ExperienceSettings.AppraisalAutomation == 4 && ModConfig.Instance.AutomationMin == 0 && ModConfig.Instance.AutomationMax == 4,
            "Five-step integer slider defaults to expert");
        for (int setting = 0; setting <= 4; setting++)
        for (int certificate = 0; certificate <= 4; certificate++)
        {
            Reset(); ModConfig.Instance.AutomationEntry.Set(setting);
            if (certificate > 0) PlayerStore.Instance.Items.Add(new GameItemElement { identifier = "cert" + certificate, Owned = true });
            var item = Target();
            bool native = AppraisalAutomation.Prefix(item);
            bool automatic = setting > 0 && certificate >= setting;
            Check(native != automatic && AppraisalServiceHelper.Calls == (automatic ? 1 : 0), "All threshold/certificate combinations");
            if (automatic)
            {
                Check(item.Destroyed && AppraisalServiceHelper.Value == 789, "Exact value delegated to native settlement once");
                AutoOpenAppraisalGuide.Postfix(item, false);
                Check(AppraisalAutomation.Prefix(item), "Destroyed target cannot be settled again");
            }
        }
        Console.WriteLine("PASS: default expert slider and all 25 off/threshold/certificate combinations, exact-value native settlement and destroyed-target deduplication.");

        Reset(); ModConfig.Instance.AutomationEntry.Set(4);
        var cert = new GameItemElement { identifier = "cert4", Owned = false };
        PlayerStore.Instance.Items.Add(cert);
        Check(AppraisalAutomation.Prefix(Target()), "Unowned certificate rejected");
        cert.Owned = true; cert.Destroyed = true;
        Check(AppraisalAutomation.Prefix(Target()), "Destroyed certificate rejected");
        cert.Destroyed = false;
        PlayerStore.Instance.appraiserLevel = 2;
        Check(AppraisalAutomation.Prefix(Target()), "Actual qualification required");
        PlayerStore.Instance.appraiserLevel = 3; PlayerStore.Instance.isAppraisalIntroduced = false;
        Check(AppraisalAutomation.Prefix(Target()), "Guild membership required");
        PlayerStore.Instance.isAppraisalIntroduced = true;
        var owned = Target(); owned.Owned = true;
        Check(AppraisalAutomation.Prefix(owned), "Player possessions never destroyed for appraisal fees");
        Check(AppraisalAutomation.Prefix(new GameItemElement()), "Non-appraisal item ignored");
        PlayerStore.Instance.currentClientInstance.Blueprint.clientIntent = StoreClient.ClientIntent.TRADE;
        Check(AppraisalAutomation.Prefix(Target()), "Trade customer ignored");
        PlayerStore.Instance.currentClientInstance = null;
        Check(AppraisalAutomation.Prefix(Target()), "No service customer ignored");
        PlayerStore.Instance.currentClientInstance = new TestClient();
        AppraisalHelper.Value = 0; Check(AppraisalAutomation.Prefix(Target()), "Invalid true value falls back");
        AppraisalHelper.Fail = true; Check(AppraisalAutomation.Prefix(Target()), "Preparation failure falls back");
        AppraisalHelper.Fail = false; AppraisalHelper.Value = 789;
        AppraisalServiceHelper.Fail = true;
        Check(!AppraisalAutomation.Prefix(Target()) && AppraisalServiceHelper.Calls == 1, "No manual retry after partially failing settlement");
        AppraisalServiceHelper.Fail = false;
        Check(!AppraisalAutomation.Prefix(Target()) && AppraisalServiceHelper.Calls == 2, "Failure releases reentrancy guard");
        entry.OnDisable(); Check(AppraisalAutomation.Prefix(Target()), "Disabled mod ignored"); entry.OnEnable();
        Console.WriteLine("PASS: ownership, qualification, service intent, invalid values, exception isolation and disabled mod guards.");

        var builder = new CustomUIBuilder();
        var configEntry = new ConfigEntry { Key = "appraisal_automation", BoxedValue = 4 };
        AppraisalAutomationSlider.Postfix(builder, ModConfig.Instance, configEntry);
        var label = builder.Last;
        Check(label.Text.EndsWith("automation_level_4"), "Initial expert label");
        for (int i = 0; i <= 4; i++)
        {
            builder.Handle.Slider.Callback(i);
            Check(label.Text.EndsWith("automation_level_" + i), "Localized slider step");
        }
        AppraisalAutomationSliderReset.Postfix(builder.Handle.Slider, 4);
        Check(label.Text.EndsWith("automation_level_4"), "Reset updates current level label");
        Check(ExperienceSettings.StorageNaming && ExperienceSettings.RetainDocumentPage, "Other settings remain independent");
        Console.WriteLine("PASS: five localized slider labels, current selection and native reset refresh.");
    }
}
