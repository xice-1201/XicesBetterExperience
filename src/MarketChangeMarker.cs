using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    public static class MarketChangeMarker
    {
        private sealed class Overlay
        {
            public TreeNodeRender[] Normal;
            public TreeNodeRender[] Drag;
            public float Size; public int Direction;
        }

        private static readonly Dictionary<GameItemElement, Overlay> overlays =
            new Dictionary<GameItemElement, Overlay>();

        public static int GetNetMarketModifier(GameItem item)
        {
            if (item == null) return 0;
            // Signs and permits represent their merchandise's market, not their own value.
            switch (item.identifier)
            {
                case "sign_water": return CommissaryData.GetTotalTypeMarketPriceModifier("WATER");
                case "sign_food": return CommissaryData.GetTotalTypeMarketPriceModifier("FOOD");
                case "sign_medical": return CommissaryData.GetTotalTypeMarketPriceModifier("MEDICAL");
                case "sign_household": return CommissaryData.GetTotalTypeMarketPriceModifier("HOUSEHOLD_GOOD");
                case "sign_material": return CommissaryData.GetTotalTypeMarketPriceModifier("MATERIAL");
                case "sign_firearm":
                case "sign_firearm_heavy":
                case "permit_gun_1":
                case "permit_gun_2":
                case "permit_gun_3":
                    return CommissaryData.GetTotalTypeMarketPriceModifier("WEAPON") +
                        CommissaryData.GetTotalTypeMarketPriceModifier("FIREARM");
            }
            int total = 0;
            List<string> types = item.GetGameItemType();
            if (types == null) return 0;
            foreach (string type in types)
                total += CommissaryData.GetTotalTypeMarketPriceModifier(type);
            return total;
        }

        public static void Refresh(GameItem item)
        {
            GameItemElement element = item as GameItemElement;
            if (element == null) return;
            try { RefreshElement(element); }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not refresh market change marker: " + error); }
        }

        private static void RefreshElement(GameItemElement item)
        {
            if (item.IsDestroyed() || item.handler == null) { Remove(item); return; }
            if (!ModEntry.IsEnabled || !ExperienceSettings.MarketChangeMarker) { Remove(item); return; }
            int modifier = GetNetMarketModifier(item);
            Overlay overlay;
            bool active = ModEntry.IsEnabled && ExperienceSettings.MarketChangeMarker &&
                modifier != 0;
            if (!overlays.TryGetValue(item, out overlay))
            {
                if (!active) return;
                overlay = new Overlay { Normal = Create(item.middle), Drag = Create(item.fakeMiddle) };
                overlays.Add(item, overlay);
            }
            float size = UISettings.current == null ? 24f : UISettings.current.gridSize * 0.5f;
            int direction = Math.Sign(modifier);
            List<MarkerTriangle> geometry = null;
            if (active && (overlay.Size != size || overlay.Direction != direction))
            {
                geometry = GetArrow(size, direction > 0);
                overlay.Size = size; overlay.Direction = direction;
            }
            Set(overlay.Normal, item.background.image.enabled && active, size, geometry);
            Set(overlay.Drag, item.fakeBackground.image.enabled && active, size, geometry);
        }

        private static readonly Dictionary<TreeNodeRender, MarkerGraphic> graphics = new Dictionary<TreeNodeRender, MarkerGraphic>();

        private static TreeNodeRender[] Create(TreeNodeRender parent)
        {
            if (parent == null) return new TreeNodeRender[0];
            TreeNodeRender node = TreeNodeRender.Instantiate().SetParent(parent);
            node.gameObject.name = "XicesBetterExperience.MarketChange";
            graphics.Add(node, MarkerGraphic.Attach(node));
            return new[] { node };
        }

        internal static List<MarkerTriangle> BuildArrow(float size, bool up)
        {
            var result = new List<MarkerTriangle>();
            // A pale outer edge, dark keyline and saturated fill keep the arrow
            // legible against both status colors and detailed item sprites.
            AddArrow(result, size, up, 1f, new Color(0.95f, 0.96f, 1f, 1f));
            AddArrow(result, size, up, 0.86f, new Color(0.08f, 0.09f, 0.12f, 1f));
            AddArrow(result, size, up, 0.65f, up ? new Color(1f, 0.18f, 0.18f, 1f) : new Color(0.15f, 0.95f, 0.35f, 1f));
            return result;
        }

        private static void AddArrow(List<MarkerTriangle> target, float size, bool up, float scale, Color color)
        {
            Vector2[] points = { new Vector2(0.5f, 0.04f), new Vector2(0.96f, 0.48f),
                new Vector2(0.68f, 0.48f), new Vector2(0.68f, 0.96f),
                new Vector2(0.32f, 0.96f), new Vector2(0.32f, 0.48f), new Vector2(0.04f, 0.48f) };
            for (int i = 0; i < points.Length; i++)
            {
                float x = 0.5f + (points[i].x - 0.5f) * scale;
                float y = 0.5f + (points[i].y - 0.5f) * scale;
                points[i] = new Vector2(x * size, -(up ? y : 1f - y) * size);
            }
            target.Add(new MarkerTriangle(points[0], points[1], points[6], color));
            target.Add(new MarkerTriangle(points[2], points[3], points[4], color));
            target.Add(new MarkerTriangle(points[2], points[4], points[5], color));
        }

        private static float cachedSize = -1f;
        private static List<MarkerTriangle> upArrow, downArrow;
        private static List<MarkerTriangle> GetArrow(float size, bool up)
        {
            if (cachedSize != size) { cachedSize = size; upArrow = null; downArrow = null; }
            if (up) return upArrow ?? (upArrow = BuildArrow(size, true));
            return downArrow ?? (downArrow = BuildArrow(size, false));
        }

        private static void Set(TreeNodeRender[] parts, bool visible, float size, List<MarkerTriangle> geometry)
        {
            foreach (TreeNodeRender node in parts)
            {
                node.rectTransform.SetAsLastSibling();
                node.rectTransform.sizeDelta = new Vector2(size, size);
                node.rectTransform.anchoredPosition = new Vector2(1f, -1f);
                MarkerGraphic graphic = graphics[node];
                if (geometry != null) graphic.UpdateGeometry(geometry);
                graphic.enabled = visible;
            }
        }

        public static void Remove(GameItemElement item)
        {
            Overlay overlay;
            if (!overlays.TryGetValue(item, out overlay)) return;
            overlays.Remove(item);
            Destroy(overlay.Normal);
            Destroy(overlay.Drag);
        }

        private static void Destroy(TreeNodeRender[] parts)
        {
            if (parts == null) return;
            foreach (TreeNodeRender node in parts)
            {
                if (node == null) continue;
                graphics[node].enabled = false;
                graphics.Remove(node);
                UnityEngine.Object.Destroy(node.gameObject);
            }
        }

        public static void Clear()
        {
            foreach (GameItemElement item in new List<GameItemElement>(overlays.Keys)) Remove(item);
        }

        public static void RefreshLoaded()
        {
            RefreshLoaded(ModEntry.IsEnabled && PlayerStore.IsInstanceExist() && EmporiumEntry.Instance != null
                ? PlayerStore.Instance.FindAllItem(false) : new List<GameItem>());
        }
        internal static void RefreshLoaded(IEnumerable<GameItem> snapshot)
        {
            var items = new HashSet<GameItem>(snapshot);
            foreach (var item in overlays.Keys) items.Add(item);
            foreach (var item in items) Refresh(item);
        }
    }

    [HarmonyPatch(typeof(GameItemElement), "ValidateSelect", new[] { typeof(bool) })]
    public static class MarketChangeValidatePatch { public static void Postfix(GameItemElement __instance) { MarketChangeMarker.Refresh(__instance); } }

    [HarmonyPatch(typeof(GameItemElement), "ToggleBackground", new[] { typeof(bool), typeof(float), typeof(float) })]
    public static class MarketChangeVisibilityPatch { public static void Postfix(GameItemElement __instance) { MarketChangeMarker.Refresh(__instance); } }

    [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", new[] { typeof(bool), typeof(Vector4) })]
    public static class MarketChangeDragPatch { public static void Postfix(GameItemElement __instance) { MarketChangeMarker.Refresh(__instance); } }

    [HarmonyPatch(typeof(GameItemElement), "UpdateRectTransform", new Type[] { })]
    public static class MarketChangeRotationPatch { public static void Postfix(GameItemElement __instance) { MarketChangeMarker.Refresh(__instance); } }

    [HarmonyPatch(typeof(GameItemElement), "Destroy", new Type[] { })]
    public static class MarketChangeDestroyPatch { public static void Prefix(GameItemElement __instance) { MarketChangeMarker.Remove(__instance); } }

    [HarmonyPatch(typeof(StoreEventManager), "ActivateEvent", new[] { typeof(StoreEvent), typeof(bool), typeof(bool) })]
    public static class MarketChangeEventActivatePatch { public static void Postfix() { MarketChangeMarker.RefreshLoaded(); } }

    [HarmonyPatch(typeof(StoreEventManager), "DeactivateEvent", new[] { typeof(StoreEvent) })]
    public static class MarketChangeEventDeactivatePatch { public static void Postfix() { MarketChangeMarker.RefreshLoaded(); } }

    [HarmonyPatch(typeof(StoreEventManager), "OnDayEnd", new Type[] { })]
    public static class MarketChangeDayEndPatch { public static void Postfix() { MarketChangeMarker.RefreshLoaded(); } }
}
