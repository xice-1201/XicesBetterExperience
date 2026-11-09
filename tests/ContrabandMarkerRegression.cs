using System;
using System.Collections.Generic;
using XicesBetterExperience;
public static class ContrabandMarkerRegression
{
    public static void Run(ModEntry entry)
    {
        entry.OnEnable();
        var item = new GameItemElement(); item.Tags.Add("CONTRABAND_LEVEL_HIGH");
        ContrabandValidatePatch.Postfix(item);
        var nodes = StatusTest.Nodes(item);
        StatusTest.Check(nodes.Count == 2, "Contraband uses one background pair");
        var mesh = StatusTest.Graphic(nodes[0]).Triangles;
        StatusTest.Check(mesh.TrueForAll(t => t.Color.r == 1f && t.Color.g < 0.2f && t.Color.a == 0.25f), "Contraband is translucent red");
        item.Tags.Add("STOLEN_TAG"); item.IntTags["STOLEN_VALUE_INT"] = 8;
        StolenHeatSetPatch.Postfix(item);
        mesh = StatusTest.Graphic(nodes[0]).Triangles;
        StatusTest.Check(StatusTest.Nodes(item).Count == 2, "Dual status shares a single mesh, not overlapping overlays");
        var yellow = mesh.FindAll(t => t.Color.g > 0.8f);
        var red = mesh.FindAll(t => t.Color.g < 0.2f);
        StatusTest.Check(StatusTest.Area(yellow) == 48*48/2 && StatusTest.Area(red) == 48*48/2, "Diagonal partitions have equal area with no overlap");
        foreach (var t in yellow) foreach (var p in new[] {t.A,t.B,t.C})
            StatusTest.Check(p.x-p.y <= 48.001f, "Yellow stays above-left of diagonal");
        foreach (var t in red) foreach (var p in new[] {t.A,t.B,t.C})
            StatusTest.Check(p.x-p.y >= 47.999f, "Red stays below-right of diagonal");
        ModConfig.Instance.StolenEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        mesh = StatusTest.Graphic(nodes[0]).Triangles;
        StatusTest.Check(StatusTest.Area(mesh) == 48*48 && mesh.TrueForAll(t => t.Color.g < 0.2f), "Disabling stolen restores full red background");
        ModConfig.Instance.StolenEntry.Set(true); ModHook.Apply(ModEntry.ModId);
        item.Tags.Remove("CONTRABAND_LEVEL_HIGH"); ContrabandRemoveStatusPatch.Postfix(item);
        StatusTest.Check(StatusTest.Graphic(nodes[0]).Triangles.TrueForAll(t => t.Color.g > 0.8f), "Removing contraband restores full yellow background");
        item.Tags.Remove("STOLEN_TAG"); StolenHeatRemoveStatusPatch.Postfix(item);
        StatusTest.Check(nodes.TrueForAll(n => !StatusTest.Graphic(n).enabled), "Removing both hides background");
        ContrabandDestroyPatch.Prefix(item);
        StatusTest.Check(StatusTest.Nodes(item).Count == 0, "Shared background cleanup is idempotent");
        entry.OnDisable();
        Console.WriteLine("PASS: contraband background, diagonal partition, independent settings and removal transitions.");
    }
}
