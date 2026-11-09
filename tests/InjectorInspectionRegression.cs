using System;
using XicesBetterExperience;

public static class InjectorInspectionRegression
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var setting = ModConfig.Instance.AutomationEntries["injector_inspection_automation"];
        Check(setting.Value == 0 && ExperienceSettings.SettingsTab("injector_inspection_automation") == "automation", "Injector default off in automation tab");
        var item = new GameItem { InjectorFeature = new ItemFeature {
            current = new ItemCondition { identifier = "genuineInjector" },
            realCondition = new ItemCondition { identifier = "counterfeitInjector" } } };
        item.Tags.Add("INS_INJECTOR_TAG"); item.Tags.Add("ISSUE_SERIAL"); item.Tags.Add("ISSUE_COLOR");
        var ui = new InspectionUIManager { currentInspectingItem = item };
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(!ui.toggle3.isOn, "Off leaves native selection intact");
        setting.Set(2);
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(ui.toggle3.isOn && !ui.toggle5.isOn && ui.toggle1.interactable, "Select only first correct reason, no submission");
        item.Owned = true; ui.ResetAllConfrontToggle();
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(!ui.toggle3.isOn, "Owned injector excluded"); item.Owned = false;
        InspectionUIManager.Locked = true;
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(!ui.toggle3.isOn, "Locked trade excluded"); InspectionUIManager.Locked = false;
        item.Tags.Add("ALREADY_CONFRONTED_TAG");
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(!ui.toggle3.isOn, "Already confronted excluded"); item.Tags.Remove("ALREADY_CONFRONTED_TAG");
        item.InjectorFeature.current.identifier = "counterfeitInjector";
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(!ui.toggle3.isOn, "No genuine claim left unchanged"); item.InjectorFeature.current.identifier = "genuineInjector";
        item.Tags.Remove("ISSUE_SERIAL"); item.Tags.Remove("ISSUE_COLOR");
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(ui.toggle1.interactable, "Missing issue tags on counterfeit do not disable UI");
        item.InjectorFeature.realCondition.identifier = "genuineInjector";
        ui.toggle2.interactable = false;
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(!ui.toggle1.interactable && !ui.toggle5.interactable && !ui.toggle3.isOn, "Genuine clears and disables reasons");
        InjectorInspectionExit.Prefix();
        Check(ui.toggle1.interactable && !ui.toggle2.interactable && ui.toggle5.interactable, "Exit restores exact prior states");
        InjectorInspectionAutomation.Postfix(ui, item);
        InjectorInspectionAutomation.Prefix(ui);
        Check(ui.toggle1.interactable, "Object switch restores immediately");
        InjectorInspectionAutomation.Postfix(ui, item);
        new InjectorInspectionLifetime().OnDisable();
        Check(ui.toggle1.interactable, "Hidden or disabled panel restores immediately");
        InjectorInspectionAutomation.Postfix(ui, item);
        new InjectorInspectionLifetime().OnDestroy();
        Check(ui.toggle1.interactable, "Destroyed panel restores immediately");
        PlayerStore.Instance = new PlayerStore(); EmporiumEntry.Instance = new EmporiumEntry();
        setting.Set(1);
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(ui.toggle1.interactable, "Guide required");
        PlayerStore.Instance.Items.Add(new GameItemElement { identifier = "injector_guide", Owned = false });
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(ui.toggle1.interactable, "Customer guide does not qualify");
        PlayerStore.Instance.Items.Add(new GameItemElement { identifier = "injector_guide", Owned = true });
        InjectorInspectionAutomation.Postfix(ui, item);
        Check(!ui.toggle1.interactable, "Owned guide enables automation");
        setting.Set(0); InjectorInspectionAutomation.CheckSession();
        Check(ui.toggle1.interactable, "Turning off restores disabled controls");
        ModConfig.Instance.Entry.Set(true); HealthData.Readable = true;
        PaperUIManager.Instance = new PaperUIManager();
        AutoOpenAppraisalGuide.Postfix(item);
        Check(PaperUIManager.Instance.Opens == 1, "Guide opens independently of automation");
        AutoOpenAppraisalGuide.Postfix(item);
        Check(PaperUIManager.Instance.Opens == 1, "Already open guide stays open");
        AutoOpenAppraisalGuide.Prefix(ui);
        Check(!PaperUIManager.Instance.injectorGuidePanel.activeSelf, "Switch closes injector guide");
        HealthData.Readable = false; AutoOpenAppraisalGuide.Postfix(item);
        Check(!PaperUIManager.Instance.injectorGuidePanel.activeSelf, "Native reading restriction retained");
        HealthData.Readable = true;
        Check(AppraisalAutomationSlider.SelectionName("injector_inspection_automation", 1).EndsWith("injector_automation_level_1"), "Injector slider uses its own labels");
        Console.WriteLine("PASS: injector reasons, ownership/guide gates, immediate lifecycle restoration and independent paper guide.");
    }
}
