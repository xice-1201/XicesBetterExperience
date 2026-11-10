using System;
using System.Collections.Generic;
using XicesBetterExperience;

public static class StatusTest
{
    public static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static List<TreeNodeRender> Nodes(GameItemElement item)
    {
        return TreeNodeRender.Created.FindAll(n => !n.gameObject.Destroyed && n.gameObject.name == "XicesBetterExperience.StatusBackground" &&
            (n.parent == item.middle || n.parent == item.fakeMiddle));
    }
    public static MarkerGraphic Graphic(TreeNodeRender n) { return (MarkerGraphic)n.gameObject.Children[0].Components[typeof(MarkerGraphic)]; }
    public static float Area(List<MarkerTriangle> mesh)
    {
        float sum = 0;
        foreach (var t in mesh) sum += Math.Abs((t.B.x-t.A.x)*(t.C.y-t.A.y)-(t.B.y-t.A.y)*(t.C.x-t.A.x))*0.5f;
        return sum;
    }
}
public static class StolenHeatRegression
{
    public static void Run(ModEntry entry)
    {
        entry.OnEnable();
        var item = new GameItemElement();
        item.Tags.Add("STOLEN_TAG"); item.IntTags["STOLEN_VALUE_INT"] = 24;
        StatusBackgroundValidatePatch.Postfix(item);
        var nodes = StatusTest.Nodes(item);
        StatusTest.Check(nodes.Count == 2, "One shared background for normal and drag paths");
        foreach (var n in nodes)
        {
            var g = StatusTest.Graphic(n);
            StatusTest.Check(!g.raycastTarget && !n.image.enabled, "Background is noninteractive and replaces native image");
            StatusTest.Check(g.enabled == (n.parent == item.middle), "Native visibility is preserved");
            StatusTest.Check(g.Triangles.TrueForAll(t => t.Color.g > 0.8f && t.Color.a == 0.25f), "Stolen background is translucent yellow");
            StatusTest.Check(StatusTest.Area(g.Triangles) == 48*48, "Background covers exactly occupied cell");
        }
        StatusBackgroundValidatePatch.Postfix(item);
        StatusTest.Check(StatusTest.Nodes(item).Count == 2, "No duplicate backgrounds");
        int updates = StatusTest.Graphic(nodes[0]).GeometryUpdates;
        for (int i = 0; i < 100; i++) StatusBackgroundValidatePatch.Postfix(item);
        StatusTest.Check(StatusTest.Graphic(nodes[0]).GeometryUpdates == updates && StatusTest.Nodes(item).Count == 2,
            "Repeated inventory refresh neither creates nodes nor dirties unchanged meshes");
        int builds = ItemStatusBackground.GeometryBuildCount;
        for (int i = 0; i < 100; i++) StatusBackgroundValidatePatch.Postfix(item);
        StatusTest.Check(ItemStatusBackground.GeometryBuildCount == builds, "Unchanged background never rebuilds geometry");
        item.background.image.enabled = false;
        StatusBackgroundVisibilityPatch.Postfix(item);
        item.background.image.enabled = true;
        StatusBackgroundVisibilityPatch.Postfix(item);
        StatusTest.Check(ItemStatusBackground.GeometryBuildCount == builds && StatusTest.Graphic(nodes[0]).enabled,
            "Background visibility restores cached geometry without rebuilding");
        item.modifiedShape.maxX = 1; item.modifiedShape.maxY = 1;
        item.modifiedShape.Empty.Add("1,1");
        StatusBackgroundRotationPatch.Postfix(item);
        StatusTest.Check(ItemStatusBackground.GeometryBuildCount == ++builds, "Changed dimensions rebuild once for both render paths");
        item.modifiedShape.Empty.Remove("1,1"); item.modifiedShape.Empty.Add("0,1");
        StatusBackgroundRotationPatch.Postfix(item);
        StatusTest.Check(ItemStatusBackground.GeometryBuildCount == ++builds, "Equal bounds and occupied count with different mask rebuild");
        UISettings.current.gridSize = 60;
        StatusBackgroundRotationPatch.Postfix(item);
        StatusTest.Check(ItemStatusBackground.GeometryBuildCount == ++builds && StatusTest.Area(StatusTest.Graphic(nodes[0]).Triangles) == 3*60*60,
            "Grid size change rebuilds correctly scaled geometry");
        UISettings.current.gridSize = 48;
        item.modifiedShape.maxX = 0; item.modifiedShape.maxY = 0; item.modifiedShape.Empty.Clear();
        StatusBackgroundRotationPatch.Postfix(item);
        builds = ItemStatusBackground.GeometryBuildCount;
        item.Tags.Add("CONTRABAND_LEVEL_LOW");
        ItemStatusBackground.Refresh(item);
        StatusTest.Check(ItemStatusBackground.GeometryBuildCount == builds + 1, "Color state change rebuilds background");
        item.Tags.Remove("CONTRABAND_LEVEL_LOW"); ItemStatusBackground.Refresh(item);
        item.IntTags["STOLEN_VALUE_INT"] = 0; StolenHeatSetPatch.Postfix(item);
        StatusTest.Check(nodes.TrueForAll(n => !StatusTest.Graphic(n).enabled), "Zero heat hides background");
        item.IntTags["STOLEN_VALUE_INT"] = 1;
        ModConfig.Instance.StolenEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        StatusTest.Check(nodes.TrueForAll(n => !StatusTest.Graphic(n).enabled), "Setting disables background");
        ModConfig.Instance.StolenEntry.Set(true); ModHook.Apply(ModEntry.ModId);
        StatusTest.Check(StatusTest.Graphic(nodes[0]).enabled, "Setting restores existing background");
        item.background.image.enabled = false; item.fakeBackground.image.enabled = true;
        StatusBackgroundDragPatch.Postfix(item);
        StatusTest.Check(nodes.TrueForAll(n => StatusTest.Graphic(n).enabled == (n.parent == item.fakeMiddle)), "Drag visibility follows native sprite");
        item.modifiedShape.minX=2; item.modifiedShape.minY=3;
        item.modifiedShape.maxX=3; item.modifiedShape.maxY=4;
        item.modifiedShape.Empty.Add("3,4");
        StatusBackgroundRotationPatch.Postfix(item);
        StatusTest.Check(StatusTest.Area(StatusTest.Graphic(nodes[0]).Triangles) == 3*48*48, "Nonrectangular and shifted shapes leave empty cells uncolored");
        item.Tags.Remove("STOLEN_TAG"); StolenHeatDecayPatch.Postfix(item);
        StatusTest.Check(nodes.TrueForAll(n => !StatusTest.Graphic(n).enabled), "Removed stolen status hides background");
        StatusBackgroundDestroyPatch.Prefix(item);
        StatusTest.Check(StatusTest.Nodes(item).Count == 0, "Destruction cleans background");
        entry.OnDisable();
        Console.WriteLine("PASS: translucent stolen backgrounds, occupied shapes, native visibility, settings and cleanup.");
    }
}
