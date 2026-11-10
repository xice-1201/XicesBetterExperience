using System;
using System.Collections.Generic;
using XicesBetterExperience;

public class TagState { public int Value; public int GetInt() { return Value; } }

public class Liquid
{
    public string identifier;
    public bool isEvaporative;
    public static readonly List<Liquid> Liquids = new List<Liquid> {
        new Liquid { identifier = "water", isEvaporative = true },
        new Liquid { identifier = "mineral", isEvaporative = false },
        new Liquid { identifier = "alcohol", isEvaporative = true },
        new Liquid { identifier = "lipobond_agent", isEvaporative = true }
    };
    public static string GetCurrentPartFromId(string id) { return "CURRENT_PART_" + id.ToUpperInvariant(); }
    public bool IsLiquid(string id) { return identifier == id; }
}

public static class MachineMoistureFarm
{
    public static int Performance, Sharing = 1, CountCalls;
    public static int GetOutputVolume(GameItem farm) { return 850000 * (100 + Performance) / 100; }
    public static int CountRunningFarms() { CountCalls++; return Sharing; }
    public static bool CanRunTonight(GameItem farm) { return true; }
    public static int ApplySharedAir(int volume, int count) { return (int)Math.Round(volume / (1.0 + .25 * (count - 1))); }
}

public static class WaterHelper
{
    public static int PurityReads;
    public static int GetFreeCapacity(GameItem item) { return item.IntTags["free"]; }
    public static int GetWaterPurity(GameItem item) { PurityReads++; return item.IntTags.ContainsKey("purity") ? item.IntTags["purity"] : 0; }
}

public static class MachinePurifier
{
    public static int GetMaxPurityCap(int quality)
    {
        double mult = quality >= 200 ? 0 : quality >= 100 ? .1 : quality >= 75 ? .2 :
            quality >= 50 ? .35 : quality >= 25 ? .65 : quality >= 15 ? .85 : 1;
        return 10000 - (int)Math.Round(99 * mult);
    }
}

