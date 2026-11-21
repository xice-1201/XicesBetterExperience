using System;
using System.Collections.Generic;
using XicesBetterExperience;

public static class StorageCapacityRegression
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static List<TreeNodeRender> Nodes(GameItemElement item)
    {
        return TreeNodeRender.Created.FindAll(n => !n.gameObject.Destroyed && n.gameObject.name.StartsWith("XicesBetterExperience.StorageCapacity") &&
            (n.parent == item.middle || n.parent == item.fakeMiddle));
    }
    public static void Run(ModEntry entry)
    {
        entry.OnEnable();
        Check(ExperienceSettings.StorageCapacity && ModConfig.Instance.CapacityEntry.Value, "Capacity defaults on");
        Check(ExperienceSettings.SettingsTab("storage_capacity") == "icons", "Capacity belongs to icon tab");
        foreach (int added in new[] { 1, 2 })
        {
            var bay = new GameItemElement { identifier="storage_bay", Owned=true };
            var original = new GameGridInventory { Owner=bay };
            bay.children.Add(original);
            var cargo = new GameItemElement(); cargo.modifiedShape.maxX=4; cargo.modifiedShape.maxY=9;
            original.UncheckedAccept(cargo);
            StorageCapacityOverlay.Refresh(bay);
            var before = Nodes(bay);
            Check(before[1].text.text=="50.00%", "Cache capacity before expansion");
            for (int page=0; page<added; page++)
            {
                var newGrid = new GameGridInventory();
                StorageCapacityResizePatch.Postfix(newGrid); // constructed before it has a parent
                var wrapper = new GraphNodeStorage(); wrapper.children.Add(newGrid); bay.children.Add(wrapper);
            }
            StorageCapacityValidatePatch.Postfix(bay);
            Check(before[1].text.text=="50.00%", "Ordinary validation retains cached ratio");
            StorageCapacityExpansionPatch.Postfix(bay);
            string expected=added==1 ? "25.00%" : "16.67%";
            Check(before.TrueForAll(n=>n.text.text==expected), "Expansion invalidates cached normal and drag ratios without moving contents");
            bay.children.Add(new GameGridInventory());
            StorageCapacityRestoreTabsPatch.Postfix(bay);
            Check(before[1].text.text==(added==1 ? "16.67%" : "12.50%"), "Restoring pages also invalidates existing cache");
            StorageCapacityOverlay.Remove(bay);
        }
        var fridge = new GameItemElement { identifier = "fridge", Owned = true };
        var chill = new GameGridInventory { Owner = fridge };
        chill.inventoryShape.width = 5; chill.inventoryShape.height = 2;
        var freeze = new GameGridInventory { Owner = fridge };
        freeze.inventoryShape.width = 3; freeze.inventoryShape.height = 2;
        fridge.children.Add(chill); fridge.children.Add(freeze);
        var chilledItem = new GameItemElement(); chilledItem.modifiedShape.maxX = 1;
        chill.UncheckedAccept(chilledItem);
        freeze.UncheckedAccept(new GameItemElement());
        StorageCapacityOverlay.Refresh(fridge);
        Check(Nodes(fridge).Count == 4 && Nodes(fridge)[1].text.text == "18.75%", "Fridge combines chilled and frozen storage: 3 of 16 cells");
        fridge.Owned = false; StorageCapacityOverlay.Refresh(fridge);
        Check(Nodes(fridge).Count == 0, "Non-player fridge capacity hidden");
        fridge.Owned = true; chill.inventoryShape.height = 4; freeze.inventoryShape.height = 4;
        StorageCapacityResizePatch.Postfix(chill); UnityEngine.Canvas.Render();
        Check(Nodes(fridge)[1].text.text == "9.38%", "Fridge expansion updates combined capacity");
        StorageCapacityOverlay.Remove(fridge);
        var hypertech = new GameItemElement { identifier = "hypertech_bay", Owned = true };
        var hypertechPages = new List<GameGridInventory>();
        for (int page = 0; page < 2; page++)
        {
            var inventory = new GameGridInventory { Owner = hypertech };
            inventory.inventoryShape.width = 16; inventory.inventoryShape.height = 30;
            var intake = new GraphNodeStorage(); intake.children.Add(inventory);
            hypertech.children.Add(intake); hypertechPages.Add(inventory);
        }
        var hypertechCargo = new GameItemElement();
        hypertechCargo.modifiedShape.maxX = 15; hypertechCargo.modifiedShape.maxY = 29;
        hypertechPages[0].UncheckedAccept(hypertechCargo);
        StorageCapacityOverlay.Refresh(hypertech);
        Check(Nodes(hypertech).Count == 4 && Nodes(hypertech)[1].text.text == "50.00%",
            "Hypertech capacity combines both wrapped 16x30 pages: 480 of 960 cells");
        hypertechPages[1].UncheckedAccept(new GameItemElement());
        StorageCapacityAcceptPatch.Postfix(hypertechPages[1]); UnityEngine.Canvas.Render();
        Check(Nodes(hypertech)[1].text.text == "50.10%", "Hypertech second-page mutations refresh combined capacity");
        StorageCapacityOverlay.Remove(hypertech);
        var store = new GameItemElement { identifier = "storage_bay", Owned=true };
        var grid = new GameGridInventory { Owner = store };
        store.children.Add(grid);
        Check(StorageCapacityOverlay.GetRatio(store) == 0d, "Empty storage is zero percent");
        var contents = new GameItemElement(); contents.modifiedShape.maxX=2; contents.modifiedShape.maxY=1;
        grid.UncheckedAccept(contents);
        Check(StorageCapacityOverlay.GetRatio(store) == 0.06d, "Count occupied cells instead of bounding box or item count");
        contents.modifiedShape.Empty.Add("1,1");
        Check(StorageCapacityOverlay.GetRatio(store) == 0.05d, "Holes in stored items do not count");
        grid.inventoryShape.Blocked.Add("2,1");
        Check(Math.Abs(StorageCapacityOverlay.GetRatio(store).Value - 4d/99d) < 0.000001, "Blocked inventory cells excluded from numerator and denominator");
        var nestedGrid = new GameGridInventory(); contents.children.Add(nestedGrid);
        Check(Math.Abs(StorageCapacityOverlay.GetRatio(store).Value - 4d/99d) < 0.000001, "Nested container capacity excluded");
        var tabWrapper = new GraphNodeStorage(); var expansion = new GameGridInventory { Owner=store };
        tabWrapper.children.Add(expansion); store.children.Add(tabWrapper);
        Check(Math.Abs(StorageCapacityOverlay.GetRatio(store).Value - 4d/199d) < 0.000001, "Expansion pages combine capacities");
        StorageCapacityOverlay.Refresh(store);
        var nodes = Nodes(store);
        Check(nodes.Count==4 && nodes.TrueForAll(n=>n.text.text=="2.01%" && !n.text.raycastTarget), "Two decimal percentage and noninteractive normal/drag text");
        store.Owned=false; StorageCapacityOverlay.Refresh(store);
        Check(Nodes(store).Count==0, "Losing ownership removes percentage nodes");
        StorageCapacityOverlay.Refresh(store); Check(Nodes(store).Count==0, "Non-player containers never create percentage nodes");
        store.Owned=true; StorageCapacityOverlay.Refresh(store); nodes=Nodes(store);
        Check(nodes.Count==4, "Acquiring storage restores percentage");
        GeneralHelper.SetCustomName(store,"Tools"); StorageCapacitySetPatch.Postfix(store);
        Check(nodes[1].rectTransform.anchoredPosition.y==0f && nodes[1].rectTransform.anchoredPosition.x==0f &&
            nodes[1].rectTransform.anchorMin.x==0.5f && nodes[1].rectTransform.anchorMin.y==0.5f &&
            nodes[1].rectTransform.pivot.y==0.5f && nodes[1].text.alignment==TMPro.TextAlignmentOptions.Center,
            "Named storage percentage is centered");
        GeneralHelper.RemoveCustomName(store); StorageCapacityClearPatch.Postfix(store);
        Check(nodes[1].rectTransform.anchoredPosition.y==0f, "Unnamed storage keeps the same center position");
        foreach (string id in new[] { "machine_bay", "machine_bay_ext" })
        {
            var machine = new GameItemElement { identifier=id, Owned=true }; machine.children.Add(new GameGridInventory());
            StorageCapacityOverlay.Refresh(machine);
            Check(Nodes(machine).Count==4, "Machine bay and expanded machine bay show capacity");
            StorageCapacityOverlay.Remove(machine);
        }
        contents.modifiedShape.maxX=0; contents.modifiedShape.maxY=0;
        StorageCapacityAcceptPatch.Postfix(grid); UnityEngine.Canvas.Render();
        Check(nodes[1].text.text=="0.50%", "Inventory mutation refreshes owner immediately");
        grid.Expel(contents); StorageCapacityExpelPatch.Postfix(grid);
        StorageCapacityExpelPatch.Postfix(grid); StorageCapacityAcceptPatch.Postfix(grid);
        Check(UnityEngine.Canvas.Subscriptions == 1, "Inventory mutation burst shares one render batch");
        // Visibility does not recount dirty data before the batch, avoiding repeated inventory walks.
        int gridReads = grid.inventoryShape.Reads;
        StorageCapacityDragPatch.Postfix(store);
        Check(grid.inventoryShape.Reads == gridReads, "Visibility performs zero capacity grid reads");
        Check(nodes[1].text.text == "0.50%", "Visibility reuses cached ratio");
        UnityEngine.Canvas.Render();
        Check(nodes[1].text.text=="0.00%", "Removal updates percentage");
        store.background.image.enabled=false; store.fakeBackground.image.enabled=true;
        StorageCapacityDragPatch.Postfix(store);
        Check(nodes.TrueForAll(n=>n.text.enabled==(n.parent==store.fakeMiddle)), "Drag visibility follows native background");
        Check(StorageCapacityOverlay.RatioColor(0.30).g==1f && StorageCapacityOverlay.RatioColor(0.30001).r==1f &&
            StorageCapacityOverlay.RatioColor(0.69999).g>0.8f && StorageCapacityOverlay.RatioColor(0.70).g<0.3f,
            "Inclusive 30/70 percent color thresholds use unrounded ratio");
        ModConfig.Instance.CapacityEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(Nodes(store).Count==0, "Config removes percentage immediately");
        ModConfig.Instance.CapacityEntry.Set(true); StorageCapacityOverlay.Refresh(store);
        StorageCapacityDestroyPatch.Prefix(store); Check(Nodes(store).Count==0, "Destroy cleans overlays");
        var ordinary = new GameItemElement { identifier="backpack" }; ordinary.children.Add(grid);
        StorageCapacityOverlay.Refresh(ordinary); Check(Nodes(ordinary).Count==0, "Scope matches storage naming");
        entry.OnDisable();
        Console.WriteLine("PASS: storage capacity occupied cells, holes, blocked cells, expansion pages, nested exclusion, thresholds and overlay lifecycle.");
    }
}
