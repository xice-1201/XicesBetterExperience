using System;
using System.Collections.Generic;
using XicesBetterExperience;

public static class StolenHeatRegression
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
        Check(ExperienceSettings.StolenMarker && ModConfig.Instance.StolenEntry.Value, "Stolen marker defaults on");
        var item = new GameItemElement();
        item.Tags.Add("STOLEN_TAG"); item.IntTags["STOLEN_VALUE_INT"] = 24;
        StolenHeatValidatePatch.Postfix(item);
        Check(Nodes(item).Count == 8, "Hot item gets normal and drag yellow frames");
        foreach (TreeNodeRender node in Nodes(item))
        {
            Check(node.image.color.r == 1f && node.image.color.g > 0.8f && node.image.color.b < 0.2f, "Frame is yellow");
            Check(!node.image.raycastTarget, "Frame does not capture input");
            Check(node.image.enabled == (node.parent == item.middle), "Frame follows normal/drag visibility");
        }
        StolenHeatValidatePatch.Postfix(item);
        Check(Nodes(item).Count == 8, "No duplicate on validation");
        StolenHeatRotationPatch.Postfix(item);
        Check(Nodes(item).Count == 8, "Rotation refresh keeps one frame pair");

        item.IntTags["STOLEN_VALUE_INT"] = 0; StolenHeatSetPatch.Postfix(item);
        Check(Nodes(item).Count == 8 && Nodes(item).TrueForAll(n => !n.image.enabled), "Zero heat hides frame");
        item.IntTags["STOLEN_VALUE_INT"] = 1; StolenHeatSetPatch.Postfix(item);
        Check(Nodes(item).Exists(n => n.image.enabled), "Positive heat restores frame");
        item.Tags.Remove("STOLEN_TAG"); StolenHeatDecayPatch.Postfix(item);
        Check(Nodes(item).TrueForAll(n => !n.image.enabled), "Expired stolen status hides frame");

        var ordinary = new GameItemElement(); StolenHeatValidatePatch.Postfix(ordinary);
        Check(Nodes(ordinary).Count == 0, "Ordinary items have no frame");
        item.Tags.Add("STOLEN_TAG"); item.IntTags["STOLEN_VALUE_INT"] = 8;
        ModConfig.Instance.StolenEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(Nodes(item).TrueForAll(n => !n.image.enabled), "Config disables marker immediately");
        ModConfig.Instance.StolenEntry.Set(true); ModHook.Apply(ModEntry.ModId);
        Check(Nodes(item).Exists(n => n.image.enabled), "Config re-enables existing marker");
        item.background.image.enabled = false; item.fakeBackground.image.enabled = true;
        StolenHeatVisibilityPatch.Postfix(item); StolenHeatDragPatch.Postfix(item);
        foreach (TreeNodeRender node in Nodes(item)) Check(node.image.enabled == (node.parent == item.fakeMiddle), "Drag preview visibility follows background");
        StolenHeatDestroyPatch.Prefix(item);
        Check(Nodes(item).Count == 0, "Destroy cleans marker nodes");
        entry.OnDisable();
        Console.WriteLine("PASS: positive stolen heat yellow frame, zero/expired heat removal, normal and drag visibility, config toggle, idempotency and cleanup.");
    }
}
