using System;
using System.Collections.Generic;
using System.Linq;
using XicesBetterExperience;

// Engine boundaries only; forecasts and row formatting are compiled from production sources.
namespace HarmonyLib { [AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch : Attribute { public HarmonyPatch(Type t, string m) {} } }
namespace UnityEngine
{
    public class Component { public Transform transform; public T GetComponent<T>() where T : new() { return new T(); } public T[] GetComponentsInChildren<T>(bool all) { return new T[0]; } }
    public class MonoBehaviour : Component {}
    public class GameObject { public T GetComponent<T>() where T : new() { return new T(); } public T AddComponent<T>() where T : new() { return new T(); } }
    public class Transform : Component { public Transform parent; public int childCount; public Dictionary<string,Transform> children=new Dictionary<string,Transform>(); public GameObject gameObject = new GameObject(); public Transform Find(string name) { return children.ContainsKey(name)?children[name]:null; } public Transform GetChild(int i) { return null; } }
    public class RectTransform : Transform { public Vector2 sizeDelta, anchoredPosition; }
    public struct Vector2 { public float x,y; public Vector2(float x,float y) {this.x=x;this.y=y;} }
    public static class Mathf { public static float Max(float a,float b) {return Math.Max(a,b);} public static float Ceil(float v) {return (float)Math.Ceiling(v);} public static int RoundToInt(float v) {return (int)Math.Round(v);} }
    public static class Debug { public static void LogError(object error) { throw new Exception(error.ToString()); } }
}
namespace UnityEngine.UI { public class LayoutElement {public float minHeight,preferredHeight;} public static class LayoutRebuilder {public static void MarkLayoutForRebuild(UnityEngine.RectTransform t) {}} }
namespace TMPro { public class TextMeshProUGUI : UnityEngine.Component {public string text; public bool enableAutoSizing,enableWordWrapping; public float fontSize,preferredHeight=28; public UnityEngine.RectTransform rectTransform = new UnityEngine.RectTransform(); public UnityEngine.Vector2 GetPreferredValues(string t,float w,float h) {return new UnityEngine.Vector2(t.Length*8,preferredHeight);} } }
public static class TabletUI {public static string Credits(long value) {return value+"<sprite name=credits>";} }
public class EndOfDayUIManager {public static EndOfDayUIManager Instance; public bool IsOpen; public UnityEngine.GameObject UI; public void RefreshUI() {} }
public class EndOfDayTabletFrame {public UnityEngine.RectTransform content = new UnityEngine.RectTransform();}
public static class ModEntry {public const string ModId="test"; public static bool IsEnabled=true;}
public static class ExperienceSettings {public static bool NightServiceInformation=true;}
public static class ModHelper {public static string GetLocalized(string id,string key) {return key;} }
public class GameInventory {public GameItem parent,childItem; public List<GameItem> childItems=new List<GameItem>(); public GameItem GetParentItem() {return parent;} }
public class GameItem
{
    public string id; public GameInventory parentInventory, battery=new GameInventory(), modules=new GameInventory();
    public int value=10,rate=60,energy=10; public bool converter,on=true,hazardous;
    public HashSet<string> tags=new HashSet<string>();
    public GameItem(string id) {this.id=id;}
    public bool IsID(string s) {return id==s;} public bool IsTag(string s) {return tags.Contains(s);}
    public long GetCurrentValue(bool a,bool b,bool c,bool d,long e) {return value;}
    public TagState GetTagReadonly(string s) {return new TagState();}
}
public class TagState {public string GetString() {return "FACTION_SECURITY";} }
public class PlayerStore
{
    public static PlayerStore Instance=new PlayerStore(); public static bool IsInstanceExist() {return true;}
    public List<GameItem> owned=new List<GameItem>(); public List<GameItem> FindAllItem() {return owned;}
    public ExchangeManager exchangeManager=new ExchangeManager(); public SecData secData=new SecData();
    public bool skipIntro; public int startingDay;
    public List<StoreReputation> storeReputations=new List<StoreReputation>();
    public int vendingMislabelCount,vendingMislabelFraudValue,vendingMislabelEndangermentValue;
    public int requestMislabelCount,requestMislabelFraudValue,requestMislabelEndangermentValue;
    public double vendingMislabelRepLoss,fountainMislabelRepLossLL,fountainMislabelRepLossUL;
    public Dictionary<string,double> requestMislabelRepLoss=new Dictionary<string,double>();
}
public class EmporiumEntry {public static EmporiumEntry Instance=new EmporiumEntry(); public GameInventory trashcan=new GameInventory();}
public static class GraphUtils {public static List<T> FindAllChildrenType<T>(GameInventory inv) {return inv.childItems.Cast<T>().ToList();} }
public class ExchangeManager {public bool isExchangeUnlocked=true; public List<ExchangeBarter> activeBarter=new List<ExchangeBarter>(),promotionalBarter=new List<ExchangeBarter>(); public List<string> activePromotionalBarterIds=new List<string>();}
public class ExchangeBarter {public string identifier; public int priority,barterCounterPerNight=1; public List<BarterCondition> conditions=new List<BarterCondition>();}
public class BarterCondition {public string id; public bool IsItemValid(GameItem i) {return i.id==id;} }
public static class GeneralHelper {public static bool IsHazardousWaste(GameItem i) {return i.hazardous;} }
public static class MachineHelper {public static GameInventory GetBatterySlot(GameItem i) {return i.battery;} public static GameInventory GetModuleInv(GameItem i) {return i.modules;} }
public static class MachineAlarm {public static int GetAlarmeStopRate(GameItem i) {return i.rate;} }
public static class MachineryHelper {public static int GetMachinePowerUsage(GameItem i) {return 4;} }
public static class PowerHelper {public static bool CanDrawPowerSource(GameItem i,int a) {return i.energy>=a;} }
public static class CurrentConverterHelper {public static bool Blackout; public static bool IsCurrentConverter(GameItem i) {return i.converter;} public static bool IsOn(GameItem i) {return i.on;} public static bool IsBlackedOut() {return Blackout;} }
public class StoreService {public string id; public bool unlocked=true,isUsing=true; public bool IsActive() {return unlocked&&isUsing;} public static StoreService converter=new StoreService(),insurance=new StoreService(); public static StoreService GetCurrentConverterService() {return converter;} public static StoreService GetInsuranceService() {return insurance;} }
public class SecData {public List<string> crimeTypes=new List<string>(); public List<int> crimeAmount=new List<int>(); public int evidenceLevel; public int GetTotalCrimeValue() {return crimeAmount.Sum();} }
public static class NetworkUpgrade {public static int Tier=3; public static int GetWildFixerTier() {return Tier;} }
public class StoreReputation {public string factionId; public double amount; public List<Perk> perks=new List<Perk>(); public double GetReputationExact() {return amount;} public static bool Distrusted; public static bool IsPerkUnlocked(string p) {return p=="BM_DISTRUSTED"&&Distrusted;} }
public class Perk {public string perkId; public int perkThreshold; public bool isAlwaysActive;}
public static class StartingPerk {public static bool IsIntimidating() {return false;} public static bool IsBlabbermouth() {return false;} public static bool IsSycophant() {return false;} }
public static class Difficulty {public static Rule Rules=new Rule(); public class Rule {public float reputationGainMultiplier=1,reputationLossMultiplier=1;} }
public class StoreStation {public static StoreStation Instance=new StoreStation(); public int dayCounter=10;}

public static class NightServiceRegression
{
    static int checks;
    static void Check(bool ok,string message) {checks++; if(!ok) throw new Exception(message);}
    static string Describe(string id) {return new NightServiceInformation.Snapshot().Description(new StoreService {id=id});}
    static void Expect(string id,string text,string color) {string s=Describe(id); Check(s.Contains("<color=#"+color+">"+text+"</color>"),s);}
    static ExchangeBarter Offer(params string[] ids) {var o=new ExchangeBarter(); foreach(var id in ids) o.conditions.Add(new BarterCondition {id=id}); return o;}
    public static void Main()
    {
        foreach(int rate in new[]{-100,0,1,99,100,160})
        {
            Check(NightServiceRules.AlarmRate(rate,false)==0,"Unpowered alarm");
            Check(NightServiceRules.AlarmRate(rate,true)==Math.Max(0,Math.Min(100,rate)),"Clamp alarm");
        }
        Check(NightServiceRules.RemainingAfterFixer(new[]{60,60},1)==2,"Fixer applies allowance per crime, not total");
        Check(NightServiceRules.AfterFixer(59,1)==1,"Fixer truncates percent before subtraction");
        Check(NightServiceRules.AfterFixer(500,2)==250 && NightServiceRules.AfterFixer(500,3)==0,"Fixer tiers");
        int[] boundaries={99,100,299,300,849,850,1199,1200,1499,1500,2499,2500};
        int[] expected={0,2,2,6,6,9,9,12,12,20,20,33};
        for(int i=0;i<boundaries.Length;i++) Check(NightServiceRules.InvestigationIncrease(boundaries[i],false,false,false,false)==expected[i],"Investigation boundary "+boundaries[i]);
        Check(NightServiceRules.InvestigationIncrease(850,true,true,true,true)==9,"Perk order, priority overrides watchlist");
        Check(NightServiceRules.InvestigationIncrease(300,false,false,true,true)==10,"Round each perk stage");
        var crimes=new Dictionary<string,int>{{"FRAUD",100}};
        var pending=new Dictionary<string,int>();
        Check(NightServiceRules.ProjectInvestigation(crimes,pending,79,0,false,false,false,false)==79,"Decay below 80");
        Check(NightServiceRules.ProjectInvestigation(crimes,pending,80,0,false,false,false,false)==82,"No decay at 80");
        pending["FRAUD"]=100;
        Check(NightServiceRules.ProjectInvestigation(crimes,pending,98,3,false,false,false,false)==100,"Deferred crime is after fixer");
        Check(crimes["FRAUD"]==100 && pending["FRAUD"]==100,"Forecast is read-only");
        Check(NightServiceRules.InvestigationColor(100,100)==NightServiceRules.Red && NightServiceRules.InvestigationColor(100,99)==NightServiceRules.Orange && NightServiceRules.InvestigationColor(99,99)==NightServiceRules.Green,"Three investigation colors");

        var p=PlayerStore.Instance;
        Expect("INSURANCE_SERVICE","0%",NightServiceRules.Red);
        var alarm=new GameItem("security_alarm"); p.owned.Add(alarm);
        Expect("INSURANCE_SERVICE","0%",NightServiceRules.Red);
        var battery=new GameItem("battery"); alarm.battery.childItem=battery;
        Expect("INSURANCE_SERVICE","60%",NightServiceRules.Orange);
        foreach(int rate in new[]{-20,0,99,100,120}) {alarm.rate=rate; Expect("INSURANCE_SERVICE",Math.Max(0,Math.Min(100,rate))+"%",NightServiceRules.AlarmColor(rate));}
        battery.energy=3; Expect("INSURANCE_SERVICE","0%",NightServiceRules.Red);
        battery.converter=true; alarm.rate=60; Expect("INSURANCE_SERVICE","60%",NightServiceRules.Orange);
        StoreService.converter.isUsing=false; Expect("INSURANCE_SERVICE","0%",NightServiceRules.Red); StoreService.converter.isUsing=true;
        CurrentConverterHelper.Blackout=true; Expect("INSURANCE_SERVICE","0%",NightServiceRules.Red); CurrentConverterHelper.Blackout=false;
        battery.on=false; Expect("INSURANCE_SERVICE","0%",NightServiceRules.Red); battery.on=true;
        Check(battery.energy==3,"Reading alarm never draws energy");

        var trash=EmporiumEntry.Instance.trashcan.childItems;
        Check(Describe("JANITORIAL_SERVICE")=="","Empty trash");
        foreach(var id in new[]{"newspaper","slip","sec_slip","procurement_slip"}) trash.Add(new GameItem(id));
        trash.Add(new GameItem("worthless"){value=0});
        trash.Add(new GameItem("hazard"){hazardous=true});
        Check(Describe("JANITORIAL_SERVICE")=="","Receipts, worthless and hazardous excluded"); trash.Clear();
        var material=new GameItem("material"); trash.Add(material);
        Check(Describe("JANITORIAL_SERVICE").Contains(NightServiceRules.Red),"Valuable garbage warning");
        p.exchangeManager.activeBarter.Add(Offer("material"));
        Check(Describe("JANITORIAL_SERVICE")=="","Complete trade is excluded");
        p.exchangeManager.activeBarter[0].conditions.Add(new BarterCondition{id="missing"});
        Check(Describe("JANITORIAL_SERVICE")!="","Incomplete trade material will be cleaned");
        p.exchangeManager.activeBarter.Clear(); p.exchangeManager.activeBarter.Add(Offer("material"));
        trash.Add(new GameItem("material"));
        Check(Describe("JANITORIAL_SERVICE")!="","Extra material beyond one trade is warned");
        Check(trash.Count==2 && p.exchangeManager.activeBarter[0].barterCounterPerNight==1,"Forecast never removes items or consumes offers");
        trash.Clear(); p.exchangeManager.activeBarter.Clear();
        var bay=new GameItem("smuggler_bay_mod"); material.parentInventory=new GameInventory{parent=bay}; trash.Add(bay); trash.Add(material);
        Check(Describe("JANITORIAL_SERVICE")=="","Smuggler compartment and contents preserved");
        trash.Clear(); material.parentInventory=null;

        Check(Describe("WILD_FIXER")=="","No security dossier, no information");
        p.owned.Add(new GameItem("intel_sec")); p.secData.crimeTypes.Add("FRAUD"); p.secData.crimeAmount.Add(500); p.secData.evidenceLevel=98;
        string s=Describe("WILD_FIXER"); Check(s.Contains("<color=#"+NightServiceRules.Green+">500") && s.Contains("<color=#"+NightServiceRules.Orange+">98"),"Fixer prevents threshold");
        Check(s.Contains(TabletUI.Credits(500)) && !s.Contains("night_service_credits"),"Crime value uses native credit symbol formatter");
        p.secData.crimeAmount[0]=3000; Check(Describe("WILD_FIXER").Contains("<color=#"+NightServiceRules.Red+">98"),"Fixer cannot prevent threshold");
        p.secData.evidenceLevel=0; Check(Describe("WILD_FIXER").Contains("<color=#"+NightServiceRules.Green+">0"),"Safe without fixer");
        Check(p.secData.crimeAmount[0]==3000 && p.secData.evidenceLevel==0,"No changes to security record");

        var root=new UnityEngine.RectTransform {sizeDelta=new UnityEngine.Vector2(502,30),parent=new UnityEngine.RectTransform()};
        var visuals=new UnityEngine.RectTransform {sizeDelta=new UnityEngine.Vector2(502,30)};
        var label=new TMPro.TextMeshProUGUI {text="Insurance",fontSize=18,enableAutoSizing=true,preferredHeight=64,transform=new UnityEngine.Transform {parent=visuals}};
        label.rectTransform.sizeDelta=new UnityEngine.Vector2(360,28);
        var row=new NightServiceInfoRow {transform=root}; row.Capture(new StoreService(),label);
        row.Apply("(detail)");
        Check(label.text=="Insurance(detail)" && label.rectTransform.sizeDelta.x==312 && root.sizeDelta.y==66 && label.enableWordWrapping && !label.enableAutoSizing,"Multiline label expands row and reserves price column");
        row.Apply("(detail)"); Check(label.text=="Insurance(detail)","Repeated refresh is idempotent");
        row.Apply("(updated)"); Check(label.text=="Insurance(updated)","Refresh replaces previous details");
        row.Apply(""); Check(label.text=="Insurance" && label.rectTransform.sizeDelta.x==360 && root.sizeDelta.y==30 && label.enableAutoSizing && !label.enableWordWrapping,"Disabling restores original text and layout");
        var info=new UnityEngine.RectTransform {anchoredPosition=new UnityEngine.Vector2(110,-7)};
        visuals.children["Info"]=info; label.rectTransform.anchoredPosition=new UnityEngine.Vector2(32,0);
        row.Capture(new StoreService(),label); row.Apply("(detail)");
        Check(label.text=="Insurance<space=22>(detail)" && info.anchoredPosition.x==108 && info.anchoredPosition.y==-7,"Tooltip icon immediately follows measured service name, before details");
        row.Apply("(updated)"); Check(label.text=="Insurance<space=22>(updated)" && info.anchoredPosition.x==108,"Refresh neither moves tooltip nor duplicates spacing");
        row.Apply(""); Check(label.text=="Insurance" && info.anchoredPosition.x==110,"Disabling restores native tooltip position");
        Console.WriteLine("PASS: night service forecasts, inventory filtering, service power and read-only state ("+checks+" checks).");
    }
}
