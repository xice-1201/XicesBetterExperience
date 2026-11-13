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
    public UnityEngine.GameObject gameObject { get { return Root ?? (UI == null ? null : UI.gameObject); } }
    public UnityEngine.GameObject Root;
    public T GetComponent<T>() where T : class {return UI as T;}
}
public class NegociationUIManager {public static NegociationUIManager Instance;public UnityEngine.GameObject uiObject;}
public class WaterScannerUIManager {public static WaterScannerUIManager Instance;public UnityEngine.GameObject panel;}
public class PhoneUIManager {public static PhoneUIManager Instance;public UnityEngine.GameObject phonePanel;}
public class TreeNodeRender { }
public class PixelWindow
{
    public UnityEngine.GameObject Root = new UnityEngine.GameObject();
    public UnityEngine.RectTransform rectTransform { get { return Root.transform; } }
    public bool IsVisible() { return Root.activeInHierarchy; }
    public PixelWindow() { Root.transform.parent = new UnityEngine.GameObject().transform; }
}
public class GameItem { public PixelWindow contentWindow; }
public class GameSlotInventory { public GameItem childItem; }
public class EmporiumEntry
{
    public static EmporiumEntry Instance;
    public GameItem dossier, trashcan;
    public GameSlotInventory vendingFountainElement;
}
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
        public static string ArchiveLayout="", TrashLayout="";
        public static void SaveArchiveLayout(string value) {if(value!=ArchiveLayout){ArchiveLayout=value;Writes++;}}
        public static void SaveTrashLayout(string value) {if(value!=TrashLayout){TrashLayout=value;Writes++;}}
        public static string VendingFountainLayout="";
        public static void SaveVendingFountainLayout(string value) {if(value!=VendingFountainLayout){VendingFountainLayout=value;Writes++;}}
        public static string WaterScannerLayout="";
        public static void SaveWaterScannerLayout(string value) {if(value!=WaterScannerLayout){WaterScannerLayout=value;Writes++;}}
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
    static void FixtureTests()
    {
        var archive = new PixelWindow(); var trash = new PixelWindow();
        var archiveDrag = new TreeNodeRender(); var trashDrag = new TreeNodeRender();
        EmporiumEntry.Instance = new EmporiumEntry { dossier = new GameItem {contentWindow=archive}, trashcan = new GameItem {contentWindow=trash} };
        WindowLayoutMemory.FixtureOpened(archive,archiveDrag);
        Check(archive.rectTransform.position.x==0,"No fixture record leaves native position untouched");
        archive.rectTransform.position=new UnityEngine.Vector3(700,450,0);
        WindowLayoutMemory.FixtureDragPatch.Postfix(archiveDrag,new UnityEngine.EventSystems.PointerEventData());
        string archiveSaved=ExperienceSettings.ArchiveLayout;
        WindowLayoutMemory.FixtureOpened(trash,trashDrag);
        trash.rectTransform.position=new UnityEngine.Vector3(1200,600,0);
        WindowLayoutMemory.FixtureHidePatch.Prefix(trash);
        string trashSaved=ExperienceSettings.TrashLayout;
        Check(archiveSaved!=trashSaved,"Archive and trash use independent records");
        int writes=ExperienceSettings.Writes;
        archive.rectTransform.position=new UnityEngine.Vector3(100,100,0);
        WindowLayoutMemory.FixtureShowAtPatch.Postfix(archive,archiveDrag);
        Check(archive.rectTransform.position.x==700,"Show restores archive");
        archive.rectTransform.position=new UnityEngine.Vector3(200,200,0);
        WindowLayoutMemory.FixtureNativeRestorePatch.Postfix(EmporiumEntry.Instance.dossier);
        Check(archive.rectTransform.position.x==700,"Global layout wins after native per-save restore");
        Check(ExperienceSettings.Writes==writes,"Opening/restoring fixtures does not persist defaults");
        var unrelated=new PixelWindow();unrelated.rectTransform.position=new UnityEngine.Vector3(333,333,0);
        WindowLayoutMemory.FixtureOpened(unrelated,new TreeNodeRender());WindowLayoutMemory.CaptureFixture(unrelated);
        Check(unrelated.rectTransform.position.x==333&&ExperienceSettings.Writes==writes,"Other containers excluded by exact fixture identity");
        ExperienceSettings.RetainWindowLayout=false;WindowLayoutMemory.ConfigApplied();
        archive.rectTransform.position=new UnityEngine.Vector3(100,100,0);
        WindowLayoutMemory.FixtureOpened(archive,archiveDrag);WindowLayoutMemory.CaptureFixture(archive);
        Check(archive.rectTransform.position.x==100&&ExperienceSettings.Writes==writes,"Disabled fixture feature neither restores nor records");
        ExperienceSettings.RetainWindowLayout=true;WindowLayoutMemory.ConfigApplied();
        Check(archive.rectTransform.position.x==700&&trash.rectTransform.position.x==1200,"Re-enable restores both open fixture windows");
        archive.rectTransform.position=new UnityEngine.Vector3(750,450,0);
        WindowLayoutMemory.FixtureDragPatch.Postfix(archiveDrag,new UnityEngine.EventSystems.PointerEventData());
        Check(ExperienceSettings.ArchiveLayout!=archiveSaved,"Re-enable keeps end-drag recording available");
        archiveSaved=ExperienceSettings.ArchiveLayout;
        for(int scene=0;scene<3;scene++)
        {
            WindowLayoutMemory.Clear();archive=new PixelWindow();trash=new PixelWindow();
            EmporiumEntry.Instance=new EmporiumEntry {dossier=new GameItem {contentWindow=archive},trashcan=new GameItem {contentWindow=trash}};
            WindowLayoutMemory.FixtureOpened(archive,new TreeNodeRender());WindowLayoutMemory.FixtureOpened(trash,new TreeNodeRender());
            Check(archive.rectTransform.position.x==750&&trash.rectTransform.position.x==1200,"Fixture layouts survive save reload, restart and new save");
            Check(archive.rectTransform.Width==400&&archive.rectTransform.Height==500,"Native fixed window size preserved");
        }
        trash.Root.activeInHierarchy=false;trash.rectTransform.position=new UnityEngine.Vector3(0,0,0);
        WindowLayoutMemory.CaptureFixture(trash);WindowLayoutMemory.RestoreFixture(trash);
        Check(ExperienceSettings.TrashLayout==trashSaved&&trash.rectTransform.position.x==0,"Hidden fixture cannot overwrite or restore layout");
        UnityEngine.Screen.width=960;UnityEngine.Screen.height=540;
        WindowLayoutMemory.RestoreFixture(archive);
        Check(archive.rectTransform.position.x==375,"Fixture position normalized on resolution change");
        ExperienceSettings.ArchiveLayout="2;2;1";WindowLayoutMemory.RestoreFixture(archive);
        Check(archive.rectTransform.position.y+250<=540,"Fixture header remains reachable");
        ExperienceSettings.ArchiveLayout=archiveSaved;
        UnityEngine.Screen.width=1920;UnityEngine.Screen.height=1080;
        WindowLayoutMemory.Clear();
        Console.WriteLine("PASS: archive/trash exact identity, independent global records, native open order, drag/close capture, config gates, scene recreation, fixed sizes and screen bounds.");
    }
    static void VendingTests()
    {
        WindowLayoutMemory.Clear();
        var window=new PixelWindow();var drag=new TreeNodeRender();
        EmporiumEntry.Instance=new EmporiumEntry {vendingFountainElement=new GameSlotInventory {childItem=new GameItem {contentWindow=window}}};
        window.rectTransform.position=new UnityEngine.Vector3(960,540,0);
        WindowLayoutMemory.FixtureShowPatch.Postfix(window,drag);WindowLayoutMemory.VendingCenterPatch.Postfix(window);
        Check(window.rectTransform.position.x==960,"Unrecorded vending window keeps native center");
        string archive=ExperienceSettings.ArchiveLayout,trash=ExperienceSettings.TrashLayout;
        window.rectTransform.position=new UnityEngine.Vector3(850,500,0);
        WindowLayoutMemory.FixtureDragPatch.Postfix(drag,new UnityEngine.EventSystems.PointerEventData());
        string saved=ExperienceSettings.VendingFountainLayout;
        Check(saved!=""&&archive==ExperienceSettings.ArchiveLayout&&trash==ExperienceSettings.TrashLayout,"Vending record is independent of other fixtures");
        int writes=ExperienceSettings.Writes;
        WindowLayoutMemory.FixtureShowPatch.Postfix(window,drag);
        window.rectTransform.position=new UnityEngine.Vector3(960,540,0); // native Center after Show
        WindowLayoutMemory.VendingCenterPatch.Postfix(window);
        Check(Math.Abs(window.rectTransform.position.x-850)<.01&&Math.Abs(window.rectTransform.position.y-500)<.01,"Final native center does not overwrite remembered vending position");
        Check(writes==ExperienceSettings.Writes,"Opening and centering do not persist intermediate layouts");
        var unrelated=new PixelWindow();unrelated.rectTransform.position=new UnityEngine.Vector3(960,540,0);
        WindowLayoutMemory.VendingCenterPatch.Postfix(unrelated);
        Check(unrelated.rectTransform.position.x==960,"Center remains unchanged for other windows");
        window.rectTransform.position=new UnityEngine.Vector3(800,450,0);
        WindowLayoutMemory.FixtureHidePatch.Prefix(window);saved=ExperienceSettings.VendingFountainLayout;
        for(int i=0;i<3;i++)
        {
            WindowLayoutMemory.Clear();window=new PixelWindow();
            EmporiumEntry.Instance=new EmporiumEntry {vendingFountainElement=new GameSlotInventory {childItem=new GameItem {contentWindow=window}}};
            WindowLayoutMemory.FixtureShowPatch.Postfix(window,drag);
            window.rectTransform.position=new UnityEngine.Vector3(960,540,0);WindowLayoutMemory.VendingCenterPatch.Postfix(window);
            Check(Math.Abs(window.rectTransform.position.x-800)<.01,"Vending layout survives save reload/restart/new save");
        }
        ExperienceSettings.RetainWindowLayout=false;WindowLayoutMemory.ConfigApplied();
        window.rectTransform.position=new UnityEngine.Vector3(960,540,0);WindowLayoutMemory.VendingCenterPatch.Postfix(window);WindowLayoutMemory.CaptureFixture(window);
        Check(window.rectTransform.position.x==960&&saved==ExperienceSettings.VendingFountainLayout,"Disabled vending uses native center and does not save");
        ExperienceSettings.RetainWindowLayout=true;WindowLayoutMemory.ConfigApplied();
        Check(Math.Abs(window.rectTransform.position.x-800)<.01,"Config re-enable restores open vending window");
        window.Root.activeInHierarchy=false;WindowLayoutMemory.CaptureFixture(window);
        Check(saved==ExperienceSettings.VendingFountainLayout,"Hidden vending window cannot overwrite record");
        EmporiumEntry.Instance=new EmporiumEntry();WindowLayoutMemory.ConfigApplied();WindowLayoutMemory.Clear();
        Check(UnityEngine.Canvas.Subscriptions==0,"Absent vending machine safe and no idle render hook");
        Console.WriteLine("PASS: vending fountain independent layout, final native centering order, drag/close persistence, scene recreation, config gates, absent fixtures and unrelated-window exclusion.");
    }
    static UnityEngine.GameObject ScannerPanel()
    {
        var root=new UnityEngine.GameObject();root.transform.parent=new UnityEngine.GameObject().transform;
        root.transform.position=new UnityEngine.Vector3(400,400,0);
        WaterScannerUIManager.Instance=new WaterScannerUIManager {panel=root};return root;
    }
    static void OpenScanner()
    {WindowLayoutMemory.WaterScannerOpenPatch.Postfix(WaterScannerUIManager.Instance,new GameItem());}
    static void WaterScannerTests()
    {
        WindowLayoutMemory.Clear();
        var root=ScannerPanel();OpenScanner();Flush();
        Check(Math.Abs(root.transform.position.x-400)<.01f,"Unrecorded scanner keeps native position without StoreUI");
        string tablet=ExperienceSettings.TabletLayout,phone=ExperienceSettings.PhoneLayout,archive=ExperienceSettings.ArchiveLayout,trash=ExperienceSettings.TrashLayout;
        root.transform.position=new UnityEngine.Vector3(1000,550,0);
        WindowLayoutMemory.DragEndPatch.Postfix(new UIDragHandler {Root=root},new UnityEngine.EventSystems.PointerEventData());
        string saved=ExperienceSettings.WaterScannerLayout;
        Check(saved!=""&&tablet==ExperienceSettings.TabletLayout&&phone==ExperienceSettings.PhoneLayout&&archive==ExperienceSettings.ArchiveLayout&&trash==ExperienceSettings.TrashLayout,"Scanner drag persists independently from all other layouts");
        int writes=ExperienceSettings.Writes;
        OpenScanner();root.transform.position=new UnityEngine.Vector3(100,100,0);Flush();
        Check(Math.Abs(root.transform.position.x-1000)<.01f&&Math.Abs(root.transform.position.y-550)<.01f,"Scanner restores after native docking");
        Check(ExperienceSettings.Writes==writes,"Restore does not rewrite configuration");
        OpenScanner();WindowLayoutMemory.WaterScannerClosePatch.Prefix(WaterScannerUIManager.Instance);root.activeInHierarchy=false;Flush();
        Check(saved==ExperienceSettings.WaterScannerLayout&&UnityEngine.Canvas.Subscriptions==0,"Quick close cancels pending restore without saving default position");
        for(int i=0;i<3;i++)
        {
            WindowLayoutMemory.Clear();root=ScannerPanel();OpenScanner();Flush();
            Check(Math.Abs(root.transform.position.x-1000)<.01f&&Math.Abs(root.transform.position.y-550)<.01f,"Scanner recreated on save reload/restart/new save restores global record");
        }
        root.transform.position=new UnityEngine.Vector3(1050,550,0);
        WindowLayoutMemory.WaterScannerClosePatch.Prefix(WaterScannerUIManager.Instance);
        Check(ExperienceSettings.WaterScannerLayout!=saved,"Closing scanner also captures final position");
        saved=ExperienceSettings.WaterScannerLayout;
        ExperienceSettings.RetainWindowLayout=false;WindowLayoutMemory.ConfigApplied();
        root.transform.position=new UnityEngine.Vector3(200,200,0);OpenScanner();Flush();
        WindowLayoutMemory.DragEndPatch.Postfix(new UIDragHandler {Root=root},new UnityEngine.EventSystems.PointerEventData());
        Check(Math.Abs(root.transform.position.x-200)<.01f&&saved==ExperienceSettings.WaterScannerLayout,"Disabled scanner neither restores nor persists");
        ExperienceSettings.RetainWindowLayout=true;WindowLayoutMemory.ConfigApplied();Flush();
        Check(Math.Abs(root.transform.position.x-1050)<.01f,"Re-enable restores visible scanner");
        var unrelated=new UnityEngine.GameObject();unrelated.transform.parent=new UnityEngine.GameObject().transform;
        WindowLayoutMemory.QueueRootRestore(unrelated);WindowLayoutMemory.CaptureWaterScanner(unrelated);Flush();
        Check(unrelated.transform.position.x==0&&saved==ExperienceSettings.WaterScannerLayout,"Only exact scanner panel is eligible");
        root.transform.position=new UnityEngine.Vector3(200,200,0);
        WindowLayoutMemory.WaterScannerOpenPatch.Postfix(WaterScannerUIManager.Instance,null);Flush();
        Check(Math.Abs(root.transform.position.x-200)<.01f,"Failed scan with no target does not queue restoration");
        UnityEngine.Screen.width=960;UnityEngine.Screen.height=540;OpenScanner();Flush();
        Check(Math.Abs(root.transform.position.x-525)<.01f,"Scanner position follows resolution");
        ExperienceSettings.WaterScannerLayout="NaN;0;1";root.transform.position=new UnityEngine.Vector3(200,200,0);OpenScanner();Flush();
        Check(Math.Abs(root.transform.position.x-200)<.01f,"Invalid scanner record preserves native position");
        ExperienceSettings.WaterScannerLayout=saved;UnityEngine.Screen.width=1920;UnityEngine.Screen.height=1080;
        OpenScanner();ModEntry.IsEnabled=false;WindowLayoutMemory.Clear();Flush();
        Check(Math.Abs(root.transform.position.x-200)<.01f,"Mod disable cancels pending scanner restoration");ModEntry.IsEnabled=true;
        WindowLayoutMemory.Clear();writes=ExperienceSettings.Writes;
        for(int i=0;i<100;i++){LetterboxManager.Update();UnityEngine.Canvas.Render();}
        Check(ExperienceSettings.Writes==writes&&UnityEngine.Canvas.Subscriptions==0,"Scanner adds no idle polling or writes");
        Console.WriteLine("PASS: water scanner panel without StoreUI, independent global position, docking, close/reopen, save recreation, failed scans, config/mod gates, invalid data, resolution and zero idle work.");
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
        FixtureTests();
        VendingTests();
        try { WaterScannerTests(); }
        catch (Exception error) { Console.WriteLine("FAIL water scanner: " + error.Message); Environment.Exit(1); }
        Console.WriteLine("PASS: phone global layout, independent records, concurrent restoration, quick close, disabled gates, exact panel identity; tablet global layout, scene/config reload, new saves, zoom continuity, docking order, quick close, gates, locale, bounds and zero idle writes.");
    }
}
