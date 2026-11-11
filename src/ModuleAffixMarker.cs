using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    public static class ModuleAffixMarker
    {
        private sealed class Badge
        {
            public TreeNodeRender Node;
            public MarkerGraphic Graphic;
            public string Effect;
            public float Size;
            public Badge(TreeNodeRender parent)
            {
                Node = TreeNodeRender.Instantiate().SetParent(parent);
                Node.gameObject.name = "XicesBetterExperience.ModuleAffix";
                Graphic = MarkerGraphic.Attach(Node);
            }
        }
        private sealed class Overlay
        {
            public Badge[] Normal, Drag;
            public Overlay(GameItemElement item)
            {
                Normal = new[] { new Badge(item.middle), new Badge(item.middle) };
                Drag = new[] { new Badge(item.fakeMiddle), new Badge(item.fakeMiddle) };
            }
        }
        private static readonly Dictionary<GameItemElement, Overlay> overlays = new Dictionary<GameItemElement, Overlay>();

        internal static bool TryGetDisplayCell(GameItemElement item, out int x, out int y)
        {
            var shape = item.modifiedShape;
            for (y = shape.maxY; y >= shape.minY; y--)
                for (x = shape.minX; x <= shape.maxX; x++)
                    if (shape.Get(x,y) != 0) return true;
            x = y = 0;
            return false;
        }
        private static string GetEffect(GameItem item, string tag)
        {
            return item.IsTag(tag) ? item.GetTagReadonly(tag).GetString() : null;
        }
        public static void Refresh(GameItem item)
        {
            var element = item as GameItemElement;
            if (element == null) return;
            try { RefreshElement(element); }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not refresh module affix icons: " + error); }
        }
        private static void RefreshElement(GameItemElement item)
        {
            if (item.IsDestroyed() || item.handler == null || !ModEntry.IsEnabled ||
                !ExperienceSettings.ModuleAffixMarker || !item.IsTag("MODULE_TAG")) { Remove(item); return; }
            string first = GetEffect(item,"MODULE_EFFECT1_TAG");
            string second = GetEffect(item,"MODULE_EFFECT2_TAG");
            if (String.IsNullOrEmpty(first)) { first = second; second = null; }
            if (String.IsNullOrEmpty(first)) { Remove(item); return; }
            int x,y;
            if (!TryGetDisplayCell(item,out x,out y)) { Remove(item); return; }
            Overlay overlay;
            if (!overlays.TryGetValue(item,out overlay))
            {
                overlay = new Overlay(item);
                overlays.Add(item,overlay);
            }
            float grid = UISettings.current.gridSize;
            float size = grid * 0.4f, inset = grid * 0.05f;
            float left = (x-item.modifiedShape.minX)*grid;
            float bottom = -(y-item.modifiedShape.minY+1)*grid;
            Set(overlay.Normal[0],first,size,left+inset,bottom+inset,item.background.image.enabled);
            Set(overlay.Drag[0],first,size,left+inset,bottom+inset,item.fakeBackground.image.enabled);
            Set(overlay.Normal[1],second,size,left+grid-inset-size,bottom+inset,item.background.image.enabled);
            Set(overlay.Drag[1],second,size,left+grid-inset-size,bottom+inset,item.fakeBackground.image.enabled);
        }
        private static void Set(Badge badge,string effect,float size,float x,float bottom,bool visible)
        {
            if (String.IsNullOrEmpty(effect)) { badge.Graphic.enabled = false; return; }
            if (badge.Effect != effect || badge.Size != size)
            {
                badge.Graphic.UpdateGeometry(ModuleAffixIcons.Get(effect,size));
                badge.Effect = effect;
                badge.Size = size;
            }
            badge.Node.rectTransform.sizeDelta = new Vector2(size,size);
            badge.Node.rectTransform.anchoredPosition = new Vector2(x,bottom+size);
            badge.Node.rectTransform.SetAsLastSibling();
            badge.Graphic.enabled = visible;
        }
        public static void Remove(GameItemElement item)
        {
            Overlay overlay;
            if (!overlays.TryGetValue(item,out overlay)) return;
            overlays.Remove(item);
            Destroy(overlay.Normal); Destroy(overlay.Drag);
        }
        private static void Destroy(Badge[] badges)
        {
            foreach (Badge badge in badges)
            {
                badge.Graphic.enabled = false;
                UnityEngine.Object.Destroy(badge.Node.gameObject);
            }
        }
        public static void Clear()
        {
            foreach(var item in new List<GameItemElement>(overlays.Keys)) Remove(item);
        }
        internal static void RefreshLoaded(IEnumerable<GameItem> snapshot)
        {
            var items = new HashSet<GameItem>(snapshot);
            foreach(var item in overlays.Keys) items.Add(item);
            foreach(var item in items) Refresh(item);
        }
        internal static void TagChanged(GameItem item,string tag)
        {
            if (tag == "MODULE_EFFECT1_TAG" || tag == "MODULE_EFFECT2_TAG" || tag == "MODULE_TAG") Refresh(item);
        }
    }
    [HarmonyPatch(typeof(GameItemElement), "ValidateSelect", new[] { typeof(bool) })]
    public static class ModuleAffixValidatePatch { public static void Postfix(GameItemElement __instance) { ModuleAffixMarker.Refresh(__instance); } }
    [HarmonyPatch(typeof(GameItemElement), "ToggleBackground", new[] { typeof(bool), typeof(float), typeof(float) })]
    public static class ModuleAffixVisibilityPatch { public static void Postfix(GameItemElement __instance) { ModuleAffixMarker.Refresh(__instance); } }
    [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", new[] { typeof(bool), typeof(Vector4) })]
    public static class ModuleAffixDragPatch { public static void Postfix(GameItemElement __instance) { ModuleAffixMarker.Refresh(__instance); } }
    [HarmonyPatch(typeof(GameItemElement), "UpdateRectTransform", new Type[] { })]
    public static class ModuleAffixRotationPatch { public static void Postfix(GameItemElement __instance) { ModuleAffixMarker.Refresh(__instance); } }
    [HarmonyPatch(typeof(GameItemElement), "Destroy", new Type[] { })]
    public static class ModuleAffixDestroyPatch { public static void Prefix(GameItemElement __instance) { ModuleAffixMarker.Remove(__instance); } }
    [HarmonyPatch(typeof(GameItem), "ModifyTag", new[] { typeof(string), typeof(Action<TagState>), typeof(bool) })]
    public static class ModuleAffixTagValuePatch { public static void Postfix(GameItem __instance,string tagName) { ModuleAffixMarker.TagChanged(__instance,tagName); } }
    [HarmonyPatch(typeof(GameItem), "EnableTag", new[] { typeof(string), typeof(bool) })]
    public static class ModuleAffixTagEnablePatch { public static void Postfix(GameItem __instance,string tagName) { ModuleAffixMarker.TagChanged(__instance,tagName); } }
    [HarmonyPatch(typeof(GameItem), "DisableTag", new[] { typeof(string), typeof(bool) })]
    public static class ModuleAffixTagDisablePatch { public static void Postfix(GameItem __instance,string tagName) { ModuleAffixMarker.TagChanged(__instance,tagName); } }
}
