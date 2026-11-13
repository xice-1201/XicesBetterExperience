using System;
using XicesBetterExperience;

public static class CigaretteInspectionRegression
{
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static GameItem Target(bool claimed, bool real)
    {
        var item = new GameItem { identifier = "packet_red_cigarette", CigaretteFeature = new ItemFeature {
            current = new ItemCondition { identifier = claimed ? "genuineCigarette" : "counterfeitCigarette" },
            realCondition = new ItemCondition { identifier = real ? "genuineCigarette" : "counterfeitCigarette" } } };
        item.Tags.Add("CIGARETTE_TAG");
        if (!real) { item.Tags.Add("CIGARETTE_COLOR_ISSUE"); item.Tags.Add("CIGARETTE_LOGO_ISSUE"); }
        return item;
    }
    static void Begin(InspectionUIManager ui, GameItem item)
    {
        InjectorInspectionAutomation.Prefix(ui);
        InjectorInspectionReset.Prefix();
        ui.ResetAllConfrontToggle(); ui.currentInspectingItem = item;
        CigaretteInspectionAutomation.Postfix(ui, item);
    }
    public static void Run()
    {
        var setting = ModConfig.Instance.AutomationEntries["cigarette_inspection_automation"];
        Check(setting.Value == 0 && ExperienceSettings.SettingsTab("cigarette_inspection_automation") == "automation" &&
            ExperienceSettings.AutomationMax("cigarette_inspection_automation") == 3, "Cigarette slider default off with four choices in automation tab");
        for (int i = 0; i < 4; i++) Check(AppraisalAutomationSlider.SelectionName("cigarette_inspection_automation", i).EndsWith("cigarette_automation_level_" + i), "Cigarette selection uses its own label");
        var ui = new InspectionUIManager();
        PlayerStore.Instance = new PlayerStore(); EmporiumEntry.Instance = new EmporiumEntry();
        var guide = new GameItemElement {identifier="cigarette_guide",Owned=true};
        var color = new GameItemElement {identifier="cigarette_color",Owned=true};
        for (int level=0; level<4; level++)
        for (int books=0; books<4; books++)
        for (int claim=0; claim<2; claim++)
        for (int actual=0; actual<2; actual++)
        {
            setting.Set(level); PlayerStore.Instance.Items.Clear();
            if ((books&1)!=0) PlayerStore.Instance.Items.Add(guide);
            if ((books&2)!=0) PlayerStore.Instance.Items.Add(color);
            var item=Target(claim==1,actual==1); Begin(ui,item);
            bool enabled=level==3 || level==2&&(books&1)!=0 || level==1&&books==3;
            if (!enabled) Check(!ui.toggle1.isOn&&!ui.toggle2.isOn&&ui.toggle1.interactable&&ui.toggle5.interactable,"Unmet prerequisites preserve controls");
            else if (claim==1&&actual==1) Check(!ui.toggle1.interactable&&!ui.toggle5.interactable&&!ui.toggle1.isOn,"Genuine/genuine disables all reasons");
            else if (claim==1) Check(ui.toggle2.isOn&&!ui.toggle4.isOn&&!ui.toggle1.isOn&&ui.toggle1.interactable,"Claimed genuine selects only first valid issue");
            else if (actual==1) Check(ui.toggle1.isOn&&!ui.toggle2.isOn&&ui.toggle1.interactable,"Claimed counterfeit selects genuine reason");
            else Check(!ui.toggle1.interactable&&ui.toggle2.interactable&&!ui.toggle1.isOn,"Counterfeit/counterfeit disables only genuine reason");
            InjectorInspectionExit.Prefix();
            Check(ui.toggle1.interactable&&ui.toggle5.interactable,"All combinations restore on exit");
        }
        setting.Set(3);
        var genuine=Target(true,true); ui.toggle3.interactable=false; Begin(ui,genuine);
        new InjectorInspectionLifetime().OnDisable();
        Check(ui.toggle1.interactable&&!ui.toggle3.interactable,"Hidden inspection restores exact prior states");ui.toggle3.interactable=true;
        Begin(ui,genuine);new InjectorInspectionLifetime().OnDestroy();Check(ui.toggle1.interactable,"Destroyed inspection restores controls");
        Begin(ui,genuine);setting.Set(0);InjectorInspectionSession.Postfix();Check(ui.toggle1.interactable,"Turning off releases controls");setting.Set(3);
        Begin(ui,genuine);ui.currentInspectingItem=Target(false,false);InjectorInspectionSession.Postfix();Check(ui.toggle1.interactable,"Session change releases controls");
        var fake=Target(true,false);fake.Tags.Clear();fake.Tags.Add("CIGARETTE_TAG");Begin(ui,fake);
        Check(!ui.toggle1.isOn&&!ui.toggle2.isOn&&ui.toggle1.interactable,"Missing issue data on fake does not infer authenticity");
        foreach (string flag in new[]{"owned","locked","confronted","hidden","wrong","missing","invalid"})
        {
            var item=Target(true,false);ui.confrontPanel.activeSelf=true;
            if(flag=="owned")item.Owned=true;
            if(flag=="locked")InspectionUIManager.Locked=true;
            if(flag=="confronted")item.Tags.Add("ALREADY_CONFRONTED_TAG");
            if(flag=="hidden")ui.confrontPanel.activeSelf=false;
            if(flag=="wrong")item.identifier="other";
            if(flag=="missing")item.CigaretteFeature=null;
            if(flag=="invalid")item.CigaretteFeature.realCondition.identifier="unknown";
            Begin(ui,item);Check(!ui.toggle2.isOn&&ui.toggle1.interactable,"Excluded inspection: "+flag);
            InspectionUIManager.Locked=false;
        }
        ui.confrontPanel.activeSelf=true;
        ui.ResetAllConfrontToggle();CigaretteInspectionAutomation.Postfix(ui,Target(true,false),false);Check(!ui.toggle2.isOn,"Skipped native inspection does not automate");
        PlayerStore.Instance.Items.Clear();guide.Destroyed=true;PlayerStore.Instance.Items.Add(guide);setting.Set(2);Begin(ui,Target(true,false));Check(!ui.toggle2.isOn,"Destroyed guide does not qualify");
        guide.Destroyed=false;guide.Owned=false;Begin(ui,Target(true,false));Check(!ui.toggle2.isOn,"Customer guide does not qualify");guide.Owned=true;
        setting.Set(3);Begin(ui,genuine);
        var injector=new GameItem {InjectorFeature=new ItemFeature {current=new ItemCondition {identifier="genuineInjector"},realCondition=new ItemCondition {identifier="genuineInjector"}}};injector.Tags.Add("INS_INJECTOR_TAG");
        InjectorInspectionAutomation.Prefix(ui);ui.currentInspectingItem=injector;
        ModConfig.Instance.AutomationEntries["injector_inspection_automation"].Set(2);InjectorInspectionAutomation.Postfix(ui,injector);
        Check(!ui.toggle1.interactable,"Injector applies its own disabled state after cigarette restoration");
        Begin(ui,Target(false,true));Check(ui.toggle1.interactable&&ui.toggle1.isOn,"Cigarette after injector restores shared controls before selection");
        InjectorInspectionExit.Prefix();ModConfig.Instance.AutomationEntries["injector_inspection_automation"].Set(0);
        setting.Set(0);ModConfig.Instance.Entry.Set(true);PaperUIManager.Instance=new PaperUIManager();HealthData.Readable=true;
        PlayerStore.Instance.Items.Clear();PlayerStore.Instance.Items.Add(guide);PlayerStore.Instance.Items.Add(color);
        var paper=PaperUIManager.Instance;AutoOpenAppraisalGuide.Postfix(genuine);AutoOpenAppraisalGuide.Postfix(genuine);
        Check(paper.CigaretteOpens==1&&paper.ColorOpens==1,"Both owned documents open once independently of automation");
        ui.currentInspectingItem=genuine;AutoOpenAppraisalGuide.Prefix(ui);
        Check(!paper.cigaretteGuidePanel.activeSelf&&!paper.cigaretteColorPanel.activeSelf,"Switching inspection closes both documents");
        HealthData.Readable=false;AutoOpenAppraisalGuide.Postfix(genuine);
        Check(!paper.cigaretteGuidePanel.activeSelf&&paper.cigaretteColorPanel.activeSelf,"Color chart remains usable while guide cannot be read");
        AutoCloseAppraisalGuide.Postfix(-3);HealthData.Readable=true;
        PlayerStore.Instance.Items.Remove(color);AutoOpenAppraisalGuide.Postfix(genuine);
        Check(paper.cigaretteGuidePanel.activeSelf&&!paper.cigaretteColorPanel.activeSelf,"Guide alone opens independently");AutoCloseAppraisalGuide.Postfix(-3);
        PlayerStore.Instance.Items.Clear();PlayerStore.Instance.Items.Add(color);AutoOpenAppraisalGuide.Postfix(genuine);
        Check(!paper.cigaretteGuidePanel.activeSelf&&paper.cigaretteColorPanel.activeSelf,"Color chart alone opens independently");AutoCloseAppraisalGuide.Postfix(-3);
        ModConfig.Instance.Entry.Set(false);AutoOpenAppraisalGuide.Postfix(genuine);Check(!paper.cigaretteColorPanel.activeSelf,"Guide toggle respected");ModConfig.Instance.Entry.Set(true);
        Console.WriteLine("PASS: 64 cigarette prerequisite/truth combinations, one valid reason, native gates, missing data, lifecycle restoration, injector switching and independent guide/color behavior.");
    }
}
