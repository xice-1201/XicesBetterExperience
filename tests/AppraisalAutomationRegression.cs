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
    public readonly List<string> Labels = new List<string>();
    public void BeginRow() { }
    public void AddLabel(string text) { Labels.Add(text); Last = new CustomUIElement { Text = text }; }
    public void End() { }
}
public static class AppraisalAutomationRegression
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static GameItemElement Target()
    {
        var item = new GameItemElement(); item.Tags.Add("APPRAISAL_ITEM"); item.Tags.Add("HAND_TAG"); return item;
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
        Check(ExperienceSettings.AppraisalAutomationFor(Target()) == 0 && ModConfig.Instance.AutomationMin == 0 && ModConfig.Instance.AutomationMax == 2,
            "Automation defaults off with type-specific ranges");
        string[] keys = { "appraisal_automation", "appraisal_automation_ribwich_v2", "appraisal_automation_plasma_v2" };
        string[] tags = { "HAND_TAG", "RIBWICH_TAG", "PLASMA_CELL_TAG" };
        for (int kind = 0; kind < 3; kind++)
        for (int setting = 0; setting <= ExperienceSettings.AutomationMax(keys[kind]); setting++)
        for (int certificate = 0; certificate <= 4; certificate++)
        {
            Reset();
            foreach (string key in keys) ModConfig.Instance.AutomationEntries[key].Set(0);
            ModConfig.Instance.AutomationEntries[keys[kind]].Set(setting);
            if (certificate > 0) PlayerStore.Instance.Items.Add(new GameItemElement { identifier = "cert" + certificate, Owned = true });
            var item = Target(); item.Tags.Remove("HAND_TAG"); item.Tags.Add(tags[kind]);
            bool native = AppraisalAutomation.Prefix(item);
            bool automatic = setting > 0 && certificate >= ExperienceSettings.AutomationLevel(keys[kind], setting);
            Check(native != automatic && AppraisalServiceHelper.Calls == (automatic ? 1 : 0), "All threshold/certificate combinations");
            if (automatic)
            {
                Check(item.Destroyed && AppraisalServiceHelper.Value == 789, "Exact value delegated to native settlement once");
                AutoOpenAppraisalGuide.Postfix(item, false);
                Check(AppraisalAutomation.Prefix(item), "Destroyed target cannot be settled again");
            }
        }
        Console.WriteLine("PASS: default-off sliders and all 60 per-item off/threshold/certificate combinations, exact-value native settlement and destroyed-target deduplication.");

        foreach (string key in keys) ModConfig.Instance.AutomationEntries[key].Set(4);
        Reset(); ModConfig.Instance.AutomationEntry.Set(4);
        var unrelated = Target(); unrelated.Tags.Remove("HAND_TAG");
        Check(ExperienceSettings.AppraisalAutomationFor(unrelated) == 0 && AppraisalAutomation.Prefix(unrelated), "Unknown appraisal types stay manual");
        for (int i = 0; i < keys.Length; i++)
            Check(ExperienceSettings.SettingsTab(keys[i]) == "automation", "All three appraisals belong to automation tab");
        Check(ExperienceSettings.SettingsTab("power_warning") == "icons" &&
            ExperienceSettings.SettingsTab("auto_open_appraisal_guide") == "general", "Existing tab assignment preserved");
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

        var builders = new List<CustomUIBuilder>();
        var selectedLabels = new List<CustomUIElement>();
        foreach (string key in keys)
        {
            var builder = new CustomUIBuilder();
            var configEntry = new ConfigEntry { Key = key, BoxedValue = 4 };
            AppraisalAutomationSlider.Postfix(builder, ModConfig.Instance, configEntry);
            builders.Add(builder); selectedLabels.Add(builder.Last);
            Check(builder.Labels.Count == 2 && builder.Labels[0].EndsWith("automation_selected"), "Only current selection caption and level are shown");
            Check(builder.Last.Text.EndsWith("automation_level_4"), "Initial expert label");
            for (int i = 0; i <= ExperienceSettings.AutomationMax(key); i++)
            {
                builder.Handle.Slider.Callback(i);
                Check(builder.Last.Text.EndsWith("automation_level_" + ExperienceSettings.AutomationLevel(key, i)), "Localized slider step");
            }
        }
        builders[0].Handle.Slider.Callback(0);
        Check(selectedLabels[0].Text.EndsWith("automation_level_0") && selectedLabels[1].Text.EndsWith("automation_level_4"), "Slider labels remain independent");
        AppraisalAutomationSliderReset.Postfix(builders[0].Handle.Slider, 4);
        Check(selectedLabels[0].Text.EndsWith("automation_level_4"), "Reset updates current level label");
        Check(ExperienceSettings.StorageNaming && ExperienceSettings.RetainDocumentPage, "Other settings remain independent");
        Console.WriteLine("PASS: five localized slider labels, current selection and native reset refresh.");
    }
}
