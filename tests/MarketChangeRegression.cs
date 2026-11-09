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
        string[] signIds = { "sign_water", "sign_food", "sign_medical", "sign_household", "sign_material", "sign_firearm", "sign_firearm_heavy", "permit_gun_1", "permit_gun_2", "permit_gun_3" };
        string[] signTypes = { "WATER", "FOOD", "MEDICAL", "HOUSEHOLD_GOOD", "MATERIAL", "WEAPON", "WEAPON", "WEAPON", "WEAPON", "WEAPON" };
        for (int i = 0; i < signIds.Length; i++)
        {
            CommissaryData.Modifiers.Clear();
            var sign = new GameItemElement { identifier = signIds[i], Owned = false };
            // Unrelated changes affecting the sign itself must not mask its advertised market.
            sign.Types.Add("LUXURY_ITEM"); CommissaryData.Modifiers["LUXURY_ITEM"] = 90;
            foreach (int modifier in new[] { -35, 0, 35 })
            {
                CommissaryData.Modifiers[signTypes[i]] = modifier;
                Check(MarketChangeMarker.GetNetMarketModifier(sign) == modifier, "Sign follows advertised market: " + signIds[i]);
                MarketChangeMarker.Refresh(sign);
                var signNodes = Nodes(sign);
                Check(signNodes.Count == 2, "Sign normal and drag markers created");
                var signGraphic = StatusTest.Graphic(signNodes[0]);
                Check(signGraphic.enabled == (modifier != 0), "Sign zero change hides arrow");
                if (modifier != 0)
                    Check((signGraphic.Triangles[6].Color.r > signGraphic.Triangles[6].Color.g) == (modifier > 0), "Sign arrow color follows direction");
            }
            if (i >= 5)
            {
                CommissaryData.Modifiers["WEAPON"] = 35; CommissaryData.Modifiers["FIREARM"] = -35;
                Check(MarketChangeMarker.GetNetMarketModifier(sign) == 0, "Gun sign cancels weapon and firearm changes");
                CommissaryData.Modifiers["FIREARM"] = 10;
                Check(MarketChangeMarker.GetNetMarketModifier(sign) == 45, "Gun sign sums both categories");
                CommissaryData.Modifiers["WEAPON"] = 0; CommissaryData.Modifiers["FIREARM"] = -20;
                Check(MarketChangeMarker.GetNetMarketModifier(sign) == -20, "Firearm-only event affects both gun signs and all permits");
            }
            MarketChangeMarker.Remove(sign);
        }
        foreach (string id in new[] { "sign_berry", "sign_luxury", "sign_ruined_machine", "sign_expedition" })
        {
            var excluded = new GameItemElement { identifier = id };
            Check(MarketChangeMarker.GetNetMarketModifier(excluded) == 0, "No advertised-market mapping for excluded sign: " + id);
        }
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
        Check(nodes.Count == 2, "One mesh for normal and drag arrows");
        var mesh = StatusTest.Graphic(nodes[0]).Triangles;
        Check(mesh.Count == 9 && mesh[6].Color.r > mesh[6].Color.g, "Red arrow with light and dark contour layers");
        float minX=24, maxX=0, minY=0, maxY=-24;
        foreach (var t in mesh) foreach (var p in new[] { t.A,t.B,t.C })
        {
            minX=Math.Min(minX,p.x); maxX=Math.Max(maxX,p.x);
            minY=Math.Min(minY,p.y); maxY=Math.Max(maxY,p.y);
            Check(p.x>=0 && p.x<=24 && p.y<=0 && p.y>=-24, "Arrow stays in half-cell bounds");
        }
        Check(maxX-minX>21 && maxY-minY>21, "Actual visible arrow fills most of its half-cell square");
        Check(nodes[0].rectTransform.pivot.x==0 && nodes[0].rectTransform.pivot.y==1,
            "Top-left placement does not depend on item rotation");
        var upTip=mesh[6].A;
        CommissaryData.Modifiers["WEAPON"] = -35;
        MarketChangeMarker.Refresh(neutral);
        mesh=StatusTest.Graphic(nodes[0]).Triangles;
        Check(mesh[6].Color.g>mesh[6].Color.r && mesh[6].A.y < upTip.y,
            "Green arrow mirrors the tip downward");
        CommissaryData.Modifiers["WEAPON"] = 0;
        MarketChangeMarker.Refresh(neutral);
        Check(Nodes(neutral).TrueForAll(node => !StatusTest.Graphic(node).enabled), "Zero market change hides markers");

        CommissaryData.Modifiers["WEAPON"] = 35;
        MarketChangeMarker.Refresh(neutral);
        var unowned = new GameItemElement { Owned = false };
        unowned.Types.Add("WEAPON");
        MarketChangeValidatePatch.Postfix(unowned);
        Check(Nodes(unowned).Count == 2 && StatusTest.Graphic(Nodes(unowned)[0]).enabled,
            "Non-player items show the same market marker");
        ModConfig.Instance.MarketChangeEntry.Set(false); ModHook.Apply(ModEntry.ModId);
        Check(Nodes(neutral).TrueForAll(node => !StatusTest.Graphic(node).enabled), "Config disables market change markers");
        Check(Nodes(unowned).TrueForAll(node => !StatusTest.Graphic(node).enabled), "Config also disables non-player markers");
        MarketChangeDestroyPatch.Prefix(neutral);
        Check(Nodes(neutral).Count == 0, "Destroy cleans market change markers");
        entry.OnDisable();
        CommissaryData.Modifiers.Clear();
        Console.WriteLine("PASS: market change netting, arrows, placement, toggle and cleanup.");
    }
}
