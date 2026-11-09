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
        }

        private static readonly Dictionary<GameItemElement, Overlay> overlays =
            new Dictionary<GameItemElement, Overlay>();

        public static int GetNetMarketModifier(GameItem item)
        {
            if (item == null) return 0;
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
            int modifier = GetNetMarketModifier(item);
            Overlay overlay;
            bool active = ModEntry.IsEnabled && ExperienceSettings.MarketChangeMarker &&
                GeneralHelper.IsItemOwned(item) && modifier != 0;
            if (!overlays.TryGetValue(item, out overlay))
            {
                if (!active) return;
                overlay = new Overlay { Normal = Create(item.middle), Drag = Create(item.fakeMiddle) };
                overlays.Add(item, overlay);
            }
            Set(overlay.Normal, item.background.image.enabled && active, modifier);
            Set(overlay.Drag, item.fakeBackground.image.enabled && active, modifier);
        }

        private static TreeNodeRender[] Create(TreeNodeRender parent)
        {
            if (parent == null) return new TreeNodeRender[0];
            float size = UISettings.current == null ? 24f : UISettings.current.gridSize * 0.5f;
            float cellCenter = size * 0.5f;
            TreeNodeRender[] parts = new TreeNodeRender[4];
            for (int i = 0; i < parts.Length; i++)
            {
                TreeNodeRender node = TreeNodeRender.Instantiate().SetParent(parent);
                node.gameObject.name = "XicesBetterExperience.MarketChange";
                // A sprite-less Unity Image renders as a flat white quad, which gives
                // us a crisp, tintable arrow without stretching an unrelated atlas icon.
                node.image.sprite = null;
                node.image.type = UnityEngine.UI.Image.Type.Simple;
                node.image.raycastTarget = false;
                node.rectTransform.anchorMin = new Vector2(0f, 1f);
                node.rectTransform.anchorMax = new Vector2(0f, 1f);
                node.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                parts[i] = node;
            }

            Configure(parts, size, true);
            return parts;
        }

        private static void Configure(TreeNodeRender[] parts, float size, bool up)
        {
            float center = size * 0.5f;
            // Four axis-aligned bars form a compact, solid arrow: a shaft and
            // three stepped rows for the triangular head. Mirroring the rows
            // avoids rotating each piece around its own center.
            SetPart(parts[0], size * 0.14f, size * 0.24f,
                center, -size * (up ? 0.38f : 0.18f), 0f);
            SetPart(parts[1], size * 0.18f, size * 0.09f,
                center, -size * (up ? 0.14f : 0.42f), 0f);
            SetPart(parts[2], size * 0.30f, size * 0.09f,
                center, -size * (up ? 0.22f : 0.34f), 0f);
            SetPart(parts[3], size * 0.42f, size * 0.09f,
                center, -size * (up ? 0.30f : 0.26f), 0f);
        }

        private static void SetPart(TreeNodeRender node, float width, float height,
            float x, float y, float angle)
        {
            if (node == null) return;
            node.rectTransform.sizeDelta = new Vector2(width, height);
            node.rectTransform.anchoredPosition = new Vector2(x, y);
            node.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private static void Set(TreeNodeRender[] parts, bool visible, int modifier)
        {
            if (parts == null) return;
            Color color = modifier > 0
                ? new Color(0.95f, 0.08f, 0.08f, 1f)
                : new Color(0.08f, 0.85f, 0.2f, 1f);
            float size = UISettings.current == null ? 24f : UISettings.current.gridSize * 0.5f;
            Configure(parts, size, modifier > 0);
            foreach (TreeNodeRender node in parts)
            {
                if (node == null) continue;
                node.rectTransform.SetAsLastSibling();
                node.image.color = color;
                node.image.enabled = visible;
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
                node.image.enabled = false;
                UnityEngine.Object.Destroy(node.gameObject);
            }
        }

        public static void Clear()
        {
            foreach (GameItemElement item in new List<GameItemElement>(overlays.Keys)) Remove(item);
        }

        public static void RefreshLoaded()
        {
            foreach (GameItemElement item in new List<GameItemElement>(overlays.Keys)) Refresh(item);
            if (!ModEntry.IsEnabled || !ExperienceSettings.MarketChangeMarker ||
                !PlayerStore.IsInstanceExist() || EmporiumEntry.Instance == null) return;
            foreach (GameItem item in PlayerStore.Instance.FindAllItem(false)) Refresh(item);
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
