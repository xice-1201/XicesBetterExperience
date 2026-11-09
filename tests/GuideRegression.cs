using System;
using System.Collections.Generic;
using XicesBetterExperience;

// Native interfaces are replaced only at the test boundary; all Mod source runs unchanged.
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type, string method) { }
        public HarmonyPatch(Type type, string method, Type[] arguments) { }
    }
}
namespace UnityEngine
{
    public static class Application { public static string version = "0.46D"; }
    public static class Debug { public static int Errors; public static void LogError(object error) { Errors++; } public static void Log(object message) { } public static void LogWarning(object message) { } }
}
public interface IMod { void Init(ModManifest manifest); void OnEnable(); void OnDisable(); }
public class ModManifest { public string ID; }
public class ModLog { public ModLog(ModManifest manifest) { } public void Log(object message) { } }
public class ConfigEntry<T> { public T Value; public void Set(T value) { Value = value; } }
public class ConfigEntry { public string Key; public object BoxedValue; }
public class ModConfig
{
    public static readonly ModConfig Instance = new ModConfig();
    public static string RequestedId;
    public int Definitions;
    public string Key;
    public Func<string> Label, Tip;
    public ConfigEntry<bool> Entry;
    public ConfigEntry<bool> StorageEntry;
    public ConfigEntry<bool> PageEntry;
    public ConfigEntry<bool> StolenEntry;
    public ConfigEntry<bool> ContrabandEntry;
    public ConfigEntry<bool> SingleCellEntry;
    public ConfigEntry<bool> ExchangeAutoPickupEntry;
    public ConfigEntry<bool> AutoDiscardReceiptEntry;
    public ConfigEntry<bool> PowerWarningEntry;
    public ConfigEntry<bool> MarketChangeEntry;
    public ConfigEntry<int> AutomationEntry;
    public int AutomationMin, AutomationMax;
    public string ModId { get { return ModEntry.ModId; } }
    public ConfigEntry<int> Define(string key, int value, string section, Func<string> label, int? min = null, int? max = null, Func<string> tooltip = null)
    {
        Definitions++; AutomationMin = min.Value; AutomationMax = max.Value;
        return AutomationEntry = new ConfigEntry<int> { Value = value };
    }
    public static ModConfig For(string id) { RequestedId = id; return Instance; }
    public ConfigEntry<bool> Define(string key, bool value, string section, Func<string> label, Func<string> tip)
    {
        Definitions++;
        if (key == "retain_document_page") return PageEntry = new ConfigEntry<bool> { Value = value };
        if (key == "storage_naming") return StorageEntry = new ConfigEntry<bool> { Value = value };
        if (key == "stolen_marker") return StolenEntry = new ConfigEntry<bool> { Value = value };
        if (key == "contraband_marker") return ContrabandEntry = new ConfigEntry<bool> { Value = value };
        if (key == "single_cell_value") return SingleCellEntry = new ConfigEntry<bool> { Value = value };
        if (key == "exchange_auto_pickup") return ExchangeAutoPickupEntry = new ConfigEntry<bool> { Value = value };
        if (key == "auto_discard_appraisal_receipt") return AutoDiscardReceiptEntry = new ConfigEntry<bool> { Value = value };
        if (key == "power_warning") return PowerWarningEntry = new ConfigEntry<bool> { Value = value };
        if (key == "market_change_marker") return MarketChangeEntry = new ConfigEntry<bool> { Value = value };
        if (key == "auto_open_appraisal_guide")
        {
            Key = key; Label = label; Tip = tip;
            return Entry = new ConfigEntry<bool> { Value = value };
        }
        return new ConfigEntry<bool> { Value = value };
    }
}
public static class ModHelper
{
    public static string Language = "zh";
    public static string GetLocalized(string id, string key) { return Language + ":" + id + ":" + key; }
}
public class GraphNodeStorage { public List<GraphNodeStorage> children = new List<GraphNodeStorage>(); }
public class GameItem : GraphNodeStorage
{
    public string identifier;
    public int uniqueId;
    public GameInventory parentInventory;
    public readonly List<GameItem> childItems = new List<GameItem>();
    public int unitCount = 1;
    public bool Owned;
    public bool Destroyed;
    public readonly HashSet<string> Tags = new HashSet<string>();
    public readonly Dictionary<string, int> IntTags = new Dictionary<string, int>();
    public bool IsTag(string tag) { return Tags.Contains(tag); }
    public List<string> GetGameItemType() { return new List<string>(Types); }
    public readonly List<string> Types = new List<string>();
    public bool IsDestroyed() { return Destroyed; }
    public List<GameItem> FindAllChildItems() { return childItems; }
}
public class StoreEvent
{
    public string identifier;
    public StoreEvent CreateAndAddSlip(int timer) { return this; }
}
public class StoreEventManager
{
    public void ActivateEvent(StoreEvent e, bool debugLog = false, bool force = false) { }
    public void DeactivateEvent(StoreEvent e) { }
    public void OnDayEnd() { }
}
public static class CommissaryData
{
    public static readonly Dictionary<string, int> Modifiers = new Dictionary<string, int>();
    public static int GetTotalTypeMarketPriceModifier(string type) { return Modifiers.ContainsKey(type) ? Modifiers[type] : 0; }
}
public static class UISettings
{
    public static UISettingsData current = new UISettingsData();
}
public class UISettingsData { public int gridSize = 48; }
public static class RenderHandler
{
    public static object LoadFromAtlas(string atlas, string name) { return name; }
}
public class GameItemElement : GameItem
{
    public bool isDebugMenu;
    public int widthPixels = 48, heightPixels = 64;
    public TreeNodeRender handler = new TreeNodeRender();
    public TreeNodeRender middle = new TreeNodeRender(), fakeMiddle = new TreeNodeRender();
    public TreeNodeRender rotator { get { return middle; } }
    public TreeNodeRender fakeRotator { get { return fakeMiddle; } }
    public TreeNodeRender background = new TreeNodeRender(), fakeBackground = new TreeNodeRender();
    public GameItemElement() { background.image.enabled = true; }
    public void ValidateSelect(bool updateBackground = true) { }
    public void ToggleBackground(bool enabled, float opacity = 1, float lightness = 1) { }
    public void ToggleFakeBackground(bool enabled, UnityEngine.Vector4 color) { }
    public void UpdateRectTransform() { }
    public void Destroy() { Destroyed = true; }
}
public class StolenHelper
{
    public static bool IsStolenItem(GameItem item) { return item != null && item.IsTag("STOLEN_TAG"); }
    public static int GetFoundChance(GameItem item) { return item.IntTags.ContainsKey("STOLEN_VALUE_INT") ? item.IntTags["STOLEN_VALUE_INT"] : 0; }
}
public static class ContrabandHelper
{
    public static bool IsContraband(GameItem item) { return item != null && (item.IsTag("CONTRABAND_LEVEL_LOW") || item.IsTag("CONTRABAND_LEVEL_MID") || item.IsTag("CONTRABAND_LEVEL_HIGH") || item.IsTag("CONTRABAND_LEVEL_CRITICAL")); }
}
public static class GeneralHelper
{
    public static bool IsItemOwned(GameItem item) { return item.Owned; }
    public static bool HasCustomName(GameItem item) { return item.Tags.Contains("CUSTOM_NAME_TAG"); }
    public static readonly Dictionary<GameItem, string> Names = new Dictionary<GameItem, string>();
    public static string GetCustomName(GameItem item) { return Names.ContainsKey(item) ? Names[item] : null; }
    public static void SetCustomName(GameItem item, string name) { item.Tags.Add("CUSTOM_NAME_TAG"); Names[item] = name; }
    public static void RemoveCustomName(GameItem item) { item.Tags.Remove("CUSTOM_NAME_TAG"); Names.Remove(item); }
}
public class PlayerStore
{
    public Dictionary<string, string> modData = new Dictionary<string, string>();
    public enum StoreState { MORNING, OPEN, CLOSED, NIGHT }
    public bool isAppraisalIntroduced = true;
    public int appraiserLevel = 3;
    public StoreState storeState;
    public TestClient currentClientInstance;
    public static PlayerStore Instance;
    public readonly List<GameItem> Items = new List<GameItem>();
    public bool Fail;
    public ExchangeManager exchangeManager = new ExchangeManager();
    public void AddDirectSellingItemToTable(GameItem item, bool a, bool b, bool c, int d)
    {
        if (item != null && item.parentInventory == null) Items.Add(item);
    }
    public void EndNight() { }
    public void ClearTrash() { }
    public static bool IsInstanceExist() { return Instance != null; }
    public List<GameItem> FindAllItem(bool isOwn = true)
    {
        if (Fail) throw new InvalidOperationException("Simulated inventory failure");
        // Deliberately return unowned entries too, verifying the explicit ownership check.
        return Items;
    }
}
public class EmporiumEntry
{
    public static EmporiumEntry Instance;
    public GameGridInventory frontInvinvElement, backInvinvElement, backInvinvElementCounter, invElement, showcaseElement;
    public GameItem trashcan;
    public GameGridInventory trashcanInvElement;
    public PixelWindow frontInvWindow = new PixelWindow(), backInvWindow = new PixelWindow();
    public int ValidateCalls;
    public void Validate(bool force = true)
    {
        ValidateCalls++;
    }
    public void OnArriveStore(bool fromSleep = false) { }
}
public class AfterhourLocation { }
public class MapUIManager
{
    public static MapUIManager Instance;
    public bool isOutside;
    public AfterhourLocation Current { get; set; }
    public void TransferBackInvToCounter()
    {
        if (EmporiumEntry.Instance == null || EmporiumEntry.Instance.backInvinvElement == null ||
            EmporiumEntry.Instance.backInvinvElementCounter == null) return;
        var back = EmporiumEntry.Instance.backInvinvElement;
        var counter = EmporiumEntry.Instance.backInvinvElementCounter;
        foreach (GameItem item in new List<GameItem>(back.childItems))
        {
            back.Expel(item);
            counter.UncheckedAccept(item);
        }
    }
}
public class PixelWindow
{
    public int widthPixels = 600, heightPixels = 160;
    public UnityEngine.Vector2 anchoredPosition;
}
public class GameInventory : GraphNodeStorage
{
    public List<GameItem> childItems = new List<GameItem>();
    public virtual GameItem TryAcceptAll(GameItem item)
    { childItems.Add(item); children.Add(item); item.parentInventory = this; return item; }
    public virtual bool Expel(GameItem item)
    { bool removed = childItems.Remove(item); if (removed) { children.Remove(item); item.parentInventory = null; } return removed; }
    public virtual bool UncheckedAccept(GameItem item)
    { if (item == null || item.parentInventory != null) return false; childItems.Add(item); children.Add(item); item.parentInventory = this; return true; }
    public virtual GameItem GetParentItem() { return null; }
}
public class GameGridInventory : GameInventory
{
    public bool CanAccept = true;
    public override GameItem TryAcceptAll(GameItem item)
    { if (!CanAccept) return null; return base.TryAcceptAll(item); }
}
public class ExchangeManager { public List<GameItem> itemsBuffer = new List<GameItem>(); }
public static class GraphUtils
{
    public static int Deliveries;
    public static GraphNodeStorage LastTarget;
    public static int TryAcceptAll(GraphNodeStorage target, GameItem item, int amount = -1)
    { Deliveries++; LastTarget = target; return target is GameGridInventory && ((GameGridInventory)target).CanAccept ? 0 : 1; }
    public static bool TryAcceptAllOrDestroy(GraphNodeStorage target, GameItem item, string context = null)
    {
        Deliveries++; LastTarget = target;
        if (item != null && item.parentInventory != null) item.parentInventory.Expel(item);
        if (target != null && item != null) target.children.Add(item);
        return true;
    }
    public static bool TryAddInto(GraphNodeStorage target, GameItem item)
    { Deliveries++; LastTarget = target; if (!(target is GameGridInventory) || !((GameGridInventory)target).CanAccept) return false; ((GameGridInventory)target).TryAcceptAll(item); return true; }
}
public static class HealthData { public static bool Readable = true; public static bool CanRead() { return Readable; } }
public class InspectionUIManager
{
    public GameItem currentInspectingItem;
    public void OnInspectItem(GameItem item) { currentInspectingItem = item; }
    public void CloseUI() { currentInspectingItem = null; }
}
public class ActiveObject { public bool activeSelf; public TestTransform transform = new TestTransform(); }
public class TestTransform { public int childCount = 8; }
public class MultiPageUIManager
{
    public static MultiPageUIManager Instance;
    public ActiveObject panel = new ActiveObject();
    public List<ActiveObject> books = new List<ActiveObject>();
    public int Opens, Closes, LastIndex = -1, Page;
    public int DisplayedPage, ButtonsUpdated;
    private int currentPage, currentBookIndex;
    private void ShowPage(int page)
    {
        DisplayedPage = page;
        DocumentPageShownPatch.Postfix(page, currentBookIndex);
    }
    private void UpdateButtons() { ButtonsUpdated++; }
    public void RememberedOpen(int index)
    {
        int state;
        DocumentPageOpenPatch.Prefix(index, out state);
        OpenUI(index); currentBookIndex = index; currentPage = 0;
        ShowPage(0);
        DocumentPageOpenPatch.Postfix(this, index, state, ref currentPage);
        Page = currentPage;
    }
    public void TurnTo(int page) { currentPage = page; Page = page; ShowPage(page); }
    public void RememberedClose()
    {
        DocumentPageClosePatch.Prefix(this, currentBookIndex, currentPage);
        CloseUI();
    }
    public MultiPageUIManager() { for (int i = 0; i < 8; i++) books.Add(new ActiveObject()); }
    public void OpenUI(int index)
    {
        foreach (var book in books) book.activeSelf = false;
        panel.activeSelf = true; books[index].activeSelf = true; LastIndex = index; Page = 0; Opens++;
    }
    public void CloseUI()
    {
        Closes++; panel.activeSelf = false;
        foreach (var book in books) book.activeSelf = false;
    }
}
public static class GuideRegression
{
    private static readonly ModEntry Entry = new ModEntry();
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static GameItem Item(string tag) { var item = new GameItem(); item.Tags.Add(tag); return item; }
    private static GameItemElement Guide(string id, bool owned = true, bool destroyed = false)
    { return new GameItemElement { identifier = id, Owned = owned, Destroyed = destroyed }; }
    private static void EndInspection(GameItem item)
    {
        var inspection = new InspectionUIManager { currentInspectingItem = item };
        int state;
        AutoCloseAppraisalGuide.Prefix(inspection, out state);
        inspection.CloseUI();
        Check(inspection.currentInspectingItem == null, "Native close clears inspected item");
        AutoCloseAppraisalGuide.Postfix(state);
    }
    private static MultiPageUIManager Reset()
    {
        Entry.OnEnable(); ModConfig.Instance.Entry.Set(true); HealthData.Readable = true;
        PlayerStore.Instance = new PlayerStore(); EmporiumEntry.Instance = new EmporiumEntry();
        UnityEngine.Debug.Errors = 0;
        return MultiPageUIManager.Instance = new MultiPageUIManager();
    }
    public static void Main()
    {
        try
        {
        bool rejected = false;
        UnityEngine.Application.version = "0.46E";
        try { Entry.Init(new ModManifest { ID = ModEntry.ModId }); }
        catch (NotSupportedException) { rejected = true; }
        Check(rejected && ModConfig.Instance.Definitions == 0, "Reject incompatible game before registering config");
        UnityEngine.Application.version = "0.46D";
        Entry.Init(new ModManifest { ID = ModEntry.ModId });
        Entry.Init(new ModManifest { ID = ModEntry.ModId });
        Check(ModConfig.RequestedId == ModEntry.ModId && ModConfig.Instance.Definitions == 11 &&
            ModConfig.Instance.Key == "auto_open_appraisal_guide" && ExperienceSettings.AutoOpenAppraisalGuide,
            "Ten independent native config entries");
        Check(ModConfig.Instance.Label().StartsWith("zh:"), "Chinese config label");
        ModHelper.Language = "en";
        Check(ModConfig.Instance.Label().StartsWith("en:") && ModConfig.Instance.Tip().StartsWith("en:"), "Lazy localized label and tooltip");
        Console.WriteLine("PASS: strict version guard, idempotent default-on native configuration and lazy localization.");

        string[] tags = { "HAND_TAG", "RIBWICH_TAG", "PLASMA_CELL_TAG" };
        string[] ids = { "hand_guide", "ribwich_guide", "fuel_guide" };
        int[] indices = { 6, 4, 0 };
        for (int i = 0; i < tags.Length; i++)
        {
            var ui = Reset();
            PlayerStore.Instance.Items.Add(null);
            PlayerStore.Instance.Items.Add(Guide(ids[i], false));
            PlayerStore.Instance.Items.Add(Guide(ids[i], true, true));
            AutoOpenAppraisalGuide.Postfix(Item(tags[i]));
            Check(ui.Opens == 0, "Unowned/destroyed guide rejected");
            var owned = Guide(ids[i]); PlayerStore.Instance.Items.Add(owned);
            PlayerStore.Instance.Items.Add(Guide(ids[i]));
            var inspected = Item(tags[i]); // The inspected item may belong to a customer.
            AutoOpenAppraisalGuide.Postfix(inspected);
            Check(ui.Opens == 1 && ui.LastIndex == indices[i], "Correct owned guide opens once");
            ui.Page = 3;
            AutoOpenAppraisalGuide.Postfix(inspected);
            Check(ui.Opens == 1 && ui.Page == 3, "Keep matching guide page");
            ui.panel.activeSelf = false;
            AutoOpenAppraisalGuide.Postfix(inspected);
            Check(ui.Opens == 2 && ui.Page == 0, "Closed guide reopens at native first page");
            PlayerStore.Instance.Items.Clear(); owned.Owned = false; PlayerStore.Instance.Items.Add(owned);
            ui.panel.activeSelf = false;
            AutoOpenAppraisalGuide.Postfix(inspected);
            Check(ui.Opens == 2, "Sold guide is not cached or reopened");
        }
        Console.WriteLine("PASS: all three mappings, customer inspections, ownership/destruction, duplicate copies, page preservation and sales.");

        var manager = Reset();
        PlayerStore.Instance.Items.Add(Guide("ribwich_guide"));
        AutoOpenAppraisalGuide.Postfix(Item("HAND_TAG"));
        AutoOpenAppraisalGuide.Postfix(Item("CIGARETTE_TAG"));
        AutoOpenAppraisalGuide.Postfix(null);
        Check(manager.Opens == 0, "Wrong guide, unrelated inspection and null ignored");
        PlayerStore.Instance.Items.Add(Guide("hand_guide"));
        ModConfig.Instance.Entry.Set(false); AutoOpenAppraisalGuide.Postfix(Item("HAND_TAG"));
        Check(manager.Opens == 0, "Live config disable");
        ModConfig.Instance.Entry.Set(true); Entry.OnDisable(); Entry.OnDisable();
        AutoOpenAppraisalGuide.Postfix(Item("HAND_TAG")); Check(manager.Opens == 0, "Mod disable");
        Entry.OnEnable(); Entry.OnEnable();
        HealthData.Readable = false; AutoOpenAppraisalGuide.Postfix(Item("HAND_TAG"));
        Check(manager.Opens == 0, "Native reading restrictions");
        HealthData.Readable = true;
        AutoOpenAppraisalGuide.Postfix(Item("HAND_TAG"));
        manager.Page = 2;
        AutoOpenAppraisalGuide.Postfix(Item("RIBWICH_TAG"));
        Check(manager.Opens == 2 && manager.LastIndex == 4 && manager.Page == 0, "Switch to correct guide");
        Entry.OnDisable(); Check(manager.panel.activeSelf, "Disable leaves existing guide open");
        Console.WriteLine("PASS: missing/wrong guide, unrelated items, live config, enable/disable, native reading restrictions and guide switching.");

        manager = Reset(); PlayerStore.Instance.Items.Add(Guide("hand_guide"));
        MultiPageUIManager.Instance = null; AutoOpenAppraisalGuide.Postfix(Item("HAND_TAG"));
        MultiPageUIManager.Instance = manager;
        EmporiumEntry.Instance = null; AutoOpenAppraisalGuide.Postfix(Item("HAND_TAG"));
        EmporiumEntry.Instance = new EmporiumEntry();
        var store = PlayerStore.Instance; PlayerStore.Instance = null;
        AutoOpenAppraisalGuide.Postfix(Item("HAND_TAG")); PlayerStore.Instance = store;
        Check(manager.Opens == 0 && UnityEngine.Debug.Errors == 0, "Unavailable scene/UI safely skipped");
        store.Fail = true; AutoOpenAppraisalGuide.Postfix(Item("HAND_TAG"));
        Check(UnityEngine.Debug.Errors == 1 && manager.Opens == 0, "Failure logged without propagating into inspection");
        Console.WriteLine("PASS: absent scene/UI/store and exception isolation. Native Unity/Harmony execution requires in-game verification.");

        for (int i = 0; i < tags.Length; i++)
        {
            manager = Reset(); PlayerStore.Instance.Items.Add(Guide(ids[i]));
            AutoOpenAppraisalGuide.Postfix(Item(tags[i]));
            EndInspection(Item(tags[i]));
            Check(manager.Closes == 1 && !manager.panel.activeSelf, "Close matching guide after native inspection clears item");
            EndInspection(Item(tags[i]));
            Check(manager.Closes == 1, "Do not repeat close/audio");
            manager.OpenUI(indices[i]); // A matching guide opened manually also closes.
            EndInspection(Item(tags[i]));
            Check(manager.Closes == 2, "Close manually opened matching guide");
            manager.OpenUI(7); manager.Page = 2;
            EndInspection(Item(tags[i]));
            Check(manager.Closes == 2 && manager.panel.activeSelf && manager.Page == 2, "Keep unrelated document and page");
        }
        Console.WriteLine("PASS: all three end-of-inspection mappings, captured native context, manual guides, already-closed guides and unrelated documents.");

        manager = Reset(); manager.OpenUI(6);
        EndInspection(Item("CIGARETTE_TAG")); EndInspection(null);
        Check(manager.Closes == 0, "Unrelated/null inspection leaves guide open");
        ModConfig.Instance.Entry.Set(false); EndInspection(Item("HAND_TAG"));
        Check(manager.Closes == 0, "Disabled config prevents auto-close");
        ModConfig.Instance.Entry.Set(true); Entry.OnDisable(); EndInspection(Item("HAND_TAG"));
        Check(manager.Closes == 0, "Disabled mod prevents auto-close");
        Entry.OnEnable(); MultiPageUIManager.Instance = null; EndInspection(Item("HAND_TAG"));
        Check(UnityEngine.Debug.Errors == 0, "Unavailable UI close is safe");
        MultiPageUIManager.Instance = manager;
        manager.books.Clear(); EndInspection(Item("HAND_TAG"));
        Check(UnityEngine.Debug.Errors == 1, "Close failure logged without breaking native close");
        Console.WriteLine("PASS: auto-close config/mod gates, unrelated inspections, missing UI and exception isolation.");
        PlayerStore.Instance = new PlayerStore();
        StorageNamingRegression.Run(Entry);
        StolenHeatRegression.Run(Entry);
        ContrabandMarkerRegression.Run(Entry);
        DocumentPageRegression.Run(Entry);
        AppraisalAutomationRegression.Run(Entry);
        ExchangeAutoPickupRegression.Run(Entry);
        AutoDiscardAppraisalReceiptRegression.Run(Entry);
        PowerWarningRegression.Run(Entry);
        MarketChangeRegression.Run(Entry);
        }
        catch (Exception error)
        {
            Console.WriteLine("FAIL: " + error.GetType().FullName + " " + error.Message);
            Environment.Exit(1);
        }
    }
}