public static class OverflowWarningRegression
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static List<TreeNodeRender> Nodes(GameItemElement item)
    {
        return TreeNodeRender.Created.FindAll(n => !n.gameObject.Destroyed && n.parent == item.middle &&
            n.gameObject.name == "XicesBetterExperience.OverflowWarning");
    }
    private static bool Visible(GameItemElement item) { return Nodes(item).Exists(n => StatusTest.Graphic(n).enabled); }

    public static void Run(ModEntry entry)
    {
        ModConfig.Instance.PowerWarningEntry.Set(true);
        entry.OnEnable();
        Check(ExperienceSettings.OverflowWarning && ExperienceSettings.SettingsTab("overflow_warning") == "icons", "Overflow defaults on in icons tab");
        var farm = new GameItemElement { Owned = true, identifier = "moisture_farm" };
        var slot = new GameSlotInventory { ParentItem = farm };
        MachineHelper.WaterSlots[farm] = slot;
        Check(OverflowWarningMarker.NeedsWarning(farm), "Missing canister warns");
        farm.Owned = false; Check(!OverflowWarningMarker.NeedsWarning(farm), "NPC machine excluded"); farm.Owned = true;
        var canister = new GameItem { parentInventory = slot };
        slot.childItem = canister;
        canister.IntTags["free"] = 850000;
        Check(!OverflowWarningMarker.NeedsWarning(farm), "Exact fit does not warn");
        canister.IntTags["free"] = 849999;
        Check(OverflowWarningMarker.NeedsWarning(farm), "One microliter short warns");
        canister.IntTags["free"] = 0;
        Check(OverflowWarningMarker.NeedsWarning(farm), "Full canister warns");
        canister.IntTags["free"] = 900000;
        Check(!OverflowWarningMarker.NeedsWarning(farm), "Empty large canister has capacity");
        MachineMoistureFarm.Performance = 100;
        Check(OverflowWarningMarker.NeedsWarning(farm), "Performance raises capacity requirement");
        MachineMoistureFarm.Performance = 0; canister.IntTags["free"] = 700000;
        MachineMoistureFarm.Sharing = 2;
        OverflowWarningMarker.NotifyChanged(farm); UnityEngine.Canvas.Render();
        Check(!OverflowWarningMarker.NeedsWarning(farm), "Shared air lowers output threshold");
        MachineMoistureFarm.Sharing = 1;
        OverflowWarningMarker.NotifyChanged(farm); UnityEngine.Canvas.Render();
        OverflowWarningMarker.Refresh(farm);
        Check(Visible(farm) && Nodes(farm).Count == 1, "One overflow mesh shows");
        var node = Nodes(farm)[0]; var mesh = StatusTest.Graphic(node);
        Check(!mesh.raycastTarget && !node.image.enabled && node.rectTransform.pivot.x == .5f && mesh.rectTransform.pivot.y == .5f,
            "Centered noninteractive vector mesh replaces Image");
        Check(mesh.Triangles.TrueForAll(t => t.Color.a == 0.78f), "Body, details and X share the same translucent alpha");
        Check(mesh.Triangles.TrueForAll(t => t.Color.g < 0.6f), "X has no pale backing tile");
        farm.Tags.Add("MACHINE_RECHARGABLE_TAG");
        var battery = new GameSlotInventory { ParentItem = farm }; MachineHelper.Slots[farm] = battery;
        PowerWarningMarker.Refresh(farm);
        Check(!Visible(farm), "Enabled power warning takes priority");
        MachineHelper.Powered.Add(farm);
        PowerWarningBatteryInsertPatch.Postfix(battery);
        Check(Visible(farm), "Restoring power immediately reveals overflow");
        MachineHelper.Powered.Remove(farm);
        ModConfig.Instance.PowerWarningEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(Visible(farm), "Disabled power warning cannot suppress overflow");
        canister.IntTags["free"] = 1000000;
        OverflowWarningMarker.EmptyPatch.Postfix(canister);
        UnityEngine.Canvas.Render();
        Check(!Visible(farm), "Liquid removal refreshes parent farm");
        canister.IntTags["free"] = 1;
        OverflowWarningMarker.TransferPatch.Postfix(null, canister);
        UnityEngine.Canvas.Render();
        Check(Visible(farm), "Liquid addition refreshes parent farm");
        MachineMoistureFarm.Sharing = 2; canister.IntTags["free"] = 700000;
        int countBefore = MachineMoistureFarm.CountCalls;
        OverflowWarningMarker.NotifyChanged(farm);
        OverflowWarningMarker.NotifyChanged(canister);
        OverflowWarningMarker.NotifyChanged(farm);
        Check(UnityEngine.Canvas.Subscriptions == 1 && MachineMoistureFarm.CountCalls == countBefore, "Mutation burst queues one deferred refresh");
        UnityEngine.Canvas.Render();
        Check(!Visible(farm) && MachineMoistureFarm.CountCalls == countBefore + 1, "Batch recomputes shared output once");
        Check(UnityEngine.Canvas.Subscriptions == 0, "Refresh unsubscribes after processing changes");
        for (int i = 0; i < 100; i++) UnityEngine.Canvas.Render();
        for (int i = 0; i < 100; i++) OverflowWarningMarker.Refresh(farm);
        Check(MachineMoistureFarm.CountCalls == countBefore + 1 && UnityEngine.Canvas.Subscriptions == 0, "Idle frames and unchanged icons never scan inventories");
        MachineMoistureFarm.Sharing = 1;
        OverflowWarningMarker.NotifyTagChanged(farm, "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT");
        UnityEngine.Canvas.Render(); Check(Visible(farm), "Relevant tag mutation refreshes shared output without reopening inventory");
        OverflowWarningMarker.NotifyTagChanged(farm, "UNRELATED_TAG");
        Check(UnityEngine.Canvas.Subscriptions == 0, "Unrelated tags do not schedule work");
        farm.background.image.enabled = false; OverflowWarningMarker.Refresh(farm);
        Check(!Visible(farm), "Hidden native item hides warning");
        farm.background.image.enabled = true; OverflowWarningMarker.Refresh(farm);
        ModConfig.Instance.OverflowWarningEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(!Visible(farm), "Config disables existing mesh");
        ModConfig.Instance.OverflowWarningEntry.Set(true); ModHook.Apply(ModEntry.ModId);
        Check(Visible(farm), "Config re-enables mesh");
        OverflowWarningMarker.DestroyPatch.Prefix(farm); Check(Nodes(farm).Count == 0, "Destroy removes mesh");
        MachineMoistureFarm.Sharing = 1;
        entry.OnDisable();
        Check(UnityEngine.Canvas.Subscriptions == 0, "Disable cancels pending render callbacks");
        ModConfig.Instance.PowerWarningEntry.Set(true);
        RunPurifier(entry);
        RunEvaporator(entry);
        Console.WriteLine("PASS: overflow boundaries, ownership, shared air, power priority, event batching, zero idle scans and cleanup.");
    }

    private static void RunEvaporator(ModEntry entry)
    {
        entry.OnEnable();
        var machine = new GameItemElement { Owned = true, identifier = "evaporator" };
        var inputSlot = new GameSlotInventory { ParentItem = machine };
        var outputSlot = new GameSlotInventory { ParentItem = machine };
        MachineHelper.WaterSlots[machine] = inputSlot; MachineHelper.OutputSlots[machine] = outputSlot;
        machine.IntTags["quality"] = 100;
        Check(!OverflowWarningMarker.NeedsWarning(machine), "No input means no evaporator harvest warning");
        var input = new GameItem { parentInventory = inputSlot }; inputSlot.childItem = input;
        input.IntTags["CURRENT_PART_MINERAL"] = 900000;
        Check(!OverflowWarningMarker.NeedsWarning(machine), "Nonvolatile input yields zero");
        input.IntTags["CURRENT_PART_WATER"] = 1000000;
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 300000, "Base evaporation is 300mL");
        Check(OverflowWarningMarker.NeedsWarning(machine), "Positive harvest and missing output canister warns");
        machine.IntTags["quality"] = 0;
        Check(!OverflowWarningMarker.NeedsWarning(machine), "Zero quality produces no warning despite missing canister");
        machine.IntTags["quality"] = -50;
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 0, "Negative quality clamps to zero");
        machine.IntTags["quality"] = 200;
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 300000, "Quality clamps to 100 percent");
        machine.IntTags["quality"] = 50; machine.IntTags["performance"] = 50;
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 225000, "Performance and quality combine");
        input.IntTags["CURRENT_PART_WATER"] = 100000;
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 50000, "Raw material limits harvest");
        machine.IntTags["performance"] = -100;
        Check(!OverflowWarningMarker.NeedsWarning(machine), "Zero performance output never warns");
        machine.IntTags["performance"] = -200;
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 0, "Negative performance clamps to zero");
        machine.IntTags["performance"] = 0; machine.IntTags["quality"] = 100;
        input.IntTags["CURRENT_PART_WATER"] = 800000; input.IntTags["CURRENT_PART_ALCOHOL"] = 200000;
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 300000, "Mixed volatiles sum to total harvest");
        var moduleGrid = new GameGridInventory { Owner = machine };
        var solvent = new GameItem { identifier = "evaporator_module_solvent_recovery", parentInventory = moduleGrid };
        MachineHelper.Modules[machine] = new List<GameItem> { solvent };
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 60000, "Solvent module filters after proportional allocation, not before");
        input.IntTags["CURRENT_PART_ALCOHOL"] = 0;
        Check(!OverflowWarningMarker.NeedsWarning(machine), "Solvent module with no alcohol yields zero");
        MachineHelper.Modules[machine].Clear();
        input.IntTags["CURRENT_PART_WATER"] = 1; input.IntTags["CURRENT_PART_ALCOHOL"] = 1;
        machine.IntTags["quality"] = 50;
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 0, "Native per-liquid midpoint rounding can yield zero total");
        input.IntTags["CURRENT_PART_WATER"] = 2; input.IntTags["CURRENT_PART_ALCOHOL"] = 3;
        Check(OverflowWarningMarker.EstimateEvaporatorHarvest(machine) == 3, "Native per-liquid rounding retained");
        machine.IntTags["quality"] = 100;
        input.IntTags["CURRENT_PART_WATER"] = 1000000; input.IntTags["CURRENT_PART_ALCOHOL"] = 0;
        var output = new GameItem { parentInventory = outputSlot }; outputSlot.childItem = output;
        output.IntTags["free"] = 300000;
        Check(!OverflowWarningMarker.NeedsWarning(machine), "Exact output capacity fits");
        output.IntTags["free"] = 299999;
        Check(OverflowWarningMarker.NeedsWarning(machine), "One microliter shortage warns");
        output.IntTags["free"] = 400000;
        Check(!OverflowWarningMarker.NeedsWarning(machine), "Sufficient output capacity does not warn");
        machine.Owned = false; outputSlot.childItem = null;
        Check(!OverflowWarningMarker.NeedsWarning(machine), "Unowned evaporator excluded"); machine.Owned = true;
        int scans = MachineMoistureFarm.CountCalls;
        OverflowWarningMarker.Refresh(machine); UnityEngine.Canvas.Render();
        Check(Visible(machine), "Evaporator missing output shows icon");
        outputSlot.childItem = output; OverflowWarningMarker.SlotInsertPatch.Postfix(outputSlot); UnityEngine.Canvas.Render();
        Check(!Visible(machine), "Inserting sufficient output container clears warning");
        output.IntTags["free"] = 1; OverflowWarningMarker.NotifyTagChanged(output, "CURRENT_PART_WATER"); UnityEngine.Canvas.Render();
        Check(Visible(machine), "Changing output liquid refreshes warning");
        MachineHelper.Modules[machine].Add(solvent);
        OverflowWarningMarker.GridInsertPatch.Postfix(moduleGrid, solvent); UnityEngine.Canvas.Render();
        Check(!Visible(machine), "Solvent module insertion removes zero-harvest warning");
        MachineHelper.Modules[machine].Clear();
        OverflowWarningMarker.GridRemovePatch.Postfix(moduleGrid, solvent); UnityEngine.Canvas.Render();
        Check(Visible(machine), "Solvent module removal restores positive harvest warning");
        inputSlot.childItem = null; OverflowWarningMarker.SlotRemovePatch.Postfix(inputSlot); UnityEngine.Canvas.Render();
        Check(!Visible(machine), "Removing input clears warning"); inputSlot.childItem = input;
        machine.Tags.Add("MACHINE_RECHARGABLE_TAG");
        var battery = new GameSlotInventory { ParentItem = machine }; MachineHelper.Slots[machine] = battery;
        PowerWarningMarker.Refresh(machine); Check(!Visible(machine), "Evaporator power warning takes priority");
        MachineHelper.Powered.Add(machine); PowerWarningBatteryInsertPatch.Postfix(battery);
        Check(Visible(machine), "Battery insertion reveals evaporator capacity warning");
        Check(input.IntTags["CURRENT_PART_WATER"] == 1000000 && output.IntTags["free"] == 1, "Prediction never mutates liquids");
        Check(scans == MachineMoistureFarm.CountCalls, "Evaporator events do not scan inventories for farms");
        ModConfig.Instance.OverflowWarningEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(!Visible(machine), "Evaporator respects configuration");
        ModConfig.Instance.OverflowWarningEntry.Set(true); ModHook.Apply(ModEntry.ModId);
        Check(Visible(machine), "Evaporator setting restores warning");
        entry.OnDisable(); Check(Nodes(machine).Count == 0 && UnityEngine.Canvas.Subscriptions == 0, "Disable cleans evaporator marker and callbacks");
        Console.WriteLine("PASS: evaporator native harvest allocation/rounding, performance, quality, solvent module, capacity boundaries, event refresh and power priority.");
    }

    private static void RunPurifier(ModEntry entry)
    {
        entry.OnEnable();
        var purifier = new GameItemElement { Owned = true, identifier = "water_purifier" };
        var slot = new GameSlotInventory { ParentItem = purifier };
        MachineHelper.WaterSlots[purifier] = slot;
        Check(!OverflowWarningMarker.NeedsWarning(purifier), "Purifier without canister does not warn");
        var water = new GameItem { parentInventory = slot };
        slot.childItem = water;
        Check(!OverflowWarningMarker.NeedsWarning(purifier), "Empty purifier canister does not warn");
        int[] qualities = { 0, 15, 25, 50, 75, 100, 200 };
        foreach (int quality in qualities)
        {
            purifier.IntTags["quality"] = quality;
            int cap = MachinePurifier.GetMaxPurityCap(quality);
            water.IntTags["purity"] = cap - 1;
            Check(!OverflowWarningMarker.NeedsWarning(purifier), "Purity below current cap does not warn");
            water.IntTags["purity"] = cap;
            Check(OverflowWarningMarker.NeedsWarning(purifier), "Exact purity cap warns for every quality tier");
            water.IntTags["purity"] = 10000;
            Check(OverflowWarningMarker.NeedsWarning(purifier), "Already purer than the machine's cap warns");
        }
        purifier.Owned = false;
        Check(!OverflowWarningMarker.NeedsWarning(purifier), "NPC purifier excluded");
        purifier.Owned = true; purifier.IntTags["quality"] = 0; water.IntTags["purity"] = 9901;
        OverflowWarningMarker.Refresh(purifier); UnityEngine.Canvas.Render();
        Check(Visible(purifier), "Purifier at cap shows overflow icon");
        int scans = MachineMoistureFarm.CountCalls;
        purifier.IntTags["quality"] = 200;
        OverflowWarningMarker.NotifyTagChanged(purifier, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT"); UnityEngine.Canvas.Render();
        Check(!Visible(purifier), "Quality upgrade immediately removes obsolete cap warning");
        water.IntTags["purity"] = 10000;
        OverflowWarningMarker.NotifyTagChanged(water, "CURRENT_PART_WATER"); UnityEngine.Canvas.Render();
        Check(Visible(purifier), "Purity mutation refreshes purifier");
        Check(MachineMoistureFarm.CountCalls == scans, "Purifier changes never scan inventories for moisture farms");
        purifier.Tags.Add("MACHINE_RECHARGABLE_TAG");
        var battery = new GameSlotInventory { ParentItem = purifier }; MachineHelper.Slots[purifier] = battery;
        PowerWarningMarker.Refresh(purifier);
        Check(!Visible(purifier), "Power warning takes priority on purifier");
        MachineHelper.Powered.Add(purifier); PowerWarningBatteryInsertPatch.Postfix(battery);
        Check(Visible(purifier), "Restoring purifier power reveals purity warning");
        slot.childItem = null; OverflowWarningMarker.SlotRemovePatch.Postfix(slot); UnityEngine.Canvas.Render();
        Check(!Visible(purifier), "Removing canister clears purity warning");
        slot.childItem = water; OverflowWarningMarker.SlotInsertPatch.Postfix(slot); UnityEngine.Canvas.Render();
        Check(Visible(purifier), "Reinserting capped water restores purity warning");
        ModConfig.Instance.OverflowWarningEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(!Visible(purifier), "Purifier respects overflow setting");
        ModConfig.Instance.OverflowWarningEntry.Set(true); ModHook.Apply(ModEntry.ModId);
        Check(Visible(purifier), "Purifier warning returns after enabling setting");
        UnityEngine.Canvas.Render();
        var otherPurifier = new GameItemElement { Owned = true, identifier = "water_purifier" };
        var otherOutput = new GameItemElement(); otherOutput.IntTags["purity"] = 10000;
        MachineHelper.WaterSlots[otherPurifier] = new GameSlotInventory { ParentItem = otherPurifier, childItem = otherOutput };
        OverflowWarningMarker.Refresh(otherPurifier); UnityEngine.Canvas.Render();
        int purityReads = WaterHelper.PurityReads;
        OverflowWarningMarker.NotifyChanged(purifier); OverflowWarningMarker.NotifyChanged(purifier);
        UnityEngine.Canvas.Render();
        Check(WaterHelper.PurityReads == purityReads + 1, "Purifier mutation evaluates only the changed machine once");
        entry.OnDisable(); Check(Nodes(purifier).Count == 0, "Disable cleans purifier overlay");
        Console.WriteLine("PASS: purifier purity thresholds at all quality tiers, empty/unowned exclusions, event refresh, power priority and zero farm scans.");
    }
}
