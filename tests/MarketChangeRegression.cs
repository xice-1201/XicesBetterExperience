using System;
using System.Collections.Generic;
using XicesBetterExperience;

public static class MarketChangeRegression
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static List<TreeNodeRender> Nodes(GameItemElement item)
    {
        return TreeNodeRender.Created.FindAll(node => !node.gameObject.Destroyed &&
            (node.parent == item.middle || node.parent == item.fakeMiddle));
    }

    public static void Run(ModEntry entry)
    {
        Check(ExperienceSettings.MarketChangeMarker && ModConfig.Instance.MarketChangeEntry.Value,
            "Market change marker defaults on");
        entry.OnEnable();
        CommissaryData.Modifiers.Clear();
        CommissaryData.Modifiers["WEAPON"] = 35;
        CommissaryData.Modifiers["HOUSEHOLD_GOOD"] = -35;
        var neutral = new GameItemElement { Owned = true };
        neutral.Types.Add("WEAPON"); neutral.Types.Add("HOUSEHOLD_GOOD");
        Check(MarketChangeMarker.GetNetMarketModifier(neutral) == 0, "Opposing type changes cancel");
        MarketChangeMarker.Refresh(neutral);
        Check(Nodes(neutral).Count == 0, "Net-zero item has no marker");

        neutral.Types.Remove("HOUSEHOLD_GOOD");
        MarketChangeMarker.Refresh(neutral);
        var nodes = Nodes(neutral);
        Check(nodes.Count == 8, "Positive item has stepped normal and drag arrows");
        Check(nodes[0].image.enabled && nodes[0].image.color.r > nodes[0].image.color.g,
            "Positive market change uses a red up arrow");
        Check(nodes.TrueForAll(node => node.image.sprite == null),
            "Arrow uses tintable flat UI parts, not an unrelated atlas sprite");
        Check(nodes[0].rectTransform.anchoredPosition.x == 12f &&
            nodes[0].rectTransform.anchoredPosition.y == -9.12f &&
            nodes[0].rectTransform.sizeDelta.x < 24f && nodes[0].rectTransform.sizeDelta.y < 24f,
            "Arrow parts fit within a half-cell marker area");
        Check(nodes[0].rectTransform.anchorMin.x == 0f && nodes[0].rectTransform.anchorMin.y == 1f &&
            nodes[0].rectTransform.pivot.x == 0.5f && nodes[0].rectTransform.pivot.y == 0.5f &&
            nodes[1].rectTransform.localEulerAngles.z == 0f &&
            nodes[2].rectTransform.localEulerAngles.z == 0f,
            "Arrow anchors at the top-left cell without rotating the glyph outside it");
        foreach (TreeNodeRender node in nodes)
        {
            float halfWidth = node.rectTransform.sizeDelta.x * 0.5f;
            float halfHeight = node.rectTransform.sizeDelta.y * 0.5f;
            Check(node.rectTransform.anchoredPosition.x - halfWidth >= 0f &&
                node.rectTransform.anchoredPosition.x + halfWidth <= 24f &&
                node.rectTransform.anchoredPosition.y + halfHeight <= 0f &&
                node.rectTransform.anchoredPosition.y - halfHeight >= -24f,
                "Every arrow bar stays within the marker bounds");
        }

        CommissaryData.Modifiers["WEAPON"] = -35;
        MarketChangeMarker.Refresh(neutral);
        nodes = Nodes(neutral);
        Check(nodes[0].image.enabled && nodes[0].image.color.g > nodes[0].image.color.r,
            "Negative market change uses a green down arrow");
        Check(nodes[1].rectTransform.anchoredPosition.y < nodes[3].rectTransform.anchoredPosition.y,
            "Negative market change mirrors the arrow head downward");
        CommissaryData.Modifiers["WEAPON"] = 0;
        MarketChangeMarker.Refresh(neutral);
        Check(Nodes(neutral).TrueForAll(node => !node.image.enabled), "Zero market change hides markers");

        CommissaryData.Modifiers["WEAPON"] = 35;
        MarketChangeMarker.Refresh(neutral);
        ModConfig.Instance.MarketChangeEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(Nodes(neutral).TrueForAll(node => !node.image.enabled), "Config disables market change markers");
        MarketChangeDestroyPatch.Prefix(neutral);
        Check(Nodes(neutral).Count == 0, "Destroy cleans market change markers");
        entry.OnDisable();
        CommissaryData.Modifiers.Clear();
        Console.WriteLine("PASS: market change netting, arrows, placement, toggle and cleanup.");
    }
}
