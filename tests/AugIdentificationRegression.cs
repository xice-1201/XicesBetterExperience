using System;
using System.Collections.Generic;
using XicesBetterExperience;
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)] public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string n,Type[] a){}}
}
namespace UnityEngine {
 public class Object { }
 public struct Color {public float r,g,b,a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public class GameObject {public EPOOutline.Outlinable Outline;public T GetComponent<T>() where T:class{return Outline as T;}}
 public static class Debug {public static void LogError(object e){throw new Exception(e.ToString());}}
}
namespace UnityEngine.EventSystems {public class PointerEventData{}}
namespace EPOOutline {
 public class Parameters {public UnityEngine.Color Color;}
 public class Outlinable {public UnityEngine.GameObject gameObject; public bool enabled;public Parameters OutlineParameters=new Parameters();}
}
public class StoreClient {public string identifier="aug_med",spriteName="maleLower1",displayName="name_lower_levels_citizen",clientFaction="NONE";public bool isAug=true;}
public class StoreClientInstance {public StoreClient Client;public StoreClient GetClientBlueprint(){return Client;}}
public class PlayerStore {public static PlayerStore Instance=new PlayerStore();public bool isAugIntroduced=true,isAugEnded;public StoreClientInstance currentClientInstance;public static bool IsInstanceExist(){return Instance!=null;}}
public class StoreUIManager {public static StoreUIManager Instance=new StoreUIManager();public bool isShot;public UnityEngine.GameObject shotgun;}
public class StoreClientMono {public static StoreClientMono Instance=new StoreClientMono();}
public class SpriteDict {
 public static SpriteDict Instance=new SpriteDict();public Dictionary<string,object> spriteDictionary=new Dictionary<string,object>();
 public static List<string> GetPool(string prefix){return new List<string>{prefix+"1"};}
}
public class LocHelper {public static string GetLocalizedClientName(string key){return key;}}
public class LocGenderHelper {public static string ResolveForCurrentClient(string line){return line;}}
public class Dialogue {public string plainText;}
public class DialogUIManager {}
public class Interactable {public UnityEngine.GameObject gameObject;}
public class Shotgun {} public class StoreClientManager {} public class EscapeUIManager {} public class AugHelper {}
namespace XicesBetterExperience {
 public static class ModEntry {public static bool IsEnabled=true;}
 public static class ExperienceSettings {public static bool AugIdentification=true;}
}
public static class AugIdentificationRegression {
 static EPOOutline.Outlinable Gun {get{return StoreUIManager.Instance.shotgun.Outline;}}
 static void Check(bool v,string text){if(!v)throw new Exception(text);}
 static StoreClient New(string sprite="maleLower1",string id="aug_med"){
  AugIdentificationAutomation.Clear();StoreUIManager.Instance.isShot=false;
  var c=new StoreClient{spriteName=sprite,identifier=id};PlayerStore.Instance.currentClientInstance=new StoreClientInstance{Client=c};return c;
 }
 static void Approach(){AugIdentificationAutomation.ApproachPatch.Postfix();}
 static void Speak(string text){AugIdentificationAutomation.SpeechPatch.Postfix(new Dialogue{plainText=text});}
 static void Idle(StoreClient c,bool freeze,bool shake){AugIdentificationAutomation.IdlePatch.Postfix(c,freeze,shake);}
 public static void Main(){
  var go=new UnityEngine.GameObject();go.Outline=new EPOOutline.Outlinable{gameObject=go};StoreUIManager.Instance.shotgun=go;
  foreach(string p in new[]{"maleLower","femaleLower","maleUpper","femaleUpper","maleBM","femaleBM","maleMed","femaleMed","maleScav","maleFarmer","maleChef","maleEng","maleMerc","maleSci","augLLMale","augLLFemale","augBMMale","augBMFemale","augULMale","augULFemale"})SpriteDict.Instance.spriteDictionary[p+"1"]=new object();
  foreach(bool enabled in new[]{false,true})foreach(bool intro in new[]{false,true})foreach(bool ended in new[]{false,true})foreach(bool aug in new[]{false,true}){
   ExperienceSettings.AugIdentification=enabled;PlayerStore.Instance.isAugIntroduced=intro;PlayerStore.Instance.isAugEnded=ended;
   var c=New("augLLMale1");c.isAug=aug;Approach();Check(Gun.enabled==(enabled&&intro&&!ended&&aug),"All configuration, crisis and human gates");
  }
  ExperienceSettings.AugIdentification=true;PlayerStore.Instance.isAugIntroduced=true;PlayerStore.Instance.isAugEnded=false;
  var client=New();Approach();Check(!Gun.enabled,"Identity alone cannot detect Aug");
  Idle(client,false,false);Check(!Gun.enabled,"Normal idle is not a tell");
  foreach(bool frozen in new[]{true,false}){client=New();Approach();Check(!Gun.enabled,"No early idle tell");Idle(client,frozen,!frozen);Check(Gun.enabled,"Actual frozen/shaking result detects");}
  client=New("maleUpper1");Approach();Check(Gun.enabled,"Dedicated buyer outfit mismatch detects on entry");
  client=New("maleMed1","event_pharmacist");string before;AugIdentificationAutomation.DisguisePatch.Prefix(client,out before);AugIdentificationAutomation.DisguisePatch.Postfix(client,before);Approach();Check(!Gun.enabled,"Legitimate professional outfit is not a mismatch");
  client=New("maleMed1","event_pharmacist");AugIdentificationAutomation.DisguisePatch.Prefix(client,out before);client.spriteName="maleUpper1";AugIdentificationAutomation.DisguisePatch.Postfix(client,before);Approach();Check(Gun.enabled,"Actually replaced outfit detects disguised event Aug");
  client=New();Approach();
  AugIdentificationAutomation.LocalizationPatch.Postfix("aug_dialog_tier2_hint_unit_b","本单位需要获取材料。");
  AugIdentificationAutomation.LocalizationPatch.Postfix("aug_dialog_tier2_hint_unit_a","你好，我需要材料。");
  Speak("你好，我需要材料。");Check(!Gun.enabled,"Clean earlier sentence is not detected, even when future suspicious line is generated");
  Speak("<i>本单位需要获取材料。</i>");Check(Gun.enabled,"Only actual displayed suspicious sentence detects");
  var hover=new Interactable{gameObject=go};AugIdentificationAutomation.HoverPatch.Prefix(hover);Gun.enabled=false;AugIdentificationAutomation.HoverPatch.Postfix(hover);Check(Gun.enabled,"Native mouse exit does not remove alert");
  AugIdentificationAutomation.LeavePatch.Prefix();Check(!Gun.enabled,"Leave restores latest native outline state");
  client=New();Approach();Speak("你好，我需要材料。");ExperienceSettings.AugIdentification=false;AugIdentificationAutomation.ConfigApplied();Speak("本单位需要获取材料。");Check(!Gun.enabled,"Disabled speech never highlights");ExperienceSettings.AugIdentification=true;AugIdentificationAutomation.ConfigApplied();Check(Gun.enabled,"Enable only uses already observed speech");
  PlayerStore.Instance.isAugEnded=true;AugIdentificationAutomation.ConfigApplied();Check(!Gun.enabled,"Ended crisis clears immediately");PlayerStore.Instance.isAugEnded=false;
  client=New("augLLMale1");Approach();AugIdentificationAutomation.ShootPatch.Prefix();StoreUIManager.Instance.isShot=true;Speak("本单位需要获取材料。");Check(!Gun.enabled,"Shoot clears without speech reactivation");
  client=New("augLLMale1");Approach();AugIdentificationAutomation.MenuPatch.Prefix();Check(!Gun.enabled,"Menu clears");
  client=New();Gun.enabled=true;Gun.OutlineParameters.Color=new UnityEngine.Color(.3f,.4f,.5f,1);client.spriteName="augLLMale1";Approach();AugIdentificationAutomation.Clear();Check(Gun.enabled&&Gun.OutlineParameters.Color.g==.4f,"Existing native hover enabled/color restored");Gun.enabled=false;
  Check(!AugIdentificationRules.IsSuspiciousKey("aug_dialog_tier1_0_a")&&!AugIdentificationRules.IsSuspiciousKey("aug_dialog_unholster_1")&&AugIdentificationRules.IsSuspiciousKey("aug_dialog_unholster_ob2_0"),"Normal dialogue and pleading excluded");
  Console.WriteLine("PASS: Aug crisis/config/human gates, entry and actual idle timing, event outfit baselines, displayed localized speech only, native hover restoration, leave/shoot/menu cleanup.");
 }
}
