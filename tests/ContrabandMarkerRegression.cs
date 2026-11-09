using System;
using System.Collections.Generic;
using XicesBetterExperience;

public static class ContrabandMarkerRegression
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static List<TreeNodeRender> Nodes(GameItemElement item)
    {
        return TreeNodeRender.Created.FindAll(node => !node.gameObject.Destroyed &&
            (node.parent == item.middle || node.parent == item.fakeMiddle));
    }

    public static void Run(ModEntry entry)
    {
        entry.OnEnable();
        Check(ExperienceSettings.ContrabandMarker && ModConfig.Instance.ContrabandEntry.Value, "Contraband marker defaults on");
        var item = new GameItemElement(); item.Tags.Add("CONTRABAND_LEVEL_HIGH");
        ContrabandValidatePatch.Postfix(item);
        Check(Nodes(item).Count == 8, "Contraband item gets normal and drag frames");
        foreach (TreeNodeRender node in Nodes(item))
            Check(node.image.color.r > 0.9f && node.image.color.g < 0.2f && node.image.color.b < 0.2f && !node.image.raycastTarget, "Frame is red and noninteractive");
        ContrabandValidatePatch.Postfix(item); Check(Nodes(item).Count == 8, "No duplicate contraband frames");
        ContrabandRotationPatch.Postfix(item); Check(Nodes(item).Count == 8, "Rotation refresh keeps one frame pair");
        item.Tags.Remove("CONTRABAND_LEVEL_HIGH"); ContrabandRemoveStatusPatch.Postfix(item);
        Check(Nodes(item).TrueForAll(n => !n.image.enabled), "Removed contraband hides frame");
        item.Tags.Add("CONTRABAND_LEVEL_LOW"); ModConfig.Instance.ContrabandEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(Nodes(item).TrueForAll(n => !n.image.enabled), "Config disables contraband marker");
        ModConfig.Instance.ContrabandEntry.Set(true); ModHook.Apply(ModEntry.ModId);
        Check(Nodes(item).Exists(n => n.image.enabled), "Config re-enables contraband marker");
        ContrabandDestroyPatch.Prefix(item); Check(Nodes(item).Count == 0, "Destroy cleans contraband marker");
        entry.OnDisable();
        Console.WriteLine("PASS: contraband red frame, all levels, state removal, config toggle, idempotency and cleanup.");
    }
}
