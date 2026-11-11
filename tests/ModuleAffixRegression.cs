using System;
using System.Collections.Generic;
using XicesBetterExperience;

public static class ModuleAffixRegression
{
    private static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    private static List<TreeNodeRender> Nodes(GameItemElement item)
    {
        return TreeNodeRender.Created.FindAll(n => !n.gameObject.Destroyed && n.gameObject.name == "XicesBetterExperience.ModuleAffix" && (n.parent == item.middle || n.parent == item.fakeMiddle));
    }
    private static void Effect(GameItem item,int slot,string id)
    {
        string key="MODULE_EFFECT"+slot+"_TAG";
        item.Tags.Add(key); item.StringTags[key]=id;
        ModuleAffixMarker.TagChanged(item,key);
    }
    public static void Run(ModEntry entry)
    {
        entry.OnEnable();
        Check(ExperienceSettings.ModuleAffixMarker && ModConfig.Instance.ModuleAffixEntry.Value && ExperienceSettings.SettingsTab("module_affix_marker")=="icons","Affix default on in icons tab");
        var item=new GameItemElement { Owned=false };
        item.Tags.Add("MODULE_TAG");
        ModuleAffixMarker.Refresh(item);
        Check(Nodes(item).Count==0,"Ordinary module without affix has no overlay");
        Effect(item,2,"MODULE_EFFECT_PREMIUM");
        var nodes=Nodes(item);
        Check(nodes.Count==4 && StatusTest.Graphic(nodes[0]).enabled && !StatusTest.Graphic(nodes[1]).enabled,"Second slot alone occupies left badge, including shop goods");
        Check(Math.Abs(nodes[0].rectTransform.anchoredPosition.x-2.4f)<0.001 && Math.Abs(nodes[0].rectTransform.anchoredPosition.y+26.4f)<0.001,"Single affix at bottom left with 5 percent inset");
        Check(Math.Abs(nodes[0].rectTransform.sizeDelta.x-19.2f)<0.001 && !StatusTest.Graphic(nodes[0]).raycastTarget,"Icon is 40 percent of grid and noninteractive");
        Effect(item,1,"MODULE_EFFECT_OVERVOLTED");
        Check(StatusTest.Graphic(nodes[1]).enabled && Math.Abs(nodes[1].rectTransform.anchoredPosition.x-26.4f)<0.001,"Two effects use left and right corners of same cell");
        int builds=ModuleAffixIcons.BuildCount, submissions=StatusTest.Graphic(nodes[0]).GeometryUpdates;
        for(int i=0;i<100;i++) ModuleAffixMarker.Refresh(item);
        Check(ModuleAffixIcons.BuildCount==builds && StatusTest.Graphic(nodes[0]).GeometryUpdates==submissions,"Unchanged refresh never rebuilds or resubmits");
        item.background.image.enabled=false; item.fakeBackground.image.enabled=true;
        ModuleAffixDragPatch.Postfix(item);
        Check(nodes.TrueForAll(n=>StatusTest.Graphic(n).enabled==(n.parent==item.fakeMiddle)),"Normal and drag visibility follow native paths");
        Check(ModuleAffixIcons.BuildCount==builds,"Drag visibility reuses geometry");
        item.modifiedShape.minX=5;item.modifiedShape.minY=7;item.modifiedShape.maxX=7;item.modifiedShape.maxY=9;
        item.modifiedShape.Empty.Add("5,9");item.modifiedShape.Empty.Add("6,9");
        ModuleAffixRotationPatch.Postfix(item);
        int x,y;
        Check(ModuleAffixMarker.TryGetDisplayCell(item,out x,out y) && x==7 && y==9,"Sparse bottom row chooses leftmost actually occupied cell");
        Check(Math.Abs(nodes[0].rectTransform.anchoredPosition.x-98.4f)<0.001 && Math.Abs(nodes[0].rectTransform.anchoredPosition.y+122.4f)<0.001,"Shifted shape uses local display coordinates");
        item.modifiedShape.Empty.Add("7,9"); ModuleAffixRotationPatch.Postfix(item);
        Check(ModuleAffixMarker.TryGetDisplayCell(item,out x,out y) && x==5 && y==8,"Entirely empty trailing row is skipped");
        // All four rotations and both flip states of a sparse L mask, expressed as native transformed occupancy.
        for(int flip=0;flip<2;flip++) for(int rotation=0;rotation<4;rotation++)
        {
            var cells=new List<int[]>();
            foreach(var cell in new[]{new[]{0,0},new[]{0,1},new[]{0,2},new[]{1,2}})
            {
                int cx=flip==0?cell[0]:1-cell[0],cy=cell[1],w=2,h=3;
                for(int r=0;r<rotation;r++) { int next=cy;cy=w-1-cx;cx=next;int old=w;w=h;h=old; }
                cells.Add(new[]{cx,cy});
            }
            int width=rotation%2==0?2:3,height=rotation%2==0?3:2;
            item.modifiedShape.minX=item.modifiedShape.minY=0;
            item.modifiedShape.maxX=width-1;item.modifiedShape.maxY=height-1;item.modifiedShape.Empty.Clear();
            for(int yy=0;yy<height;yy++) for(int xx=0;xx<width;xx++)
                if(!cells.Exists(c=>c[0]==xx && c[1]==yy)) item.modifiedShape.Empty.Add(xx+","+yy);
            int expectedY=-1,expectedX=99;
            foreach(var c in cells) if(c[1]>expectedY || c[1]==expectedY && c[0]<expectedX) { expectedX=c[0];expectedY=c[1]; }
            ModuleAffixRotationPatch.Postfix(item);
            Check(ModuleAffixMarker.TryGetDisplayCell(item,out x,out y) && x==expectedX && y==expectedY,"Rotated/flipped L chooses bottommost left occupied cell");
        }
        UISettings.current.gridSize=60;ModuleAffixMarker.Refresh(item);
        Check(nodes[0].rectTransform.sizeDelta.x==24,"UI scaling resizes icons");UISettings.current.gridSize=48;
        string[] ids={"PREMIUM","INFERIOR","RECEIVER","TOP_MOUNT","SIDE_MOUNT","OVERVOLTED","DEGRADING","LEARNING_ALGORITHM","NEGATIVE_FEEDBACK"};
        var fingerprints=new HashSet<string>();
        foreach(string id in ids)
        {
            var mesh=ModuleAffixIcons.Get("MODULE_EFFECT_"+id,20);
            Check(mesh.Count>10,"All nine icons triangulate completely");
            var fingerprint=new System.Text.StringBuilder();
            foreach(var t in mesh)
            {
                foreach(var p in new[]{t.A,t.B,t.C}) { Check(p.x>=0 && p.x<=20 && p.y<=0 && p.y>=-20,"Icon geometry stays inside badge");fingerprint.Append(p.x).Append(',').Append(p.y).Append(';'); }
            }
            Check(fingerprints.Add(fingerprint.ToString()),"Each affix has a different silhouette");
            Check(Object.ReferenceEquals(mesh,ModuleAffixIcons.Get("MODULE_EFFECT_"+id,20)),"Cached glyph list reused");
        }
        Check(ModuleAffixIcons.Get("unknown",20).Count>0,"Unknown affix gets visible fallback");
        item.Tags.Remove("MODULE_EFFECT1_TAG");ModuleAffixMarker.TagChanged(item,"MODULE_EFFECT1_TAG");
        Check(!StatusTest.Graphic(nodes[1]).enabled,"Removing first effect compacts remaining effect to left");
        ModConfig.Instance.ModuleAffixEntry.Set(false);ModHook.Apply(ModEntry.ModId);
        Check(Nodes(item).Count==0,"Config removes existing markers immediately");
        ModConfig.Instance.ModuleAffixEntry.Set(true);ModHook.Apply(ModEntry.ModId);ModuleAffixMarker.Refresh(item);
        Check(Nodes(item).Count==4,"Reenable restores markers");
        ModuleAffixDestroyPatch.Prefix(item);Check(Nodes(item).Count==0,"Destroy cleans badge nodes");
        item.Destroyed=true;ModuleAffixMarker.Refresh(item);Check(Nodes(item).Count==0,"Destroyed item remains excluded");
        var unrelated=new GameItemElement();Effect(unrelated,1,"MODULE_EFFECT_PREMIUM");Check(Nodes(unrelated).Count==0,"Non-module excluded");
        entry.OnDisable();
        Console.WriteLine("PASS: nine affix glyphs, native tags, slots, shop goods, sparse shape/rotations/flips, scaling, normal/drag, caches, toggles and cleanup.");
    }
}
