using System;
using System.Collections.Generic;
using System.Reflection;
using XicesBetterExperience;

namespace UnityEngine
{
    public class GameObject { public T AddComponent<T>() where T:new() { return new T(); } }
    public class Transform
    {
        public GameObject gameObject = new GameObject();
        public Transform Find(string name) { return this; }
        public T GetComponent<T>() where T:new() { return new T(); }
    }
    public class CanvasGroup { public float alpha; }
}
public class WildNetworkSlot { public Action<string> onClick; public Action<string,bool> onHover; }
public class NewGameDifficultyFrame { public UnityEngine.Transform platesHost = new UnityEngine.Transform(); }
public class DifficultyRules
{
    public int retailMarkup = 15;
    public float rentIncreaseMultiplier = 1.05f;
    public bool landlordGraceOwnedAtStart = true;
    public int powerCostMultiplier=1, insuranceCostMultiplier=1;
    public int vendorPermitFee=1500, appraisalJoinCostMultiplier=1;
    public int inventorMarkup=15, retiredSupplierMarkup=15;
    public DifficultyRules Clone() { return (DifficultyRules)MemberwiseClone(); }
}
public class Difficulty
{
    public class DifficultyEffect { public string key; public Func<DifficultyRules,string> describe; }
    public static List<DifficultyEffect> Effects = new List<DifficultyEffect> {
        new DifficultyEffect { key="service_fees", describe=r=>"native service fees" },
        new DifficultyEffect { key="supplier_markup", describe=r=>"native suppliers" }
    };
    public string id, iconKey;
    public int order;
    public Func<string> title, subtitle;
    public DifficultyRules rules;
    public static Difficulty Normal = new Difficulty { id="normal", rules=new DifficultyRules() };
    public static List<string> GetEffectLines(Difficulty d) { return new List<string> { d.id, "rent="+d.rules.rentIncreaseMultiplier+";markup="+d.rules.retailMarkup, "native service fees", "permit="+d.rules.vendorPermitFee, "native suppliers" }; }
}
public static class ModHelper
{
    public static Difficulty Registered;
    public static Dictionary<string,int> Numbers = new Dictionary<string,int>();
    public static Dictionary<string,bool> Flags = new Dictionary<string,bool>();
    public static void SetModDataInt(string mod,string key,int value) { Numbers[key]=value; }
    public static int GetModDataInt(string mod,string key,int fallback) { int value; return Numbers.TryGetValue(key,out value) ? value : fallback; }
    public static void SetModDataBool(string mod,string key,bool value) { Flags[key]=value; }
    public static bool GetModDataBool(string mod,string key,bool fallback) { bool value; return Flags.TryGetValue(key,out value) ? value : fallback; }
    public static void AddDifficulty(string modId,string id,int order,Func<string> title,Func<string> subtitle,
        Action<DifficultyRules> configure=null,string basedOn="normal",Func<string>[] notes=null,string iconKey=null)
    {
        Registered = new Difficulty { id=id, order=order, title=title, subtitle=subtitle,
            rules=Difficulty.Normal.rules.Clone(), iconKey=iconKey };
    }
    public static int RemoveDifficulties(string id) { Registered=null; return 1; }
}
public class NewGameData
{
    public static NewGameData Instance = new NewGameData();
    public int startType;
    public string startScenarioId, difficultyId;
    public bool hardMode;
}
public static class StartingScenario { public static string ResolveId(int type,string id) { return id; } }
public static class StoreService { }
public static class ItemFeatureList { }
public class ItemFeature { public int Percent; public void SetPreExposedValueMod(int value) { Percent=value; } }
public class PlayerStore
{
    public static PlayerStore Instance;
    public static bool IsInstanceExist() { return Instance!=null; }
    public string difficultyId;
}
public static class NewGameDifficultyUI
{
    public static Difficulty Selected;
    public static int Closed;
    public static void CloseWithoutReturning() { Closed++; Selected=null; }
}
namespace XicesBetterExperience
{
    public static class ModEntry { public const string ModId="test"; public static bool IsEnabled=true; }
    public static class ExperienceSettings
    {
        public static int FreeStartRentGrowth=5, FreeStartRetailMarkup=15;
        public static int FreeStartPowerMultiplier=1, FreeStartInsuranceMultiplier=1;
        public static int FreeStartVendorPermitFee=1000, FreeStartAppraisalJoinMultiplier=1;
        public static int FreeStartRunnerMarkup=15, FreeStartHypertechMarkup=50, FreeStartDoctorMarkup=15, FreeStartRetiredMarkup=15;
        public static bool FreeStartLandlordGrace;
    }
    public static class FreeStartEvents { public static bool IsCustom() { return FreeStartDifficulty.IsFreeStart(); } }
    public static class FreeStartScenario
    {
        public const string Id="free_start";
        public static string Text(string key) { return key; }
    }
}
public static class FreeStartDifficultyRegression
{
    static int checks;
    static void Check(bool value) { checks++; if (!value) throw new Exception("Failed check "+checks); }
    static object Hook(string name, params object[] args)
    {
        return typeof(FreeStartDifficulty).GetNestedType(name,BindingFlags.NonPublic)
            .GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    }
    static object EconomyHook(string name,string method,params object[] args)
    {
        return typeof(FreeStartEconomy).GetNestedType(name,BindingFlags.NonPublic)
            .GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
    }
    static DifficultyRules Active(DifficultyRules source)
    {
        object[] args={source}; EconomyHook("ActiveRules","Postfix",args); return (DifficultyRules)args[0];
    }
    public static void Main()
    {
        FreeStartDifficulty.Register();
        Difficulty custom=ModHelper.Registered;
        Check(custom.id==FreeStartDifficulty.Id && custom.order==0);
        Check(custom.title()=="free_difficulty_title" && custom.subtitle()=="free_difficulty_subtitle");
        Check(custom.rules!=Difficulty.Normal.rules && custom.rules.retailMarkup==15 && custom.rules.rentIncreaseMultiplier==1.05f);
        object[] description={custom,null};
        Check(!(bool)Hook("Description",description));
        Check(((List<string>)description[1])[0]=="normal" && ((List<string>)description[1]).Count==5);
        foreach(bool free in new[]{false,true})
        {
            NewGameData.Instance.startScenarioId=free ? FreeStartScenario.Id : "apprentice";
            var choices=new List<Difficulty> { custom,Difficulty.Normal,new Difficulty { id="hard" },new Difficulty { id="other_mod" } };
            for(int i=0;i<choices.Count;i++)
            {
                bool allowed=free ? i==0 : i!=0;
                Check(FreeStartDifficulty.CanSelect(choices[i].id)==allowed);
                Check((bool)Hook("BlockSelection",i.ToString(),choices)==allowed);
                NewGameDifficultyUI.Selected=choices[i];
                Check((bool)Hook("ValidateStart")==allowed);
            }
        }
        NewGameData.Instance.startScenarioId=FreeStartScenario.Id;
        NewGameData.Instance.difficultyId="hard"; NewGameData.Instance.hardMode=true;
        NewGameDifficultyUI.Selected=new Difficulty { id="hard" };
        Hook("Selection");
        Check(NewGameData.Instance.difficultyId==custom.id && !NewGameData.Instance.hardMode);
        Check(NewGameDifficultyUI.Selected==null && NewGameDifficultyUI.Closed==1);
        NewGameDifficultyUI.Selected=custom;
        Hook("Selection"); Check(NewGameDifficultyUI.Closed==2);
        NewGameDifficultyUI.Selected=custom;
        NewGameData.Instance.startScenarioId="apprentice";
        Hook("Selection");
        Check(NewGameData.Instance.difficultyId=="normal" && NewGameDifficultyUI.Closed==3);
        NewGameData.Instance.difficultyId="hard"; NewGameData.Instance.hardMode=true;
        Hook("Selection"); Check(NewGameData.Instance.difficultyId=="hard" && NewGameData.Instance.hardMode);
        Check(!(bool)Hook("BlockSelection","bad",new List<Difficulty>{custom}));
        // New runs use chosen rules; changing menu defaults never rewrites an existing run.
        NewGameData.Instance.startScenarioId=FreeStartScenario.Id;
        NewGameData.Instance.difficultyId=custom.id;
        ExperienceSettings.FreeStartRentGrowth=20; ExperienceSettings.FreeStartRetailMarkup=35;
        ExperienceSettings.FreeStartLandlordGrace=false;
        var preview=FreeStartEconomy.Preview(Difficulty.Normal.rules);
        Check(Math.Abs(preview.rentIncreaseMultiplier-1.2f)<0.00001 && preview.retailMarkup==35 && !preview.landlordGraceOwnedAtStart);
        object[] customDescription={custom,null}; Hook("Description",customDescription);
        Check(((List<string>)customDescription[1])[1].Contains("markup=35"));
        NewGameData.Instance.startScenarioId="apprentice";
        object[] disabledDescription={custom,null}; Hook("Description",disabledDescription);
        Check(((List<string>)disabledDescription[1])[1].Contains("markup=15"));
        NewGameData.Instance.startScenarioId=FreeStartScenario.Id;
        Check(Difficulty.Normal.rules.retailMarkup==15 && Difficulty.Normal.rules.landlordGraceOwnedAtStart);
        Check(Active(custom.rules).retailMarkup==35);
        PlayerStore.Instance=new PlayerStore { difficultyId=custom.id };
        EconomyHook("SaveStartingRules","Prefix");
        var saved=Active(custom.rules);
        Check(saved.retailMarkup==35 && !saved.landlordGraceOwnedAtStart && Math.Abs(saved.rentIncreaseMultiplier-1.2f)<0.00001);
        ExperienceSettings.FreeStartRentGrowth=100; ExperienceSettings.FreeStartRetailMarkup=0;
        ExperienceSettings.FreeStartLandlordGrace=true;
        Check(Active(custom.rules).retailMarkup==35 && !Active(custom.rules).landlordGraceOwnedAtStart);
        Check(FreeStartEconomy.Preview(custom.rules).rentIncreaseMultiplier==2f && FreeStartEconomy.Preview(custom.rules).retailMarkup==0);
        // A second run stores a distinct snapshot; old saves without keys keep normal defaults.
        ModHelper.Numbers=new Dictionary<string,int>(); ModHelper.Flags=new Dictionary<string,bool>();
        Check(Active(custom.rules).retailMarkup==15 && Active(custom.rules).landlordGraceOwnedAtStart);
        EconomyHook("SaveStartingRules","Prefix");
        Check(Active(custom.rules).retailMarkup==0 && Active(custom.rules).rentIncreaseMultiplier==2f && Active(custom.rules).landlordGraceOwnedAtStart);
        ExperienceSettings.FreeStartRentGrowth=0; ExperienceSettings.FreeStartRetailMarkup=100;
        EconomyHook("SaveStartingRules","Prefix");
        Check(Active(custom.rules).rentIncreaseMultiplier==1f && Active(custom.rules).retailMarkup==100);
        PlayerStore.Instance.difficultyId="normal";
        Check(Active(Difficulty.Normal.rules)==Difficulty.Normal.rules);
        int savedRent=ModHelper.Numbers["free_start_rent_growth"];
        EconomyHook("SaveStartingRules","Prefix");
        Check(ModHelper.Numbers["free_start_rent_growth"]==savedRent);
        NewGameData.Instance.startScenarioId=FreeStartScenario.Id;
        PlayerStore.Instance.difficultyId=custom.id;
        // Every supported pair must use the specified category, including sums of one.
        for(int power=0;power<=10;power++)
            for(int insurance=0;insurance<=10;insurance++)
            {
                ExperienceSettings.FreeStartPowerMultiplier=power;
                ExperienceSettings.FreeStartInsuranceMultiplier=insurance;
                var servicePreview=FreeStartEconomy.Preview(custom.rules);
                Check(servicePreview.powerCostMultiplier==power && servicePreview.insuranceCostMultiplier==insurance);
                object[] serviceDescription={custom,null}; Hook("Description",serviceDescription);
                int sum=power+insurance;
                string category=sum<=0 ? "free_difficulty_service_low" : sum<=2 ? "free_difficulty_service_standard" : "free_difficulty_service_high";
                Check(((List<string>)serviceDescription[1])[2]==category);
                EconomyHook("SaveStartingRules","Prefix");
                ExperienceSettings.FreeStartPowerMultiplier=1; ExperienceSettings.FreeStartInsuranceMultiplier=1;
                var restored=Active(custom.rules);
                Check(restored.powerCostMultiplier==power && restored.insuranceCostMultiplier==insurance);
            }
        NewGameData.Instance.startScenarioId="apprentice";
        object[] lockedDescription={custom,null}; Hook("Description",lockedDescription);
        Check(((List<string>)lockedDescription[1])[2]=="native service fees");
        NewGameData.Instance.startScenarioId=FreeStartScenario.Id;
        ModHelper.Numbers.Clear(); ModHelper.Flags.Clear();
        Check(Active(custom.rules).powerCostMultiplier==1 && Active(custom.rules).insuranceCostMultiplier==1);
        Check(Difficulty.Normal.rules.powerCostMultiplier==1 && Difficulty.Normal.rules.insuranceCostMultiplier==1);
        Check(Active(custom.rules).vendorPermitFee==1500 && Active(custom.rules).appraisalJoinCostMultiplier==1);
        foreach(int fee in new[]{0,1000,4321,10000})
            foreach(int multiplier in new[]{0,1,7,10})
            {
                ExperienceSettings.FreeStartVendorPermitFee=fee;
                ExperienceSettings.FreeStartAppraisalJoinMultiplier=multiplier;
                var feePreview=FreeStartEconomy.Preview(custom.rules);
                Check(feePreview.vendorPermitFee==fee && feePreview.appraisalJoinCostMultiplier==multiplier);
                object[] feeDescription={custom,null}; Hook("Description",feeDescription);
                Check(((List<string>)feeDescription[1])[3]=="permit="+fee);
                EconomyHook("SaveStartingRules","Prefix");
                ExperienceSettings.FreeStartVendorPermitFee=1000; ExperienceSettings.FreeStartAppraisalJoinMultiplier=1;
                var feeRestored=Active(custom.rules);
                Check(feeRestored.vendorPermitFee==fee && feeRestored.appraisalJoinCostMultiplier==multiplier);
                Check(Difficulty.Normal.rules.vendorPermitFee==1500 && Difficulty.Normal.rules.appraisalJoinCostMultiplier==1);
            }
        NewGameData.Instance.startScenarioId="apprentice";
        object[] normalFeeDescription={custom,null}; Hook("Description",normalFeeDescription);
        Check(((List<string>)normalFeeDescription[1])[3]=="permit=1500");
        Check(((List<string>)normalFeeDescription[1])[4]=="native suppliers");
        NewGameData.Instance.startScenarioId=FreeStartScenario.Id;
        foreach(int runner in new[]{0,15,30,31,99,100,1000})
            foreach(int doctor in new[]{0,15,1000})
                foreach(int retired in new[]{0,15,1000})
                {
                    ExperienceSettings.FreeStartRunnerMarkup=runner;
                    ExperienceSettings.FreeStartDoctorMarkup=doctor;
                    ExperienceSettings.FreeStartRetiredMarkup=retired;
                    object[] supplierDescription={custom,null}; Hook("Description",supplierDescription);
                    int total=runner+doctor+retired;
                    Check(((List<string>)supplierDescription[1])[4]==(total<=30 ? "free_difficulty_supplier_low" : total>=100 ? "free_difficulty_supplier_high" : "free_difficulty_supplier_standard"));
                    var supplierPreview=FreeStartEconomy.Preview(custom.rules);
                    Check(supplierPreview.inventorMarkup==doctor && supplierPreview.retiredSupplierMarkup==retired);
                    EconomyHook("SaveStartingRules","Prefix");
                    ExperienceSettings.FreeStartRunnerMarkup=15; ExperienceSettings.FreeStartHypertechMarkup=50; ExperienceSettings.FreeStartDoctorMarkup=15; ExperienceSettings.FreeStartRetiredMarkup=15;
                    var restoredSupplier=Active(custom.rules);
                    Check(restoredSupplier.inventorMarkup==doctor && restoredSupplier.retiredSupplierMarkup==retired);
                    var feature=new ItemFeature(); EconomyHook("RunnerPrice","Postfix",feature);
                    Check(feature.Percent==runner);
                    var hypertechFeature=new ItemFeature(); EconomyHook("HypertechPrice","Postfix",hypertechFeature);
                    Check(hypertechFeature.Percent==50);
                }
        ModHelper.Numbers.Clear();
        var legacyFeature=new ItemFeature(); EconomyHook("RunnerPrice","Postfix",legacyFeature);
        Check(legacyFeature.Percent==50);
        PlayerStore.Instance.difficultyId="normal";
        var nativeFeature=new ItemFeature { Percent=50 }; EconomyHook("RunnerPrice","Postfix",nativeFeature);
        Check(nativeFeature.Percent==50);
        ModEntry.IsEnabled=false;
        Check(FreeStartDifficulty.CanSelect(custom.id) && (bool)Hook("ValidateStart"));
        Check((bool)Hook("Description",new object[]{custom,null}));
        Check(Active(custom.rules)==custom.rules);
        FreeStartDifficulty.Remove(); Check(ModHelper.Registered==null);
        Console.WriteLine("PASS: custom difficulty rules, full normal description, profession restrictions, cached-window transitions and disabled gates ("+checks+" checks).");
    }
}
