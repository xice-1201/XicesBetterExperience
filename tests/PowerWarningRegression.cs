using System;
using System.Collections.Generic;
using XicesBetterExperience;

public class GameSlotInventory : GameInventory
{
    public GameItem childItem, ParentItem;
    public override GameItem GetParentItem() { return ParentItem; }
}

public static class MachineHelper
{
    public const string MACHINE_RECHARGABLE_TAG = "MACHINE_RECHARGABLE_TAG";
    public static readonly Dictionary<GameItem, GameSlotInventory> Slots = new Dictionary<GameItem, GameSlotInventory>();
    public static readonly HashSet<GameItem> Powered = new HashSet<GameItem>();
    public static GameSlotInventory GetBatterySlot(GameItem item) { GameSlotInventory slot; return item != null && Slots.TryGetValue(item, out slot) ? slot : null; }
    public static bool CanPower(GameItem item, GameSlotInventory slot) { return item != null && slot != null && Powered.Contains(item); }
}

public static class MachineryHelper
{
    public static readonly HashSet<GameItem> On = new HashSet<GameItem>();
    public static bool IsMachineryToggleOn(GameItem item) { return item != null && On.Contains(item); }
}

public class CustomUIManager
{
    public static CustomUIManager Instance;
    public object spriteWarning = new object();
}

public static class PowerWarningRegression
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static List<TreeNodeRender> Nodes(GameItemElement item)
    {
        return TreeNodeRender.Created.FindAll(node => !node.gameObject.Destroyed &&
            (node.parent == item.middle || node.parent == item.fakeMiddle));
    }

    public static void Run(ModEntry entry)
    {
        CustomUIManager.Instance = new CustomUIManager();
        entry.OnEnable();
        Check(ExperienceSettings.PowerWarning && ModConfig.Instance.PowerWarningEntry.Value, "Power warning defaults on");
        var item = new GameItemElement { Owned = true };
        item.Tags.Add("MACHINE_RECHARGABLE_TAG");
        var slot = new GameSlotInventory();
        slot.ParentItem = item;
        MachineHelper.Slots[item] = slot;
        PowerWarningValidatePatch.Postfix(item);
        Check(Nodes(item).Count == 1 && Nodes(item).Exists(n => n.parent == item.middle && n.image.enabled), "Empty battery shows one warning");
        Check(Nodes(item)[0].image.type == UnityEngine.UI.Image.Type.Simple &&
            Nodes(item)[0].image.fillAmount == 1f && Nodes(item)[0].image.preserveAspect, "Warning uses unfilled simple sprite rendering");
        item.fakeBackground.image.enabled = true;
        PowerWarningDragPatch.Postfix(item);
        Check(Nodes(item).Count == 1 && Nodes(item).Exists(n => n.parent == item.middle && n.image.enabled), "Drag preview does not duplicate warning");
        item.fakeBackground.image.enabled = false;
        PowerWarningDragPatch.Postfix(item);
        slot.childItem = new GameItem();
        MachineHelper.Powered.Add(item);
        PowerWarningBatteryInsertPatch.Postfix(slot);
        Check(Nodes(item).TrueForAll(n => !n.image.enabled), "Sufficient battery hides warning");
        MachineHelper.Powered.Remove(item);
        PowerWarningBatteryRemovePatch.Postfix(slot);
        Check(Nodes(item).Exists(n => n.image.enabled), "Insufficient battery restores warning");
        item.Tags.Add("machinery");
        MachineryHelper.On.Add(item);
        MachineHelper.Powered.Add(item);
        PowerWarningMarker.Refresh(item);
        Check(Nodes(item).TrueForAll(n => !n.image.enabled), "Powered machinery hides warning");
        MachineryHelper.On.Remove(item);
        PowerWarningMarker.Refresh(item);
        Check(Nodes(item).Exists(n => n.image.enabled), "Disabled machinery shows warning");
        ModConfig.Instance.PowerWarningEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(Nodes(item).TrueForAll(n => !n.image.enabled), "Config disables warning");
        PowerWarningDestroyPatch.Prefix(item); Check(Nodes(item).Count == 0, "Destroy cleans warning");
        entry.OnDisable();
        Console.WriteLine("PASS: power warning ownership, battery state, machinery toggle, config and cleanup.");
    }
}
