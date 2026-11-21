using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using XicesBetterExperience;

namespace UnityEngine
{
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point }
    public enum TextureWrapMode { Clamp }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a) { this.r=r;this.g=g;this.b=b;this.a=a; } }
    public struct Vector2 { public Vector2(float x, float y) { } }
    public struct Rect { public Rect(float x, float y, float w, float h) { } }
    public class Texture2D
    {
        public Color[] Pixels; public FilterMode filterMode; public TextureWrapMode wrapMode;
        public Texture2D(int x, int y, TextureFormat format, bool mip) { }
        public void SetPixels(Color[] colors) { Pixels=colors; }
        public void Apply(bool mip, bool unreadable) { }
    }
    public class Sprite
    {
        public string name; public Texture2D texture;
        public static Sprite Create(Texture2D texture, Rect r, Vector2 p, float unit) { return new Sprite { texture=texture }; }
    }
}
namespace XicesBetterExperience
{
    public static class ModEntry { public const string ModId="test"; public static bool IsEnabled=true; }
    public static class ExperienceSettings
    {
        public static int FreeStartCash=600;
        public static bool FreeStartStorage;
        public static bool FreeStartFood, FreeStartHousehold;
        public static bool FreeStartForceSkip, FreeStartNoSkipSupplies;
        public static bool FreeStartWaterTools, FreeStartWaterContact;
        public static bool FreeStartSmugglerStorage, FreeStartContraband, FreeStartBlackLamps, FreeStartExchange;
        public static bool FreeStartMusic; public static bool FreeStartHydroponics, FreeStartRanching, FreeStartAquaculture, FreeStartChemistry;
        public static bool FreeStartFarmerContact,FreeStartRancherContact,FreeStartAquafarmerContact,FreeStartChemistContact,FreeStartGunsmithContact,FreeStartGunPermit;
        public static int FreeStartBailouts;
        public static int FreeStartRent=200;
        public static bool DisableDisplayCaseTip=true;
        public static int[] Reputation=new int[5];
        public static int FreeStartReputation(int index) { return Reputation[index]; }
    }
    public static class FreeStartFundsPanel
    {
        public static int Commits, Builds, Hides;
        public static void CommitCurrent() { Commits++; }
        public static void HideCurrent() { Commits++; Hides++; }
        public static void Build(NewGameSpecializationFrame frame, CustomUIWindow window) { Builds++; }
    }
}
public static class ModHelper
{
    public static Dictionary<string,int> data=new Dictionary<string,int>();
    public static Dictionary<string,bool> flags=new Dictionary<string,bool>();
    public static bool GetModDataBool(string mod,string key,bool fallback) { bool value; return flags.TryGetValue(key,out value)?value:fallback; }
    public static void SetModDataBool(string mod,string key,bool value) { flags[key]=value; }
    public static string GetLocalized(string mod,string key) { return key; }
    public static void SetModDataInt(string mod,string key,int value) { data[key]=value; }
    public static int GetModDataInt(string mod,string key,int fallback) { int value;return data.TryGetValue(key,out value)?value:fallback; }
}
public class NewGameData
{
    public enum StartType { NONE,APPRENTICE,MODDED,FENCE }
    public static NewGameData Instance=new NewGameData();
    public StartType startType; public string startScenarioId; public bool skipIntro;
    public static void HardReset() { }
    public static void HandleInitialItem()
    {
        if(!StartingPerk.IsTapehead())
            foreach(string id in new[]{"cassette3d","cassette4d","cassette5d","cassette6d"})
                GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement,DirectoryMaster.Item(id));
    }
}
public static class StartingPerk { public static bool Tapehead; public static bool IsTapehead() { return Tapehead; } }
public static class StoreReputation
{
    public static List<string> Calls=new List<string>();
    public static List<int> Values=new List<int>();
    public static void ModReputation(string faction,int amount) { Calls.Add(faction);Values.Add(amount); }
}
public class StartingScenario
{
    public enum TagTone { Positive }
    public string id,source;
    public NewGameData.StartType startType;
    public Func<string> titleProvider,descriptionProvider,tagProvider;
    public TagTone tagTone;
    public bool skipIntro;
    public Action onStart;
    public static List<StartingScenario> list=new List<StartingScenario>();
    public static void Register(StartingScenario scenario) { list.RemoveAll(s=>s.id==scenario.id);list.Add(scenario); }
    public static int RemoveMod(string id) { return list.RemoveAll(s=>s.source==id); }
    public static List<StartingScenario> GetVisible() { return new List<StartingScenario>(list); }
    public static StartingScenario Get(string id) { return list.Find(s=>s.id==id); }
    public static string ResolveId(NewGameData.StartType type,string id) { return id; }
}
public class NewGameSpecializationFrame { }
public class CustomUIWindow { }
public static class NewGameSpecializationUI
{
    private static List<StartingScenario> scenarios;
    private static NewGameSpecializationFrame frame=new NewGameSpecializationFrame();
    private static CustomUIWindow window=new CustomUIWindow();
    public static StartingScenario Selected;
    public static int NativeIndex=-1;
    public static void Setup() { scenarios=StartingScenario.GetVisible(); }
    public static int IndexOfCurrentSelection() { return NativeIndex; }
    public static UnityEngine.Sprite ResolveIcon(StartingScenario scenario) { return new UnityEngine.Sprite(); }
    public static void ShowScenario() { GC.KeepAlive(frame);GC.KeepAlive(window); }
}
public class IconProvider
{
    public UnityEngine.Sprite GetIcon(string scenarioId) { return null; }
}
public class MainMenuUIController { public void OnScenarioContinue(StartingScenario scenario) { NewGameData.Instance.startType=scenario.startType; NewGameData.Instance.startScenarioId=scenario.id; } }
public static class NewGameDifficultyUI
{
    private static bool skipIntroForced;
    private static bool cached;
    public static bool Locked { get { return skipIntroForced; } }
    public static void Open()
    {
        if(cached) return;
        cached=true;
        skipIntroForced=StartingScenario.Get(NewGameData.Instance.startScenarioId).skipIntro;
        if(skipIntroForced) NewGameData.Instance.skipIntro=true;
    }
}
public class PlayerStore
{
    public int PermitsGranted;
    public int Tips;
    public void ShowDisplayCaseTimingTipOnce() { Tips++; }
    public ExchangeManager exchangeManager=new ExchangeManager();
    public StoreClientData storeClientData=new StoreClientData();
    public Dictionary<string,NetworkUpgrade> networkUpgrade=new Dictionary<string,NetworkUpgrade> {
        { "WATER_TRADER",new NetworkUpgrade() }, { "RETIRED_FARMER",new NetworkUpgrade() }, { "RETIRED_RANCHER",new NetworkUpgrade() },
        { "AQUACULTURE",new NetworkUpgrade() }, { "RETIRED_CHEMIST",new NetworkUpgrade() }, { "RETIRED_GUNSMITH",new NetworkUpgrade() }, { "GUN_PERMIT_I",new NetworkUpgrade() }
    };
    public void AddDirectSellingItemToTable(GameItem item) { EmporiumEntry.Instance.invElement.Add(item); }
    public static PlayerStore Instance;
    public static bool IsInstanceExist() { return Instance!=null; }
    public NewGameData.StartType startType; public string startScenarioId;
    public int playerCash,SavedCash,Saves;
    public int startingRent=200,rentValue=200;
    public bool skipIntro,Introduced;
    public void HandleSkipIntro()
    {
        skipIntro=NewGameData.Instance.skipIntro;
        if(!skipIntro) return;
        Introduced=true;
        var book=DirectoryMaster.Item("tutorial_book");
        GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement,book);
        GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement,DirectoryMaster.Item("magnifier"));
        GraphUtils.EmporiumTryAdd(EmporiumEntry.Instance.invElement,DirectoryMaster.Item("intel_sec"));
        StoreStation.Instance.AddEvents();
    }
    public void InitialSave() { SavedCash=playerCash;Saves++; }
}
public class StoreStation
{
    private static StoreStation instance=new StoreStation();
    public static StoreStation Instance { get { return instance; } }
    public int Events,dayCounter=1;
    public void AddEvents() { Events++; }
}
public class NetworkUpgrade { public int state=1; }
public class ExchangeManager { public bool isExchangeUnlocked; }
public class StoreClientData { public bool isSawyerDead; }
public class StoreClient { public string identifier; }
public class StoreClientManager
{
    public void HandleSkipClient() { }
    public void AddClient(StoreClient client) { clientStack.Add(client); }
    public bool wildBailoutGiven,ConditionsMet=true,ReminderEnabled;
    public int WealthChecks;
    public void HandleWildBailout()
    {
        if(!wildBailoutGiven&&Difficulty.Rules.wildBailout)
        {
            WealthChecks++;
            if(ConditionsMet) { clientStack.Add(new StoreClient { identifier="wild_bailout" });wildBailoutGiven=true; }
        }
    }
    public void HandleContentUnlockClient() { ReminderEnabled=Difficulty.Rules.wildReminders; }
    public List<StoreClient> clientStack=new List<StoreClient>();
    public void GenerateClient() { clientStack.Add(new StoreClient { identifier="ordinary" }); }
    public void AddNextClient(StoreClient client) { clientStack.Insert(0,client); }
}
public static class StoreClientListGun
{
    public static StoreClient CreateLLGunBuyer() { return new StoreClient { identifier="ll_gun_buyer" }; }
    public static StoreClient CreateLLGunSeller() { return new StoreClient { identifier="ll_gun_seller" }; }
}
public static class PermitHelper { public static void GrantedFromPermit(GameItem item) { PlayerStore.Instance.PermitsGranted++; } }
public class DifficultyRules { public bool wildBailout=true,wildReminders=true; }
public static class Difficulty { public static DifficultyRules Rules=new DifficultyRules(); }
public static class StoreClientListStory
{
    public static StoreClient CreateJanitorUndergroundExchangeIntroductionFence() { return new StoreClient { identifier="janitorUEIntroductionFence" }; }
}
public static class ItemFeatureList { public enum EquipmentCondition { Fair } }
public class GameItem { public string Id,Water,name,Sex; public int Age,Uses; public bool Stuck; public bool Shabby; public int Durability; public ItemFeatureList.EquipmentCondition Condition; public List<GameItem> Contents=new List<GameItem>(); }
public static class WaterHelper
{
    public static void FillWithGhostwater(GameItem item) { item.Water="ghost"; }
    public static void FillWithRustWaterLowEnd(GameItem item) { item.Water="rust_low"; }
}
public static class DurabilityHelper { public static void SetCurrentDurability(GameItem item,int value) { item.Durability=value; } }
public static class PreBuiltItemHelper
{
    public static GameItem CreateOldHydroponic1() { return DirectoryMaster.Item("old_hydroponic"); }
    public static GameItem CreateHydroponic() { return DirectoryMaster.Item("hydroponic"); }
    public static GameItem FullNutrientTabletBox() { return DirectoryMaster.Item("nutrient_box"); }
    public static GameItem CreateRat(string sex,int age,bool defaultStats) { return new GameItem { Id="rat",Sex=sex,Age=age }; }
    public static GameItem CreateNutrifruitSmall() { return DirectoryMaster.Item("nutrifruit_small"); }
    public static GameItem CreateEvaporator(int performance) { return new GameItem { Id="evaporator",Durability=performance }; }
    public static GameItem CreateHeavyHandmadeGun(ItemFeatureList.EquipmentCondition condition) { return new GameItem { Id="heavy_handmade_pistol",Condition=condition }; }
    public static GameItem FullWaterTestStripBox() { return DirectoryMaster.Item("full_test_strip_box"); }
    public static GameItem FullWaterPurificationTabletBox() { return DirectoryMaster.Item("full_tablet_box"); }
    public static GameItem WaterFilter() { return DirectoryMaster.Item("water_filter"); }
    public static GameItem EnergyCredit(int amount) { return DirectoryMaster.Item("energy_"+amount); }
}
public static class ModuleHelper { public static void SetModuleStuck(GameItem item) { item.Stuck=true; } }
public static class UseCountHelper { public static void SetCurrentUseCount(GameItem item,int count) { item.Uses=count; } }
public static class ItemSpawner { public static GameItem Spawn(string id) { return DirectoryMaster.Item(id); } }
public static class AnimalCage
{
    public static GameItem SpawnCageWithDispenser(bool shabby) { var item=DirectoryMaster.Item("cage");item.Shabby=shabby;item.Contents.Add(DirectoryMaster.Item("dispenser"));return item; }
}
public class FishSpecies
{
    public static FishSpecies Tilapia=new FishSpecies();public int[] stageStartDays={0,2,4,6,8};
}
public static class AquacultureItems
{
    public static GameItem SpawnFish(FishSpecies species,string sex,int age) { return new GameItem { Id="tilapia",Sex=sex,Age=age }; }
    public static GameItem SpawnFertilizedRoe(FishSpecies species) { return DirectoryMaster.Item("tilapia_roe"); }
}
public class BarterOffer { public Action onInstance; public string Conditions="FOOD"; }
public static class BarterOfferList
{
    public static BarterOffer CreateBarterFoodTutorial() { return new BarterOffer { onInstance=()=>{
        TutorialUIManager.Instance.OnBarterTutorialStart();PlayerStore.Instance.AddDirectSellingItemToTable(PreBuiltItemHelper.EnergyCredit(33));
    } }; }
}
public class TutorialUIManager
{
    public static TutorialUIManager Instance=new TutorialUIManager(); public int Starts;
    public void OnBarterTutorialStart() { Starts++; }
}
public class EmporiumEntry { public static EmporiumEntry Instance=new EmporiumEntry(); public List<GameItem> invElement=new List<GameItem>(); public List<GameItem> showcaseElement=new List<GameItem>(); }
public static class DirectoryMaster { public static GameItem Item(string id) { return new GameItem { Id=id }; } }
public static class ItemFeatureHelper { public static GameItem SetShabby(GameItem item) { item.Shabby=true;return item; } }
public static class GraphUtils
{
    public static void EmporiumTryAdd(List<GameItem> inventory,GameItem item) { inventory.Add(item); }
    public static int TryAcceptAll(GameItem parent,GameItem child) { parent.Contents.Add(child);return 1; }
}
public static class FreeStartRegression
{
    static int count;
    static void Check(bool value,string message) { count++;if(!value)throw new Exception(message); }
    static void Main()
    {
        new Harmony("free-start-test").PatchAll(typeof(FreeStartScenario).Assembly);
        foreach(int v in new[]{-200,-199,-1,0,1,199,200}) Check(FreeStartRules.ParseReputation(v.ToString(),17)==v,"Signed reputation input");
        Check(FreeStartRules.ParseReputation(new string('9',1000),17)==200&&FreeStartRules.ParseReputation("-"+new string('9',1000),17)==-200,"Reputation overflow safely clamps both ways");
        foreach(string v in new[]{null,"","-","+","bad","1.2","1e2"}) Check(FreeStartRules.ParseReputation(v,17)==17,"Invalid or incomplete reputation restores previous");
        foreach(int value in new[]{10,200,1999,2000}) Check(FreeStartRules.ParseRange(value.ToString(),200,10,2000)==value,"Rent integer input");
        Check(FreeStartRules.ParseRange("9",200,10,2000)==10&&FreeStartRules.ParseRange("2001",200,10,2000)==2000,"Rent boundaries");
        Check(FreeStartRules.ParseRange(new string('9',1000),200,10,2000)==2000&&FreeStartRules.ParseRange("-20",200,10,2000)==10,"Rent huge or negative input safely clamps");
        Check(FreeStartRules.ParseRange("-",200,10,2000)==200&&FreeStartRules.ParseRange("bad",200,10,2000)==200,"Incomplete rent restores previous");
        Check(FreeStartRules.ParseRange("11",0,0,10)==10&&FreeStartRules.ParseRange("-1",5,0,10)==0&&FreeStartRules.ParseRange("0",5,0,10)==0,"Bailout input bounds include zero");
        foreach(int v in new[]{0,1,599,600,9999,10000}) Check(FreeStartRules.Parse(v.ToString(),600)==v,"Valid cash value");
        Check(FreeStartRules.Parse("-100",600)==0,"Negative clamped");
        Check(FreeStartRules.Parse("10001",600)==10000,"Upper clamped");
        Check(FreeStartRules.Parse(new string('9',1000),600)==10000,"Huge input cannot overflow");
        foreach(string v in new[]{null,""," ","-","+","3.5","1e3","bad","10x","1,000"})
            Check(FreeStartRules.Parse(v,721)==721,"Incomplete/invalid input restores previous");
        Check(FreeStartRules.Parse(" +0024 ",600)==24,"Signed/leading-zero input normalized");

        var apprentice=new StartingScenario { id="apprentice",startType=NewGameData.StartType.APPRENTICE };
        var water=new StartingScenario { id="water",startType=NewGameData.StartType.FENCE };
        var other=new StartingScenario { id="other",source="other_mod",startType=NewGameData.StartType.MODDED };
        StartingScenario.list.AddRange(new[]{apprentice,water,other});
        FreeStartScenario.Register();FreeStartScenario.Register();
        Check(StartingScenario.list.Count==4,"Registration idempotent");
        var custom=StartingScenario.list.Single(s=>s.id==FreeStartScenario.Id);
        Check(custom.startType==NewGameData.StartType.MODDED&&!custom.skipIntro&&custom.onStart!=null,"Native start with optional storage callback and no forced intro skip");
        Check(custom.titleProvider()=="free_start_title"&&custom.descriptionProvider()=="free_start_description"&&custom.tagProvider()=="free_start_tag","Lazy localized title description and tag");
        var visible=StartingScenario.GetVisible();
        Check(visible[0]==custom&&visible[1]==apprentice&&visible[2]==water&&visible[3]==other,"Custom first and remaining order preserved");
        Check(StartingScenario.list[0]==apprentice,"Only returned list reordered");
        NewGameSpecializationUI.Setup();
        Check(NewGameSpecializationUI.IndexOfCurrentSelection()==1,"Initial default remains apprentice");
        NewGameSpecializationUI.NativeIndex=0;Check(NewGameSpecializationUI.IndexOfCurrentSelection()==0,"Explicit custom selection survives return");
        NewGameSpecializationUI.NativeIndex=3;Check(NewGameSpecializationUI.IndexOfCurrentSelection()==3,"Other selection survives return");
        NewGameSpecializationUI.Selected=custom;NewGameSpecializationUI.ShowScenario();
        Check(FreeStartFundsPanel.Builds==1,"Custom gets inline controls");
        NewGameSpecializationUI.Selected=apprentice;NewGameSpecializationUI.ShowScenario();
        Check(FreeStartFundsPanel.Builds==1&&FreeStartFundsPanel.Hides==2,"Switch commits/hides previous controls without modifying native details");

        var icon=NewGameSpecializationUI.ResolveIcon(custom);
        Check(icon==new IconProvider().GetIcon(custom.id),"Selection and save menu share cached icon");
        Check(icon.texture.filterMode==UnityEngine.FilterMode.Point&&icon.texture.Pixels[0].a==0,"Crisp transparent icon");
        Check(icon.texture.Pixels.Count(p=>p.a>0)>100&&icon.texture.Pixels.Count(p=>p.a>0)<400,"Pixel badge has bounded ring and question mark");

        var menu=new MainMenuUIController();
        ExperienceSettings.FreeStartCash=2345;menu.OnScenarioContinue(custom);
        ExperienceSettings.FreeStartCash=9999;
        var store=new PlayerStore {startType=NewGameData.StartType.MODDED,startScenarioId=custom.id,playerCash=12};
        store.InitialSave();Check(store.SavedCash==2345&&store.playerCash==2345,"Selection snapshot overrides final native/perk cash before first save");
        Check(ModHelper.data["free_start_cash"]==2345,"Chosen cash recorded in save metadata");
        menu.OnScenarioContinue(apprentice);
        var native=new PlayerStore {startType=NewGameData.StartType.APPRENTICE,playerCash=550};native.InitialSave();
        Check(native.SavedCash==550,"Native start unaffected");
        var otherStore=new PlayerStore {startType=NewGameData.StartType.MODDED,startScenarioId="other",playerCash=31};otherStore.InitialSave();
        Check(otherStore.SavedCash==31,"Other modded start unaffected");
        ExperienceSettings.FreeStartCash=0;menu.OnScenarioContinue(custom);store.InitialSave();Check(store.SavedCash==0,"Zero cash applied exactly");
        ExperienceSettings.FreeStartCash=10000;menu.OnScenarioContinue(custom);store.InitialSave();Check(store.SavedCash==10000,"Maximum cash applied exactly");
        ExperienceSettings.FreeStartCash=80;menu.OnScenarioContinue(custom);NewGameData.HardReset();ExperienceSettings.FreeStartCash=600;store.InitialSave();
        Check(store.SavedCash==600,"Menu reset clears stale selected snapshot");
        PlayerStore.Instance=store;
        ExperienceSettings.FreeStartStorage=false;menu.OnScenarioContinue(custom);custom.onStart();
        Check(EmporiumEntry.Instance.invElement.Count==0,"Storage disabled by default gives no item");
        ExperienceSettings.FreeStartStorage=true;menu.OnScenarioContinue(custom);
        ExperienceSettings.FreeStartStorage=false;custom.onStart();
        Check(EmporiumEntry.Instance.invElement.Count==1,"Selected storage snapshot survives setting changes");
        var storage=EmporiumEntry.Instance.invElement[0];
        Check(storage.Id=="storage_bay"&&storage.Shabby,"One shabby storage bay placed in starting inventory");
        Check(store.playerCash==600,"Storage does not deduct cash");
        custom.onStart();Check(EmporiumEntry.Instance.invElement.Count==1,"Repeated start callback cannot duplicate storage");
        store.InitialSave();
        ModHelper.flags.Clear();ExperienceSettings.FreeStartStorage=true;
        PlayerStore.Instance=native;custom.onStart();
        PlayerStore.Instance=otherStore;custom.onStart();
        Check(EmporiumEntry.Instance.invElement.Count==1,"Other professions receive no storage");
        PlayerStore.Instance=store;menu.OnScenarioContinue(custom);custom.onStart();
        Check(EmporiumEntry.Instance.invElement.Count==2,"New save can independently receive storage");
        ModHelper.flags.Clear();
        ExperienceSettings.FreeStartStorage=false;
        ExperienceSettings.FreeStartFood=true;ExperienceSettings.FreeStartHousehold=false;menu.OnScenarioContinue(custom);
        ExperienceSettings.FreeStartFood=false;custom.onStart();
        Check(EmporiumEntry.Instance.invElement.Count(i=>i.Id=="processed_juice")==1&&
            EmporiumEntry.Instance.invElement.Count(i=>i.Id=="processed_meat")==2,"Food snapshot grants exact items and quantities");
        Check(!EmporiumEntry.Instance.invElement.Any(i=>i.Id=="toothpaste"),"Household remains independent and off");
        custom.onStart();Check(EmporiumEntry.Instance.invElement.Count==5,"Food cannot be duplicated by repeated callback");
        ExperienceSettings.FreeStartHousehold=true;menu.OnScenarioContinue(custom);
        ExperienceSettings.FreeStartHousehold=false;custom.onStart();
        Check(EmporiumEntry.Instance.invElement.Count(i=>i.Id=="toothpaste")==2&&
            EmporiumEntry.Instance.invElement.Count(i=>i.Id=="toilet_paper")==2,"Household snapshot grants exact items and quantities");
        Check(EmporiumEntry.Instance.invElement.Where(i=>i.Id!="storage_bay").All(i=>!i.Shabby),"Food and household items are not shabby");
        custom.onStart();Check(EmporiumEntry.Instance.invElement.Count==9,"Household cannot be duplicated");
        Check(store.playerCash==600,"Optional supplies do not deduct starting funds");
        EmporiumEntry.Instance.invElement.Clear();
        ExperienceSettings.FreeStartForceSkip=true;ExperienceSettings.FreeStartNoSkipSupplies=false;
        menu.OnScenarioContinue(custom);NewGameDifficultyUI.Open();
        Check(NewGameData.Instance.skipIntro&&NewGameDifficultyUI.Locked,"Force skip checks and locks native difficulty option");
        store.HandleSkipIntro();
        Check(store.Introduced&&StoreStation.Instance.Events==1&&EmporiumEntry.Instance.invElement.Count==3,"Force skip preserves native supplies, unlocks and events");
        ExperienceSettings.FreeStartForceSkip=false;ExperienceSettings.FreeStartNoSkipSupplies=true;
        menu.OnScenarioContinue(custom);NewGameDifficultyUI.Open();
        Check(!NewGameDifficultyUI.Locked,"Returning with force off unlocks cached difficulty checkbox");
        ExperienceSettings.FreeStartNoSkipSupplies=false;
        EmporiumEntry.Instance.invElement.Clear();store.Introduced=false;NewGameData.Instance.skipIntro=true;
        store.HandleSkipIntro();
        Check(store.Introduced&&StoreStation.Instance.Events==2&&EmporiumEntry.Instance.invElement.Count==0,"Manual skip with captured suppression keeps unlocks/events but no supplies");
        ExperienceSettings.FreeStartForceSkip=true;ExperienceSettings.FreeStartNoSkipSupplies=true;menu.OnScenarioContinue(custom);
        NewGameDifficultyUI.Open();store.HandleSkipIntro();
        Check(NewGameDifficultyUI.Locked&&EmporiumEntry.Instance.invElement.Count==0&&StoreStation.Instance.Events==3,"Forced skip also respects supply suppression");
        ModHelper.flags.Clear();ExperienceSettings.FreeStartStorage=true;ExperienceSettings.FreeStartFood=true;
        menu.OnScenarioContinue(custom);custom.onStart();
        Check(EmporiumEntry.Instance.invElement.Count==4,"Suppression preserves explicitly selected storage and food");
        EmporiumEntry.Instance.invElement.Clear();NewGameData.Instance.skipIntro=false;store.Introduced=false;store.HandleSkipIntro();
        Check(!store.Introduced&&EmporiumEntry.Instance.invElement.Count==0&&StoreStation.Instance.Events==3,"Without skip neither compensation nor tutorial unlocks run");
        PlayerStore.Instance=native;menu.OnScenarioContinue(apprentice);NewGameDifficultyUI.Open();NewGameData.Instance.skipIntro=true;native.HandleSkipIntro();
        Check(!NewGameDifficultyUI.Locked&&EmporiumEntry.Instance.invElement.Count==3,"Other professions keep native skip supplies and no stale force lock");
        PlayerStore.Instance=store;menu.OnScenarioContinue(custom);
        EmporiumEntry.Instance.invElement.Clear();
        ModHelper.flags.Clear();ExperienceSettings.FreeStartStorage=false;ExperienceSettings.FreeStartFood=false;
        ExperienceSettings.FreeStartWaterTools=true;ExperienceSettings.FreeStartWaterContact=false;
        store.skipIntro=false;menu.OnScenarioContinue(custom);ExperienceSettings.FreeStartWaterTools=false;custom.onStart();
        var tools=EmporiumEntry.Instance.invElement;
        Check(tools.Count==12,"Water tools snapshot grants all twelve native starting items");
        foreach(string id in new[]{"water_guide","full_test_strip_box","full_tablet_box","water_jug","bottled_water","small_bottled_water","water_pitcher","water_filter","portable_water_purifier","bottle_printer"})
            Check(tools.Count(i=>i.Id==id)==1,"Exact water-merchant item: "+id);
        Check(tools.Count(i=>i.Id=="large_bottled_water")==2&&tools.Count(i=>i.Id=="large_bottled_water"&&i.Water=="ghost")==1,"One filled and one empty large bottle");
        Check(tools.Single(i=>i.Id=="water_jug").Water=="rust_low","Native low-end rusty water in jug");
        Check(tools.Single(i=>i.Id=="portable_water_purifier").Shabby&&tools.Single(i=>i.Id=="portable_water_purifier").Durability==75,"Native shabby purifier at 75 durability");
        var printer=tools.Single(i=>i.Id=="bottle_printer");
        Check(printer.Shabby&&printer.Contents.Single().Id=="energy_25","Native shabby printer with preinstalled 25 energy");
        Check(store.playerCash==600&&store.networkUpgrade["WATER_TRADER"].state==1,"Water tools do not deduct cash or unlock contacts");
        custom.onStart();Check(tools.Count==12,"Water tools cannot duplicate");
        var offer=BarterOfferList.CreateBarterFoodTutorial();offer.onInstance();
        Check(tools.Last().Id=="energy_33","Tools alone retain original food tutorial reward");
        tools.Clear();ModHelper.flags.Clear();ExperienceSettings.FreeStartWaterTools=true;store.skipIntro=true;
        ExperienceSettings.FreeStartNoSkipSupplies=false;menu.OnScenarioContinue(custom);custom.onStart();
        Check(tools.Count==13&&tools.Count(i=>i.Id=="sign_water")==1,"Skipped water-tools start includes native water sign");
        tools.Clear();ModHelper.flags.Clear();ExperienceSettings.FreeStartNoSkipSupplies=true;menu.OnScenarioContinue(custom);custom.onStart();
        Check(tools.Count==12&&!tools.Any(i=>i.Id=="sign_water"),"Supply suppression excludes water sign but retains chosen equipment");
        tools.Clear();ModHelper.flags.Clear();ExperienceSettings.FreeStartWaterTools=false;ExperienceSettings.FreeStartWaterContact=true;
        menu.OnScenarioContinue(custom);ExperienceSettings.FreeStartWaterContact=false;custom.onStart();
        Check(store.networkUpgrade["WATER_TRADER"].state==2&&tools.Count==0,"Contact snapshot independently unlocks native water-trader upgrade without equipment");
        store.InitialSave();
        int tutorialStarts=TutorialUIManager.Instance.Starts;
        offer=BarterOfferList.CreateBarterFoodTutorial();offer.onInstance();
        Check(offer.Conditions=="FOOD"&&TutorialUIManager.Instance.Starts==tutorialStarts+1&&tools.Single().Id=="full_tablet_box","Saved contact choice changes only food-tutorial reward and preserves tutorial start");
        tools.Clear();PlayerStore.Instance=native;offer.onInstance();
        Check(tools.Single().Id=="energy_33","Native profession ignores custom save marker");
        tools.Clear();PlayerStore.Instance=store;ModHelper.flags.Clear();offer.onInstance();
        Check(tools.Single().Id=="energy_33","Different save without chosen contact keeps original reward");
        tools.Clear();ModHelper.flags["free_start_water_contact"]=true;
        ExperienceSettings.FreeStartSmugglerStorage=true;ExperienceSettings.FreeStartContraband=true;
        ExperienceSettings.FreeStartBlackLamps=true;ExperienceSettings.FreeStartExchange=true;
        menu.OnScenarioContinue(custom);
        ExperienceSettings.FreeStartSmugglerStorage=false;ExperienceSettings.FreeStartContraband=false;
        ExperienceSettings.FreeStartBlackLamps=false;ExperienceSettings.FreeStartExchange=false;
        custom.onStart();
        Check(tools.Count==11,"Fence item snapshots grant exact total without cash or extra supplies");
        Check(tools.Single(i=>i.Id=="smuggler_bay").Shabby,"Starting smuggler bay is shabby");
        Check(tools.Count(i=>i.Id=="black_lamp"&&i.Shabby)==2,"Two starting lamps are shabby");
        Check(tools.Count(i=>i.Id=="dream_dust")==2&&tools.Count(i=>i.Id=="heavy_pistol_ammo")==2,"Exact dream dust and ten-millimeter ammo quantities");
        foreach(string id in new[]{"med_keycard","eng_keycard","sec_keycard"})
            Check(tools.Count(i=>i.Id==id)==1,"One disposable keycard per department: "+id);
        Check(tools.Single(i=>i.Id=="heavy_handmade_pistol").Condition==ItemFeatureList.EquipmentCondition.Fair,"Native Ironcast 10 pistol has Fair condition");
        Check(store.exchangeManager.isExchangeUnlocked&&ModHelper.flags["free_start_exchange"],"Selected exchange access unlocked and saved independently");
        Check(store.playerCash==600,"Fence items and access do not charge native fence fee");
        custom.onStart();Check(tools.Count==11,"Fence item callbacks do not duplicate goods");
        var clients=new StoreClientManager();
        clients.clientStack.Add(new StoreClient { identifier="janitorUEIntroductionFence" });
        clients.clientStack.Add(new StoreClient { identifier="janitorUEIntroduction" });
        store.skipIntro=false;clients.GenerateClient();
        Check(clients.clientStack[0].identifier=="janitorUEIntroductionFence"&&clients.clientStack.Count(c=>c.identifier=="janitorUEIntroductionFence")==1&&
            !clients.clientStack.Any(c=>c.identifier=="janitorUEIntroduction"),"First-day normal tutorial gets exactly one native fence introduction in first position");
        clients.GenerateClient();Check(clients.clientStack.Count(c=>c.identifier=="janitorUEIntroductionFence")==1,"Repeated generation does not repeat introduction");
        ModHelper.flags.Remove("free_start_exchange_intro_queued");clients=new StoreClientManager();store.skipIntro=true;clients.GenerateClient();
        Check(clients.clientStack[0].identifier=="janitorUEIntroductionFence","Skipped intro also gets fence introduction first");
        ModHelper.flags.Remove("free_start_exchange_intro_queued");clients=new StoreClientManager();StoreStation.Instance.dayCounter=2;clients.GenerateClient();
        Check(clients.clientStack.Count==1,"No first-day introduction on later days");
        StoreStation.Instance.dayCounter=1;store.storeClientData.isSawyerDead=true;clients=new StoreClientManager();clients.GenerateClient();
        Check(clients.clientStack.Count==1,"Dead Sawyer is not resurrected by custom start");
        store.storeClientData.isSawyerDead=false;PlayerStore.Instance=native;clients=new StoreClientManager();clients.GenerateClient();
        Check(clients.clientStack.Count==1,"Other professions ignore custom exchange marker");
        PlayerStore.Instance=store;ModHelper.flags.Remove("free_start_exchange");clients=new StoreClientManager();clients.GenerateClient();
        Check(clients.clientStack.Count==1,"A save without selected exchange access gets no extra customer");
        tools.Clear();ModHelper.flags.Clear();store.exchangeManager.isExchangeUnlocked=false;
        ExperienceSettings.FreeStartSmugglerStorage=true;menu.OnScenarioContinue(custom);custom.onStart();
        Check(tools.Count==1&&!store.exchangeManager.isExchangeUnlocked,"Smuggler storage remains independent of contraband lamps and exchange access");
        tools.Clear();ModHelper.flags.Clear();ExperienceSettings.FreeStartSmugglerStorage=false;
        ExperienceSettings.FreeStartExchange=true;menu.OnScenarioContinue(custom);custom.onStart();
        Check(tools.Count==0&&store.exchangeManager.isExchangeUnlocked,"Exchange access alone grants no initial items");
        ModHelper.flags.Remove("free_start_exchange_intro_queued");
        tools.Clear();ExperienceSettings.FreeStartMusic=false;menu.OnScenarioContinue(custom);NewGameData.HandleInitialItem();
        Check(tools.Count==0,"Custom start defaults to no standard music cassettes");
        ExperienceSettings.FreeStartMusic=true;menu.OnScenarioContinue(custom);ExperienceSettings.FreeStartMusic=false;NewGameData.HandleInitialItem();
        Check(tools.Select(i=>i.Id).SequenceEqual(new[]{"cassette3d","cassette4d","cassette5d","cassette6d"}),"Selected music snapshot retains native four cassettes");
        tools.Clear();StartingPerk.Tapehead=true;NewGameData.HandleInitialItem();
        Check(tools.Count==0,"Enabling music still respects native Tapehead restriction");
        StartingPerk.Tapehead=false;PlayerStore.Instance=native;NewGameData.HandleInitialItem();
        Check(tools.Count==4,"Native professions retain standard music");tools.Clear();PlayerStore.Instance=store;
        ModHelper.flags.Remove("free_start_reputation_applied");StoreReputation.Calls.Clear();StoreReputation.Values.Clear();
        ExperienceSettings.Reputation=new[]{-200,200,-17,0,33};menu.OnScenarioContinue(custom);
        ExperienceSettings.Reputation=new int[5];store.InitialSave();
        Check(StoreReputation.Calls.SequenceEqual(new[]{"FACTION_LOWER_LEVEL","FACTION_UPPER_LEVEL","FACTION_SECURITY","FACTION_REVOLUTION"})&&
            StoreReputation.Values.SequenceEqual(new[]{-200,200,-17,33}),"Captured reputation amounts sent unscaled to native adjustment API; zero skipped");
        store.InitialSave();Check(StoreReputation.Calls.Count==4,"Reputation adjustments cannot repeat on another save call");
        ModHelper.flags.Remove("free_start_reputation_applied");menu.OnScenarioContinue(custom);store.InitialSave();
        Check(StoreReputation.Calls.Count==4,"All-zero reputation makes no adjustment calls");
        ModHelper.flags.Remove("free_start_reputation_applied");ExperienceSettings.Reputation=new[]{1,2,3,4,5};menu.OnScenarioContinue(custom);
        NewGameData.HardReset();ExperienceSettings.Reputation=new int[5];store.InitialSave();
        Check(StoreReputation.Calls.Count==4,"Reset clears captured reputation values");
        ExperienceSettings.Reputation=new[]{1,2,3,4,5};menu.OnScenarioContinue(custom);
        TestEvents(custom,menu,store,native);
        ModHelper.flags.Remove("free_start_rent_applied");ExperienceSettings.FreeStartRent=1250;
        menu.OnScenarioContinue(custom);ExperienceSettings.FreeStartRent=200;store.rentValue=315;store.InitialSave();
        Check(store.startingRent==1250&&store.rentValue==1250,"Selected rent snapshot sets exact first-week rent before save");
        store.rentValue=1500;store.InitialSave();Check(store.rentValue==1500,"Later save cannot reset subsequent rent");
        ModHelper.flags.Remove("free_start_rent_applied");ExperienceSettings.FreeStartRent=2000;menu.OnScenarioContinue(custom);
        NewGameData.HardReset();ExperienceSettings.FreeStartRent=200;store.InitialSave();
        Check(store.startingRent==200&&store.rentValue==200,"Reset discards stale rent snapshot");
        menu.OnScenarioContinue(apprentice);native.rentValue=420;native.InitialSave();
        Check(native.rentValue==420,"Other professions keep their native rent");
        TestEquipment(custom,menu,store);
        TestContacts(custom,menu,store,native);
        ModEntry.IsEnabled=false;store.playerCash=72;store.InitialSave();Check(store.SavedCash==72,"Disabled mod does not change cash");
        Check(StoreReputation.Calls.Count==4,"Disabled mod does not adjust reputation");
        NewGameData.HandleInitialItem();Check(tools.Count==4,"Disabled mod retains native cassettes");tools.Clear();
        clients=new StoreClientManager();clients.GenerateClient();Check(clients.clientStack.Count==1,"Disabled mod adds no exchange introduction");
        offer.onInstance();Check(tools.Single().Id=="energy_33","Disabled mod retains original tutorial reward");tools.Clear();
        custom.onStart();Check(EmporiumEntry.Instance.invElement.Count==0,"Disabled mod does not grant items");
        store.HandleSkipIntro();Check(EmporiumEntry.Instance.invElement.Count==3,"Disabled mod leaves native skip supplies intact");
        Check(StartingScenario.GetVisible()[0]==apprentice,"Disabled mod leaves original ordering");
        FreeStartScenario.Remove();Check(StartingScenario.list.Count==3&&StartingScenario.list.Contains(other),"Disable removes only own scenario");
        Console.WriteLine("PASS: custom profession registration, ordering/default selection, inline panel scope, pixel icon, cash input and first-save snapshot ("+count+" checks).");
    }
    static void TestEvents(StartingScenario custom,MainMenuUIController menu,PlayerStore store,PlayerStore native)
    {
        var manager=new StoreClientManager();PlayerStore.Instance=store;
        ModHelper.flags.Remove("free_start_events_saved");
        ExperienceSettings.FreeStartBailouts=0;menu.OnScenarioContinue(custom);store.InitialSave();
        manager.HandleWildBailout();manager.HandleContentUnlockClient();
        Check(manager.clientStack.Count==0&&!manager.ReminderEnabled,"Zero limit disables custom bailouts and Wild reminders");
        ModHelper.flags.Remove("free_start_events_saved");
        ExperienceSettings.FreeStartBailouts=3;menu.OnScenarioContinue(custom);ExperienceSettings.FreeStartBailouts=0;store.InitialSave();
        Check(ModHelper.data["free_start_bailout_limit"]==3,"Bailout limit captured and saved separately from global choice");
        manager.ConditionsMet=false;StoreStation.Instance.dayCounter=8;manager.HandleWildBailout();
        Check(!ModHelper.data.ContainsKey("free_start_bailout_count"),"Failed native financial conditions do not consume quota");
        manager.ConditionsMet=true;manager.HandleWildBailout();
        Check(manager.clientStack.Count==1&&ModHelper.data["free_start_bailout_count"]==1,"Eligible bailout consumes one saved quota");
        manager.HandleWildBailout();StoreStation.Instance.dayCounter=17;manager.HandleWildBailout();
        Check(manager.clientStack.Count==1,"Same day and nine-day gap cannot repeat bailout");
        StoreStation.Instance.dayCounter=18;manager.HandleWildBailout();
        Check(manager.clientStack.Count==2&&ModHelper.data["free_start_bailout_count"]==2,"Second bailout allowed exactly ten days later");
        var reloaded=new StoreClientManager();StoreStation.Instance.dayCounter=27;reloaded.HandleWildBailout();
        Check(reloaded.clientStack.Count==0,"Saved interval survives manager recreation");
        StoreStation.Instance.dayCounter=28;reloaded.HandleWildBailout();StoreStation.Instance.dayCounter=38;reloaded.HandleWildBailout();
        Check(reloaded.clientStack.Count==1&&ModHelper.data["free_start_bailout_count"]==3,"Saved quota enforced across recreation");
        Difficulty.Rules.wildReminders=false;reloaded.HandleContentUnlockClient();
        Check(reloaded.ReminderEnabled,"Positive custom quota enables reminder branch");
        Difficulty.Rules.wildReminders=true;PlayerStore.Instance=native;
        var ordinary=new StoreClientManager();ordinary.HandleWildBailout();ordinary.HandleWildBailout();ordinary.HandleContentUnlockClient();
        Check(ordinary.clientStack.Count==1&&ordinary.ReminderEnabled,"Native professions retain one bailout and native reminders");
        PlayerStore.Instance=store;ModHelper.data["free_start_bailout_count"]=0;Difficulty.Rules.wildBailout=false;
        var disallowed=new StoreClientManager();disallowed.HandleWildBailout();
        Check(disallowed.clientStack.Count==0&&ModHelper.data["free_start_bailout_count"]==0,"Native difficulty restriction still applies to financial bailout");
        Difficulty.Rules.wildBailout=true;
        ExperienceSettings.DisableDisplayCaseTip=true;store.ShowDisplayCaseTimingTipOnce();native.ShowDisplayCaseTimingTipOnce();
        Check(store.Tips==0&&native.Tips==0,"Display timing popup blocked for every profession");
        ExperienceSettings.DisableDisplayCaseTip=false;store.ShowDisplayCaseTimingTipOnce();
        Check(store.Tips==1,"Turning off suppression restores native popup method");
        ExperienceSettings.DisableDisplayCaseTip=true;ModEntry.IsEnabled=false;store.ShowDisplayCaseTimingTipOnce();
        Check(store.Tips==2,"Disabled mod leaves native popup method intact");ModEntry.IsEnabled=true;
    }
    static void TestEquipment(StartingScenario custom,MainMenuUIController menu,PlayerStore store)
    {
        PlayerStore.Instance=store;
        var inventory=EmporiumEntry.Instance.invElement;
        int cash=store.playerCash,rent=store.rentValue;
        int[] totals={10,17,13,17};
        for(int choice=0;choice<4;choice++)
        {
            ModHelper.flags.Clear();inventory.Clear();
            ExperienceSettings.FreeStartHydroponics=choice==0;ExperienceSettings.FreeStartRanching=choice==1;
            ExperienceSettings.FreeStartAquaculture=choice==2;ExperienceSettings.FreeStartChemistry=choice==3;
            menu.OnScenarioContinue(custom);
            ExperienceSettings.FreeStartHydroponics=false;ExperienceSettings.FreeStartRanching=false;
            ExperienceSettings.FreeStartAquaculture=false;ExperienceSettings.FreeStartChemistry=false;
            FreeStartEquipment.Give();
            Check(inventory.Count==totals[choice],"Captured equipment selection grants exact native top-level quantity: "+choice);
            if(choice==0)
            {
                var hydro=inventory.Single(i=>i.Id=="hydroponic");
                Check(hydro.Shabby&&hydro.name=="Decrepit Hydroponic System"&&hydro.Contents.Count==6,"Hydroponic equipment retains shabby status, name and six installed items");
                Check(hydro.Contents.Count(i=>i.Id=="system_module_eco"&&i.Stuck)==2&&hydro.Contents.Count(i=>i.Id=="system_module_ruined"&&i.Stuck)==2,"Hydroponic modules preserve stuck state and types");
                Check(hydro.Contents.Any(i=>i.Id=="energy_100")&&hydro.Contents.Single(i=>i.Id=="large_bottled_water").Water=="ghost","Hydroponic native energy and water installed");
                Check(inventory.Count(i=>i.Id=="nutrifruit_seed")==2&&inventory.Count(i=>i.Id=="fillerweed_seed")==2&&inventory.Count(i=>i.Id=="water_ration_small")==2,"Native seeds and water rations");
            }
            if(choice==1)
            {
                Check(inventory.Single(i=>i.Id=="cage").Shabby&&inventory.Single(i=>i.Id=="cage").Contents.Single().Id=="dispenser","Native shabby cage with dispenser");
                foreach(int age in new[]{1,3,5}) foreach(string sex in new[]{"MALE","FEMALE"})
                    Check(inventory.Count(i=>i.Id=="rat"&&i.Age==age&&i.Sex==sex)==1,"Native rat sex and age pairing");
                Check(inventory.Single(i=>i.Id=="serum_green").Uses==2&&inventory.Count(i=>i.Id=="nutrifruit_small")==6,"Two-use serum and six native fruits");
            }
            if(choice==2)
            {
                Check(inventory.Count(i=>i.Id=="ras"&&i.Shabby&&i.Contents.Any(c=>c.Id=="ras_water_tank")&&i.Contents.Any(c=>c.Id=="energy_50"))==2,"Two shabby aquaculture systems with tanks and batteries");
                Check(inventory.Count(i=>i.Id=="tilapia"&&i.Age==FishSpecies.Tilapia.stageStartDays[4])==2&&inventory.Count(i=>i.Id=="tilapia"&&i.Sex=="MALE")==1&&inventory.Count(i=>i.Id=="tilapia"&&i.Sex=="FEMALE")==1,"Native mature fish pair");
                Check(inventory.Count(i=>i.Id=="tilapia_roe")==1&&inventory.Count(i=>i.Id=="water_jug"&&i.Water=="ghost")==2,"Fertilized roe and two filled water jugs");
            }
            if(choice==3)
            {
                foreach(string id in new[]{"blender","heating_plate","evaporator","chem_finisher","chem_scanner","smuggler_bay_mini"})
                    Check(inventory.Single(i=>i.Id==id).Shabby,"Native shabby chemistry item: "+id);
                Check(inventory.Single(i=>i.Id=="evaporator").Durability==40&&inventory.Count(i=>i.Id=="bottled_water")==2&&inventory.Count(i=>i.Id=="chemistry_guide")==1,"Native evaporator factory argument, water quantity and guide");
            }
            FreeStartEquipment.Give();Check(inventory.Count==totals[choice],"Equipment cannot be granted twice");
            Check(store.playerCash==cash&&store.rentValue==rent,"Equipment grants do not alter funds or rent");
        }
        inventory.Clear();ModHelper.flags.Clear();
        ExperienceSettings.FreeStartHydroponics=true;ExperienceSettings.FreeStartRanching=true;
        ExperienceSettings.FreeStartAquaculture=true;ExperienceSettings.FreeStartChemistry=true;
        menu.OnScenarioContinue(custom);FreeStartEquipment.Give();Check(inventory.Count==57,"All equipment choices combine independently");
        inventory.Clear();ModHelper.flags.Clear();NewGameData.HardReset();
        ExperienceSettings.FreeStartHydroponics=false;ExperienceSettings.FreeStartRanching=false;
        ExperienceSettings.FreeStartAquaculture=false;ExperienceSettings.FreeStartChemistry=false;
        FreeStartEquipment.Give();Check(inventory.Count==0,"Reset discards stale equipment selections");
    }
    static void TestContacts(StartingScenario custom,MainMenuUIController menu,PlayerStore store,PlayerStore native)
    {
        string[] ids={"RETIRED_FARMER","RETIRED_RANCHER","AQUACULTURE","RETIRED_CHEMIST","RETIRED_GUNSMITH","GUN_PERMIT_I"};
        PlayerStore.Instance=store;
        int cash=store.playerCash,rent=store.rentValue;
        for(int selection=0;selection<6;selection++)
        {
            ModHelper.flags.Clear();foreach(string id in ids) store.networkUpgrade[id].state=1;
            ExperienceSettings.FreeStartFarmerContact=selection==0;ExperienceSettings.FreeStartRancherContact=selection==1;
            ExperienceSettings.FreeStartAquafarmerContact=selection==2;ExperienceSettings.FreeStartChemistContact=selection==3;
            ExperienceSettings.FreeStartGunsmithContact=selection==4;ExperienceSettings.FreeStartGunPermit=selection==5;
            menu.OnScenarioContinue(custom);
            ExperienceSettings.FreeStartFarmerContact=false;ExperienceSettings.FreeStartRancherContact=false;
            ExperienceSettings.FreeStartAquafarmerContact=false;ExperienceSettings.FreeStartChemistContact=false;
            ExperienceSettings.FreeStartGunsmithContact=false;ExperienceSettings.FreeStartGunPermit=false;
            FreeStartContacts.Give();
            Check(ids.Count(id=>store.networkUpgrade[id].state==2)==1&&store.networkUpgrade[ids[selection]].state==2,"Captured contact independently unlocks exact native upgrade: "+ids[selection]);
            int permits=store.PermitsGranted;FreeStartContacts.Give();
            Check(store.PermitsGranted==permits,"Repeated contact grant cannot duplicate permit activation");
            Check(store.playerCash==cash&&store.rentValue==rent,"Contact grant does not alter funds or rent");
        }
        Check(store.PermitsGranted==1&&EmporiumEntry.Instance.showcaseElement.Single().Id=="permit_gun_1","Class I permit physically placed in display case and activated");
        var manager=new StoreClientManager();
        foreach(int day in new[]{1,2,4})
        {
            StoreStation.Instance.dayCounter=day;store.skipIntro=false;manager.HandleSkipClient();manager.HandleSkipClient();
        }
        Check(manager.clientStack.Select(c=>c.identifier).SequenceEqual(new[]{"ll_gun_buyer","ll_gun_seller","ll_gun_buyer"}),"Extra gun visitors on days 1, 2, 4 independently of intro mode, without duplicates");
        StoreStation.Instance.dayCounter=3;manager.HandleSkipClient();StoreStation.Instance.dayCounter=11;manager.HandleSkipClient();
        Check(manager.clientStack.Count==3,"Extra gun visitor schedule does not repeat weekly");
        StoreStation.Instance.dayCounter=1;PlayerStore.Instance=native;manager=new StoreClientManager();manager.HandleSkipClient();
        Check(manager.clientStack.Count==0,"Other professions ignore custom permit marker");
        PlayerStore.Instance=store;ModHelper.flags.Clear();manager.HandleSkipClient();
        Check(manager.clientStack.Count==0,"No selected permit means no early gun visitors");
        ExperienceSettings.FreeStartFarmerContact=true;ExperienceSettings.FreeStartRancherContact=true;
        ExperienceSettings.FreeStartAquafarmerContact=true;ExperienceSettings.FreeStartChemistContact=true;
        ExperienceSettings.FreeStartGunsmithContact=true;ExperienceSettings.FreeStartGunPermit=true;
        menu.OnScenarioContinue(custom);FreeStartContacts.Give();
        Check(ids.All(id=>store.networkUpgrade[id].state==2),"All six contact upgrades combine");
        ModHelper.flags.Clear();foreach(string id in ids) store.networkUpgrade[id].state=1;
        NewGameData.HardReset();ExperienceSettings.FreeStartFarmerContact=false;ExperienceSettings.FreeStartRancherContact=false;
        ExperienceSettings.FreeStartAquafarmerContact=false;ExperienceSettings.FreeStartChemistContact=false;
        ExperienceSettings.FreeStartGunsmithContact=false;ExperienceSettings.FreeStartGunPermit=false;FreeStartContacts.Give();
        Check(ids.All(id=>store.networkUpgrade[id].state==1),"Reset discards stale contact choices");
        ModHelper.flags["free_start_contact_GUN_PERMIT_I"]=true;StoreStation.Instance.dayCounter=2;
        ModEntry.IsEnabled=false;manager.HandleSkipClient();Check(manager.clientStack.Count==0,"Disabled mod adds no early gun visitors");ModEntry.IsEnabled=true;
    }
}
