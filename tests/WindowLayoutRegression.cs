using System;
using System.Collections.Generic;
using System.Globalization;
using XicesBetterExperience;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name, Type[] arguments) { } }
}
namespace UnityEngine
{
    public class Camera { }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x=x; this.y=y; } }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;} }
    public class Transform
    {
        public GameObject gameObject; public Transform parent; public Vector3 position;
    }
    public class RectTransform : Transform
    {
        public float Width=400, Height=500, Scale=1;
        public T GetComponentInParent<T>() where T : class { return gameObject.Canvas as T; }
        public void GetWorldCorners(Vector3[] corners)
        {
            corners[0]=new Vector3(position.x-Width*Scale/2,position.y-Height*Scale/2,0);
            corners[2]=new Vector3(position.x+Width*Scale/2,position.y+Height*Scale/2,0);
        }
    }
    public class GameObject
    {
        public bool activeInHierarchy=true; public RectTransform transform; public StoreUI UI; public Canvas Canvas = new Canvas();
        public GameObject() {transform=new RectTransform {gameObject=this};}
        public T GetComponent<T>() where T : class { return UI as T; }
    }
    public class Canvas
    {
        public RenderMode renderMode; public Camera worldCamera;
        public static event Action willRenderCanvases;
        public static int Subscriptions {get {return willRenderCanvases==null?0:willRenderCanvases.GetInvocationList().Length;}}
        public static void Render() {if(willRenderCanvases!=null)willRenderCanvases();}
    }
    public static class Screen {public static int width=1920,height=1080;}
    public static class RectTransformUtility
    {
        public static Vector2 WorldToScreenPoint(Camera camera, Vector3 point) {return new Vector2(point.x,point.y);}
        public static bool ScreenPointToWorldPointInRectangle(RectTransform rect,Vector2 point,Camera camera,out Vector3 world)
        {world=new Vector3(point.x,point.y,0);return true;}
    }
    public static class Debug { public static void LogError(object error) {throw new Exception(error.ToString());} }
}
namespace UnityEngine.EventSystems
{
    public class PointerEventData { public enum InputButton {Left,Right} public InputButton button; }
}
public class StoreUI
{
    public string identifier="negociation_ui"; public UnityEngine.GameObject gameObject;
    public UnityEngine.RectTransform transform {get {return gameObject.transform;}}
    public void OnEnable()
    {
        int level=1;
        PlayerStore.Instance.storeUIZoomLevels.TryGetValue(identifier,out level); if(level==0)level=1;
        WindowLayoutMemory.ZoomReadPatch.Postfix(this,ref level);
        transform.Scale=level==2?.5f:level==3?.75f:1f;
        WindowLayoutMemory.OpenPatch.Postfix(this);
    }
}
public class UIDragHandler
{
    public StoreUI UI;
    public T GetComponent<T>() where T : class {return UI as T;}
}
public class NegociationUIManager {public static NegociationUIManager Instance;public UnityEngine.GameObject uiObject;}
public class PhoneUIManager {public static PhoneUIManager Instance;public UnityEngine.GameObject phonePanel;}
public class PlayerStore
{
    public static PlayerStore Instance=new PlayerStore(); public static bool IsInstanceExist() {return Instance!=null;}
    public Dictionary<string,int> storeUIZoomLevels=new Dictionary<string,int>();
}
public static class LetterboxManager
{
    public static List<Action> Deferred=new List<Action>();
    public static void RunNextUpdate(Action action) {Deferred.Add(action);}
    public static void Update() {var snapshot=Deferred.ToArray();Deferred.Clear();foreach(var action in snapshot)action();}
}
namespace XicesBetterExperience
{
    public static class ModEntry {public static bool IsEnabled=true;}
    public static class ExperienceSettings
    {
        public static bool RetainWindowLayout=true;
        public static string TabletLayout="";
        public static string PhoneLayout="";
        public static void SavePhoneLayout(string value) {if(value!=PhoneLayout){PhoneLayout=value;Writes++;}}
        public static int Writes;
        public static void SaveTabletLayout(string value) {if(value!=TabletLayout){TabletLayout=value;Writes++;}}
    }
}
public static class WindowLayoutRegression
{
    static void Check(bool condition,string message) {if(!condition)throw new Exception(message);}
    static StoreUI Create(float x=400,float y=400)
    {
        var root=new UnityEngine.GameObject();root.transform.parent=new UnityEngine.GameObject().transform;
        root.transform.position=new UnityEngine.Vector3(x,y,0);
        var ui=new StoreUI {gameObject=root};root.UI=ui;
        NegociationUIManager.Instance=new NegociationUIManager {uiObject=root};return ui;
    }
    static void Flush() {LetterboxManager.Update();LetterboxManager.Update();UnityEngine.Canvas.Render();}
    static void At(StoreUI ui,float x,float y,float scale,string message)
    {Check(Math.Abs(ui.transform.position.x-x)<.01&&Math.Abs(ui.transform.position.y-y)<.01&&ui.transform.Scale==scale,message);}
    static void Drag(StoreUI ui,float x,float y)
    {
        ui.transform.position=new UnityEngine.Vector3(x,y,0);
        WindowLayoutMemory.DragEndPatch.Postfix(new UIDragHandler {UI=ui},new UnityEngine.EventSystems.PointerEventData());
    }
    public static void Main()
    {
        var ui=Create();ui.OnEnable();Flush();At(ui,400,400,1,"No saved layout preserves native defaults");
        Drag(ui,800,500);
        PlayerStore.Instance.storeUIZoomLevels[ui.identifier]=3;ui.transform.Scale=.75f;
        WindowLayoutMemory.ZoomChangedPatch.Postfix(ui);
        string saved=ExperienceSettings.TabletLayout;
        Check(ExperienceSettings.Writes==2,"Each completed mutation saves one combined record");
        WindowLayoutMemory.ClosePatch.Prefix(NegociationUIManager.Instance);ui.gameObject.activeInHierarchy=false;
        ui.gameObject.activeInHierarchy=true;ui.OnEnable();
        // Native dock/reclamp may transform coordinates after enable.
        ui.transform.position=new UnityEngine.Vector3(200,200,0);Flush();At(ui,800,500,.75f,"Close/reopen restores B and S after docking");
        Check(ExperienceSettings.Writes==2,"Restoration does not rewrite saved layout");
        WindowLayoutMemory.Clear();PlayerStore.Instance=new PlayerStore();ui=Create();ui.OnEnable();Flush();
        At(ui,800,500,.75f,"New save with no zoom record uses global layout");
        WindowLayoutMemory.Clear();ExperienceSettings.TabletLayout=saved;PlayerStore.Instance=new PlayerStore();
        PlayerStore.Instance.storeUIZoomLevels["negociation_ui"]=2;ui=Create();ui.OnEnable();Flush();
        At(ui,800,500,.75f,"Reloaded global configuration overrides another save's zoom");
        int next=1;WindowLayoutMemory.ZoomReadPatch.Postfix(ui,ref next);next=next%3+1;
        Check(next==1,"Next native zoom click continues from restored level");
        var other=Create();other.identifier="other";other.OnEnable();Flush();At(other,400,400,1,"Other windows excluded");
        WindowLayoutMemory.Clear();ui.OnEnable();LetterboxManager.Update();
        WindowLayoutMemory.ClosePatch.Prefix(new NegociationUIManager {uiObject=ui.gameObject});ui.gameObject.activeInHierarchy=false;
        Flush();Check(ExperienceSettings.TabletLayout==saved&&UnityEngine.Canvas.Subscriptions==0,"Quick close cancels restore without overwriting B with defaults");
        ui.gameObject.activeInHierarchy=true;ui.OnEnable();
        WindowLayoutMemory.DragEndPatch.Postfix(new UIDragHandler {UI=other},new UnityEngine.EventSystems.PointerEventData());
        Flush();At(ui,800,500,.75f,"Unrelated drag does not cancel tablet restore");
        int writes=ExperienceSettings.Writes;
        ExperienceSettings.RetainWindowLayout=false;WindowLayoutMemory.ConfigApplied();
        ui=Create();PlayerStore.Instance.storeUIZoomLevels[ui.identifier]=2;ui.OnEnable();Flush();At(ui,400,400,.5f,"Disabled feature uses native layout and zoom");
        Drag(ui,900,600);Check(ExperienceSettings.Writes==writes,"Disabled feature does not record user actions");
        ExperienceSettings.RetainWindowLayout=true;WindowLayoutMemory.ConfigApplied();Flush();At(ui,800,500,.75f,"Re-enable applies retained layout to open tablet");
        var culture=CultureInfo.CurrentCulture;CultureInfo.CurrentCulture=new CultureInfo("fr-FR");
        Drag(ui,850,550);saved=ExperienceSettings.TabletLayout;WindowLayoutMemory.Layout parsed;
        Check(WindowLayoutMemory.Layout.TryDecode(saved,out parsed)&&parsed.Zoom==3,"Layout serialization is locale independent");
        CultureInfo.CurrentCulture=culture;
        foreach(string bad in new[]{"","bad","NaN;0.5;1","Infinity;0;2","0;0;4","999;0;1"})
            Check(!WindowLayoutMemory.Layout.TryDecode(bad,out parsed),"Invalid layout rejected: "+bad);
        UnityEngine.Screen.width=960;UnityEngine.Screen.height=540;ui=Create();ui.OnEnable();Flush();
        At(ui,425,275,.75f,"Different resolution restores normalized position");
        ExperienceSettings.TabletLayout="2;2;1";ui.OnEnable();Flush();
        Check(ui.transform.position.x<=UnityEngine.Screen.width+200-40&&ui.transform.position.y+250<=UnityEngine.Screen.height,"Offscreen restored header clamped into view");
        writes=ExperienceSettings.Writes;WindowLayoutMemory.Clear();
        for(int i=0;i<100;i++){LetterboxManager.Update();UnityEngine.Canvas.Render();}
        Check(ExperienceSettings.Writes==writes&&UnityEngine.Canvas.Subscriptions==0,"No idle persistence or polling");
        UnityEngine.Screen.width=1920;UnityEngine.Screen.height=1080;
        ExperienceSettings.TabletLayout=saved;
        var phone=Create();phone.identifier="";
        PhoneUIManager.Instance=new PhoneUIManager {phonePanel=phone.gameObject};
        phone.OnEnable();WindowLayoutMemory.PhoneOpenPatch.Postfix(PhoneUIManager.Instance);Flush();
        At(phone,400,400,1,"Phone has no saved layout initially");
        Drag(phone,1100,600);string phoneSaved=ExperienceSettings.PhoneLayout;
        Check(ExperienceSettings.TabletLayout==saved,"Phone drag does not overwrite tablet layout");
        WindowLayoutMemory.PhoneClosePatch.Prefix(PhoneUIManager.Instance);phone.gameObject.activeInHierarchy=false;
        phone.gameObject.activeInHierarchy=true;phone.OnEnable();WindowLayoutMemory.PhoneOpenPatch.Postfix(PhoneUIManager.Instance);
        phone.transform.position=new UnityEngine.Vector3(200,200,0);Flush();At(phone,1100,600,1,"Phone reopen restores after native docking");
        for(int scene=0;scene<3;scene++)
        {
            WindowLayoutMemory.Clear();PlayerStore.Instance=new PlayerStore();phone=Create();phone.identifier="";
            PhoneUIManager.Instance=new PhoneUIManager {phonePanel=phone.gameObject};
            phone.OnEnable();WindowLayoutMemory.PhoneOpenPatch.Postfix(PhoneUIManager.Instance);Flush();
            At(phone,1100,600,1,"Phone layout survives scene/process configuration reload and new save");
        }
        ui=Create();ui.OnEnable();phone.OnEnable();
        WindowLayoutMemory.PhoneClosePatch.Prefix(PhoneUIManager.Instance);phone.gameObject.activeInHierarchy=false;
        Flush();At(ui,850,550,.75f,"Closing phone does not cancel pending tablet restoration");
        Check(ExperienceSettings.PhoneLayout==phoneSaved,"Quick phone close does not overwrite saved position");
        phone.gameObject.activeInHierarchy=true;phone.OnEnable();
        Drag(ui,900,550);Flush();At(phone,1100,600,1,"Dragging tablet does not cancel pending phone restoration");
        ui.OnEnable();LetterboxManager.Update();phone.OnEnable();LetterboxManager.Update();UnityEngine.Canvas.Render();
        At(ui,900,550,.75f,"Earlier window restore completes first");
        LetterboxManager.Update();UnityEngine.Canvas.Render();At(phone,1100,600,1,"Later window restore waits for its own native docking");
        ExperienceSettings.RetainWindowLayout=false;WindowLayoutMemory.ConfigApplied();
        Drag(phone,1200,650);Check(ExperienceSettings.PhoneLayout==phoneSaved,"Disabled feature does not save phone position");
        ExperienceSettings.RetainWindowLayout=true;NegociationUIManager.Instance=null;WindowLayoutMemory.ConfigApplied();Flush();
        At(phone,1100,600,1,"Re-enable restores phone even without tablet manager");
        var unrelated=Create();unrelated.identifier="";Drag(unrelated,700,500);
        Check(ExperienceSettings.PhoneLayout==phoneSaved,"Blank identifier alone does not identify phone");
        WindowLayoutMemory.Clear();
        Check(UnityEngine.Canvas.Subscriptions==0,"No phone idle render subscription");
        Console.WriteLine("PASS: phone global layout, independent records, concurrent restoration, quick close, disabled gates, exact panel identity; tablet global layout, scene/config reload, new saves, zoom continuity, docking order, quick close, gates, locale, bounds and zero idle writes.");
    }
}
