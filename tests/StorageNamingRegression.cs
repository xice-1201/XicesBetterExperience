using System;
using System.Collections.Generic;
using XicesBetterExperience;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
    }
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; } }
    public struct Vector4 { }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b, float a) { this.r=r; this.g=g; this.b=b; this.a=a; } public static Color black, white; }
    public class MonoBehaviour { }
    public class GameObject
    {
        public string name; public bool Destroyed;
        public readonly List<GameObject> Children = new List<GameObject>();
        public readonly RectTransform transform;
        public readonly Dictionary<Type, object> Components = new Dictionary<Type, object>();
        public GameObject() { transform = new RectTransform { Owner = this }; }
        public GameObject(string name, params Type[] types) : this() { this.name = name; }
        public T GetComponent<T>() where T : class { return Components.ContainsKey(typeof(T)) ? Components[typeof(T)] as T : transform as T; }
        public T AddComponent<T>() where T : new()
        {
            if (typeof(UnityEngine.UI.MaskableGraphic).IsAssignableFrom(typeof(T)) && Components.ContainsKey(typeof(TestImage)))
                throw new InvalidOperationException("A GameObject can only contain one Graphic component");
            var value = new T();
            var graphic = value as UnityEngine.UI.MaskableGraphic;
            if (graphic != null) graphic.rectTransform = transform;
            Components[typeof(T)] = value; return value;
        }
    }
    public class RectTransform
    {
        public GameObject Owner;
        public void SetParent(RectTransform parent, bool worldPositionStays) { parent.Owner.Children.Add(Owner); }
        public Vector2 anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition;
        public UnityEngine.Vector3 localEulerAngles;
        public void SetAsLastSibling() { }
        public void SetAsFirstSibling() { }
    }
    public static class Object { public static void Destroy(GameObject gameObject) { gameObject.Destroyed = true; } }
    public static class Canvas
    {
        public static event Action willRenderCanvases;
        public static int Subscriptions { get { return willRenderCanvases == null ? 0 : willRenderCanvases.GetInvocationList().Length; } }
        public static void Render() { if (willRenderCanvases != null) willRenderCanvases(); }
    }
}
namespace TMPro
{
    public enum FontStyles { Normal }
    public enum TextAlignmentOptions { Bottom, Center }
    public enum TextOverflowModes { Ellipsis }
    public class TextMeshProUGUI
    {
        public bool raycastTarget, richText, enableWordWrapping, enableAutoSizing, enabled;
        public FontStyles fontStyle;
        public TextAlignmentOptions alignment;
        public TextOverflowModes overflowMode;
        public float fontSizeMin, fontSizeMax;
        public UnityEngine.Color color;
        public string text;
    }
}
namespace UnityEngine.UI
{
    public class Toggle { public bool isOn; public bool interactable = true; }
    public class Image { public enum Type { Simple, Sliced, Tiled, Filled } }
    public class MaskableGraphic
    {
        public UnityEngine.RectTransform rectTransform;
        public bool enabled, raycastTarget;
        public int GeometryUpdates;
        public void SetVerticesDirty() { GeometryUpdates++; }
        protected virtual void OnPopulateMesh(VertexHelper vh) { }
    }
    public class VertexHelper
    {
        public int currentVertCount;
        public void Clear() { currentVertCount = 0; }
        public void AddVert(UnityEngine.Vector3 point, UnityEngine.Color color, UnityEngine.Vector2 uv) { currentVertCount++; }
        public void AddTriangle(int a, int b, int c) { }
    }
}
public class TestImage { public bool enabled, raycastTarget, preserveAspect; public UnityEngine.UI.Image.Type type; public float fillAmount; public object sprite; public UnityEngine.Color color; }
public class TreeNodeRender
{
    public readonly UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
    public readonly UnityEngine.RectTransform rectTransform = new UnityEngine.RectTransform();
    public readonly TMPro.TextMeshProUGUI text = new TMPro.TextMeshProUGUI();
    public readonly TestImage image = new TestImage();
    public TreeNodeRender() { rectTransform.Owner = gameObject; gameObject.Components[typeof(TestImage)] = image; }
    public TreeNodeRender parent;
    public static readonly List<TreeNodeRender> Created = new List<TreeNodeRender>();
    public static TreeNodeRender InstantiateText() { var node = new TreeNodeRender(); Created.Add(node); return node; }
    public static TreeNodeRender Instantiate() { var node = new TreeNodeRender(); Created.Add(node); return node; }
    public TreeNodeRender SetParent(TreeNodeRender parent) { this.parent = parent; return this; }
}
public static class ModHook
{
    public static event Action OnGameLoadedLate;
    public static event Action<string> OnModConfigApplied;
    public static void Load() { if (OnGameLoadedLate != null) OnGameLoadedLate(); }
    public static void Apply(string id) { if (OnModConfigApplied != null) OnModConfigApplied(id); }
}
public static class StorageNamingRegression
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static List<TreeNodeRender> Nodes(GameItemElement item)
    {
        return TreeNodeRender.Created.FindAll(node => !node.gameObject.Destroyed &&
            (node.parent == item.middle || node.parent == item.fakeMiddle));
    }
    private static GameItemElement Storage(string id, string name)
    {
        var item = new GameItemElement { identifier = id, Owned = true };
        item.background.image.enabled = true;
        if (name != null) GeneralHelper.SetCustomName(item, name);
        return item;
    }
    public static void Run(ModEntry entry)
    {
        Check(ExperienceSettings.StorageNaming && ModConfig.Instance.StorageEntry.Value, "Storage naming defaults on");
        string[] ids = { "makeshift_storage_bay", "storage_bay", "storage_bay_large", "storage_bay_gun",
            "storage_bay_chem", "machine_bay", "machine_bay_ext", "fridge", "smuggler_bay_mini", "smuggler_bay", "smuggler_bay_mod" };
        foreach (string id in ids)
        {
            var item = Storage(id, "化学原料 <b>甲</b>");
            item.Tags.Add("STORAGE_EXPANSION_TABS_INT"); // Expansion leaves the identifier unchanged.
            StorageNameValidatePatch.Postfix(item);
            var nodes = Nodes(item);
            Check(nodes.Count == 4, "Four overlay nodes for " + id);
            foreach (var node in nodes)
            {
                Check(node.text.text == GeneralHelper.GetCustomName(item), "Use native custom name");
                Check(!node.text.raycastTarget && !node.text.richText, "Plain text cannot capture mouse or markup");
                Check(node.rectTransform.sizeDelta.x == 44 && node.text.enableAutoSizing &&
                    node.text.overflowMode == TMPro.TextOverflowModes.Ellipsis, "Bounded long-name layout");
                Check(node.text.enabled == (node.parent == item.middle), "Normal icon only");
            }
            StorageNameValidatePatch.Postfix(item);
            Check(Nodes(item).Count == 4, "Repeated validation does not duplicate nodes");
            GeneralHelper.SetCustomName(item, "食品"); StorageNameSetPatch.Postfix(item);
            Check(Nodes(item)[0].text.text == "食品", "Rename refreshes immediately");
            item.background.image.enabled = false; item.fakeBackground.image.enabled = true;
            StorageNameVisibilityPatch.Postfix(item); StorageNameDragPatch.Postfix(item);
            foreach (var node in Nodes(item)) Check(node.text.enabled == (node.parent == item.fakeMiddle), "Drag visibility follows native sprite");
            GeneralHelper.RemoveCustomName(item); StorageNameClearPatch.Postfix(item);
            Check(Nodes(item).Count == 0, "Clearing name removes only our overlay");
            Check(item.handler != null && item.widthPixels == 48 && item.Tags.Contains("STORAGE_EXPANSION_TABS_INT"), "Original icon and expansion unchanged");
        }
        Console.WriteLine("PASS: eight native storage IDs and expansion tags, literal names, bounded overlays, rename/clear, idempotency and drag visibility.");

        foreach (string id in new[] { "water_tank", "storage_bay_expansion_kit", "storage_bay_unknown", "storage_coordinator" })
        {
            var other = Storage(id, "不应显示"); StorageNameOverlay.Refresh(other);
            Check(Nodes(other).Count == 0, "Unrelated item excluded: " + id);
        }
        var foreign = Storage("machine_bay", "Other owner"); foreign.Owned=false;
        StorageNameOverlay.Refresh(foreign); Check(Nodes(foreign).Count==0, "Non-player storage names are hidden");
        foreign.Owned=true; StorageNameOverlay.Refresh(foreign); Check(Nodes(foreign).Count==4, "Acquiring storage reveals its name");
        foreign.Owned=false; StorageNameOverlay.Refresh(foreign); Check(Nodes(foreign).Count==0, "Losing ownership removes existing name nodes");
        var unnamed = Storage("storage_bay", null); StorageNameOverlay.Refresh(unnamed);
        Check(Nodes(unnamed).Count == 0, "Unnamed storage excluded");
        GeneralHelper.SetCustomName(unnamed, "   "); StorageNameSetPatch.Postfix(unnamed);
        Check(Nodes(unnamed).Count == 0, "Whitespace name excluded");
        var debug = Storage("storage_bay", "Debug"); debug.isDebugMenu = true; StorageNameOverlay.Refresh(debug);
        Check(Nodes(debug).Count == 0, "Debug catalog excluded");

        var loaded = Storage("storage_bay_chem", "已有存档名称");
        PlayerStore.Instance.Items.Add(loaded); ModHook.Load();
        Check(Nodes(loaded).Count == 4, "Load event restores saved names");
        ModConfig.Instance.StorageEntry.Set(false); ModHook.Apply("unrelated.mod");
        Check(Nodes(loaded).Count == 4, "Other mod config does not refresh");
        ModHook.Apply(ModEntry.ModId);
        Check(Nodes(loaded).Count == 0 && ExperienceSettings.AutoOpenAppraisalGuide, "Storage toggle removes names independently");
        ModConfig.Instance.StorageEntry.Set(true); ModHook.Apply(ModEntry.ModId);
        Check(Nodes(loaded).Count == 4, "Toggle back restores existing names");
        entry.OnDisable();
        Check(Nodes(loaded).Count == 0 && GeneralHelper.GetCustomName(loaded) == "已有存档名称", "Disable removes overlay and preserves name");
        entry.OnEnable();
        Check(Nodes(loaded).Count == 4, "Enable restores overlay without reload");
        int createdBefore = TreeNodeRender.Created.Count;
        entry.OnEnable(); ModHook.Load();
        Check(TreeNodeRender.Created.Count == createdBefore, "Repeated enable/load is idempotent");
        StorageNameDestroyPatch.Prefix(loaded); loaded.Destroy();
        Check(Nodes(loaded).Count == 0, "Item destruction removes overlay");
        ModHook.Load(); Check(Nodes(loaded).Count == 0, "Destroyed storage stays excluded");
        entry.OnDisable();
        Console.WriteLine("PASS: strict scope, unnamed/debug exclusions, existing-save restoration, independent live toggle, disable/enable and destroy cleanup.");
    }
}
