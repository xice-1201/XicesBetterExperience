using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace XicesBetterExperience
{
    public static class StorageCapacityOverlay
    {
        private sealed class Overlay
        {
            public TreeNodeRender Normal, Shadow, Drag, DragShadow;
        }

        private static readonly Dictionary<GameItemElement, Overlay> overlays = new Dictionary<GameItemElement, Overlay>();

        internal static double? GetRatio(GameItem item)
        {
            long capacity = 0, occupied = 0;
            var visited = new HashSet<GraphNodeStorage>();
            var pending = new Queue<GraphNodeStorage>();
            pending.Enqueue(item);
            while (pending.Count > 0)
            {
                GraphNodeStorage node = pending.Dequeue();
                if (node == null || !visited.Add(node)) continue;
                if (node != item && node is GameItem) continue;
                GameGridInventory grid = node as GameGridInventory;
                if (grid != null && grid.inventoryShape != null)
                {
                    var shape = grid.inventoryShape;
                    var used = new HashSet<long>();
                    foreach (GameItem child in grid.childItems)
                    {
                        if (child == null || (child is GameItemElement && ((GameItemElement)child).IsDestroyed())) continue;
                        var placed = child.modifiedShape;
                        for (int y = Math.Max(0, placed.minY); y <= Math.Min(shape.height - 1, placed.maxY); y++)
                            for (int x = Math.Max(0, placed.minX); x <= Math.Min(shape.width - 1, placed.maxX); x++)
                                if (placed.Get(x, y) != 0 && shape.Get(x, y) == 0)
                                    used.Add((long)y * shape.width + x);
                    }
                    for (int y = 0; y < shape.height; y++)
                        for (int x = 0; x < shape.width; x++)
                            if (shape.Get(x, y) == 0) capacity++;
                    occupied += used.Count;
                    // Items stored inside this grid have their own capacity, not this storage's.
                    continue;
                }
                foreach (GraphNodeStorage child in node.children) pending.Enqueue(child);
            }
            return capacity == 0 ? (double?)null : (double)occupied / capacity;
        }

        internal static Color RatioColor(double ratio)
        {
            return ratio <= 0.30 ? new Color(0.2f, 1f, 0.3f, 1f) :
                ratio >= 0.70 ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(1f, 0.88f, 0.15f, 1f);
        }

        public static void RefreshInventory(GameInventory inventory)
        {
            if (inventory == null || !ModEntry.IsEnabled || !ExperienceSettings.StorageCapacity) return;
            foreach (GameItem owner in GraphUtils.FindParentsType<GameItem>(inventory))
                if (StorageNameOverlay.IsStorage(owner)) Refresh(owner);
        }

        public static void Refresh(GameItem item)
        {
            var element = item as GameItemElement;
            if (element == null) return;
            try { RefreshElement(element); }
            catch (Exception error)
            {
                Debug.LogError("[XicesBetterExperience] Could not refresh storage capacity: " + error);
            }
        }

        private static void RefreshElement(GameItemElement item)
        {
            if (item.IsDestroyed() || item.handler == null) { Remove(item); return; }
            if (!ModEntry.IsEnabled || !ExperienceSettings.StorageCapacity || item.isDebugMenu ||
                !StorageNameOverlay.IsStorage(item) || !GeneralHelper.IsItemOwned(item)) { Remove(item); return; }
            double? ratio = GetRatio(item);
            if (!ratio.HasValue) { Remove(item); return; }
            string name = (ratio.Value * 100d).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "%";
            Color color = RatioColor(ratio.Value);
            Overlay overlay;
            if (!overlays.TryGetValue(item, out overlay))
            {
                overlay = new Overlay();
                // Register first so partial creation is still cleaned up on disable/destroy.
                overlays.Add(item, overlay);
                overlay.Shadow = Create(item.middle, true);
                overlay.Normal = Create(item.middle, false);
                overlay.DragShadow = Create(item.fakeMiddle, true);
                overlay.Drag = Create(item.fakeMiddle, false);
            }
            int width = Math.Max(1, item.widthPixels - 4);
            int height = Math.Min(24, Math.Max(12, item.heightPixels - 4));
            Set(overlay.Shadow, name, width, height, item.background.image.enabled, true, color, item);
            Set(overlay.Normal, name, width, height, item.background.image.enabled, false, color, item);
            Set(overlay.DragShadow, name, width, height, item.fakeBackground.image.enabled, true, color, item);
            Set(overlay.Drag, name, width, height, item.fakeBackground.image.enabled, false, color, item);
        }

        private static TreeNodeRender Create(TreeNodeRender parent, bool shadow)
        {
            TreeNodeRender node = TreeNodeRender.InstantiateText().SetParent(parent);
            node.gameObject.name = "XicesBetterExperience.StorageCapacity" + (shadow ? ".Shadow" : "");
            node.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            node.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            node.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            node.text.raycastTarget = false;
            node.text.richText = false;
            node.text.fontStyle = FontStyles.Normal;
            node.text.alignment = TextAlignmentOptions.Center;
            node.text.enableWordWrapping = false;
            node.text.enableAutoSizing = true;
            node.text.fontSizeMin = 6;
            node.text.fontSizeMax = 10;
            node.text.overflowMode = TextOverflowModes.Ellipsis;
            node.text.color = shadow ? Color.black : Color.white;
            return node;
        }

        private static void Set(TreeNodeRender node, string name, int width, int height, bool visible, bool shadow, Color color, GameItemElement item)
        {
            node.rectTransform.sizeDelta = new Vector2(width, height);
            node.rectTransform.anchoredPosition = shadow ? new Vector2(0.5f, -1f) : Vector2.zero;
            node.text.color = shadow ? Color.black : color;
            node.rectTransform.SetAsLastSibling();
            node.text.text = name;
            node.text.enabled = visible;
        }

        public static void Remove(GameItemElement item)
        {
            Overlay overlay;
            if (!overlays.TryGetValue(item, out overlay)) return;
            overlays.Remove(item);
            Destroy(overlay.Normal); Destroy(overlay.Shadow); Destroy(overlay.Drag); Destroy(overlay.DragShadow);
        }

        private static void Destroy(TreeNodeRender node)
        {
            if (node == null) return;
            node.text.enabled = false;
            UnityEngine.Object.Destroy(node.gameObject);
        }

        public static void Clear()
        {
            foreach (GameItemElement item in new List<GameItemElement>(overlays.Keys)) Remove(item);
        }

        public static void RefreshLoaded()
        {
            foreach (GameItemElement item in new List<GameItemElement>(overlays.Keys)) Refresh(item);
            if (!ModEntry.IsEnabled || !ExperienceSettings.StorageCapacity ||
                !PlayerStore.IsInstanceExist() || EmporiumEntry.Instance == null) return;
            foreach (GameItem item in PlayerStore.Instance.FindAllItem(false))
                if (StorageNameOverlay.IsStorage(item)) Refresh(item);
        }
    }

    [HarmonyPatch(typeof(GameItemElement), "ValidateSelect", new[] { typeof(bool) })]
    public static class StorageCapacityValidatePatch
    {
        public static void Postfix(GameItemElement __instance) { StorageCapacityOverlay.Refresh(__instance); StorageCapacityOverlay.RefreshInventory(__instance.parentInventory); }
    }

    [HarmonyPatch(typeof(GeneralHelper), "SetCustomName", new[] { typeof(GameItem), typeof(string) })]
    public static class StorageCapacitySetPatch
    {
        public static void Postfix(GameItem __0) { StorageCapacityOverlay.Refresh(__0); }
    }

    [HarmonyPatch(typeof(GeneralHelper), "RemoveCustomName", new[] { typeof(GameItem) })]
    public static class StorageCapacityClearPatch
    {
        public static void Postfix(GameItem __0) { StorageCapacityOverlay.Refresh(__0); }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleBackground", new[] { typeof(bool), typeof(float), typeof(float) })]
    public static class StorageCapacityVisibilityPatch
    {
        public static void Postfix(GameItemElement __instance) { StorageCapacityOverlay.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", new[] { typeof(bool), typeof(Vector4) })]
    public static class StorageCapacityDragPatch
    {
        public static void Postfix(GameItemElement __instance) { StorageCapacityOverlay.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "Destroy", new Type[] { })]
    public static class StorageCapacityDestroyPatch
    {
        public static void Prefix(GameItemElement __instance) { StorageCapacityOverlay.Remove(__instance); }
    }
    [HarmonyPatch(typeof(GameGridInventory), "UncheckedAccept", new[] { typeof(GameItem) })]
    public static class StorageCapacityAcceptPatch
    { public static void Postfix(GameGridInventory __instance) { StorageCapacityOverlay.RefreshInventory(__instance); } }
    [HarmonyPatch(typeof(GameGridInventory), "Expel", new[] { typeof(GameItem) })]
    public static class StorageCapacityExpelPatch
    { public static void Postfix(GameGridInventory __instance) { StorageCapacityOverlay.RefreshInventory(__instance); } }
    [HarmonyPatch(typeof(GameGridInventory), "SetShape", new[] { typeof(int), typeof(int) })]
    public static class StorageCapacityResizePatch
    { public static void Postfix(GameGridInventory __instance) { StorageCapacityOverlay.RefreshInventory(__instance); } }
    [HarmonyPatch(typeof(GameGridInventory), "SetShape", new[] { typeof(string), typeof(int) })]
    public static class StorageCapacityReshapePatch
    { public static void Postfix(GameGridInventory __instance) { StorageCapacityOverlay.RefreshInventory(__instance); } }

}
