using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    public static class ContrabandMarker
    {
        private sealed class Overlay
        {
            public TreeNodeRender[] Normal;
            public TreeNodeRender[] Drag;
        }

        private static readonly Dictionary<GameItemElement, Overlay> overlays =
            new Dictionary<GameItemElement, Overlay>();
        private static readonly Color highlight = new Color(1f, 0.12f, 0.12f, 1f);

        public static bool IsContraband(GameItem item)
        {
            return item != null && ContrabandHelper.IsContraband(item);
        }

        public static void Refresh(GameItem item)
        {
            GameItemElement element = item as GameItemElement;
            if (element == null) return;
            try { RefreshElement(element); }
            catch (Exception error)
            {
                Debug.LogError("[XicesBetterExperience] Could not refresh contraband marker: " + error);
            }
        }

        private static void RefreshElement(GameItemElement item)
        {
            if (item.IsDestroyed() || item.handler == null) { Remove(item); return; }
            Overlay overlay;
            bool active = ModEntry.IsEnabled && ExperienceSettings.ContrabandMarker && IsContraband(item);
            if (!overlays.TryGetValue(item, out overlay))
            {
                if (!active) return;
                overlay = new Overlay();
                overlays.Add(item, overlay);
                // Match the native sprite transform so rotated items keep a tight red frame.
                overlay.Normal = CreateFrame(item.rotator);
                overlay.Drag = CreateFrame(item.fakeRotator);
            }
            SetFrame(overlay.Normal, item.background.image.enabled && active);
            SetFrame(overlay.Drag, item.fakeBackground.image.enabled && active);
        }

        private static TreeNodeRender[] CreateFrame(TreeNodeRender parent)
        {
            TreeNodeRender[] nodes = new TreeNodeRender[4];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = TreeNodeRender.Instantiate().SetParent(parent);
                nodes[i].gameObject.name = "XicesBetterExperience.Contraband";
                nodes[i].image.sprite = null;
                nodes[i].image.raycastTarget = false;
            }
            return nodes;
        }

        // Inset the red frame so it remains visible alongside the outer yellow stolen frame.
        private static void SetFrame(TreeNodeRender[] nodes, bool visible)
        {
            if (nodes == null) return;
            const float inset = 0.08f;
            const float width = 2f;
            Place(nodes[0], new Vector2(inset, 1f - inset), new Vector2(1f - inset, 1f - inset), new Vector2(0.5f, 1f), new Vector2(0f, width));
            Place(nodes[1], new Vector2(inset, inset), new Vector2(1f - inset, inset), new Vector2(0.5f, 0f), new Vector2(0f, width));
            Place(nodes[2], new Vector2(inset, inset), new Vector2(inset, 1f - inset), new Vector2(0f, 0.5f), new Vector2(width, 0f));
            Place(nodes[3], new Vector2(1f - inset, inset), new Vector2(1f - inset, 1f - inset), new Vector2(1f, 0.5f), new Vector2(width, 0f));
            foreach (TreeNodeRender node in nodes)
            {
                node.rectTransform.SetAsLastSibling();
                node.image.color = highlight;
                node.image.enabled = visible;
            }
        }

        private static void Place(TreeNodeRender node, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
        {
            node.rectTransform.anchorMin = anchorMin;
            node.rectTransform.anchorMax = anchorMax;
            node.rectTransform.pivot = pivot;
            node.rectTransform.sizeDelta = size;
            node.rectTransform.anchoredPosition = Vector2.zero;
        }

        public static void Remove(GameItemElement item)
        {
            Overlay overlay;
            if (!overlays.TryGetValue(item, out overlay)) return;
            overlays.Remove(item);
            Destroy(overlay.Normal);
            Destroy(overlay.Drag);
        }

        private static void Destroy(TreeNodeRender[] nodes)
        {
            if (nodes == null) return;
            foreach (TreeNodeRender node in nodes)
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
            if (!ModEntry.IsEnabled || !ExperienceSettings.ContrabandMarker ||
                !PlayerStore.IsInstanceExist() || EmporiumEntry.Instance == null) return;
            foreach (GameItem item in PlayerStore.Instance.FindAllItem(false)) Refresh(item);
        }
    }

    [HarmonyPatch(typeof(GameItemElement), "ValidateSelect", new[] { typeof(bool) })]
    public static class ContrabandValidatePatch
    {
        public static void Postfix(GameItemElement __instance) { ContrabandMarker.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleBackground", new[] { typeof(bool), typeof(float), typeof(float) })]
    public static class ContrabandVisibilityPatch
    {
        public static void Postfix(GameItemElement __instance) { ContrabandMarker.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", new[] { typeof(bool), typeof(Vector4) })]
    public static class ContrabandDragPatch
    {
        public static void Postfix(GameItemElement __instance) { ContrabandMarker.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "UpdateRectTransform", new Type[] { })]
    public static class ContrabandRotationPatch
    {
        public static void Postfix(GameItemElement __instance) { ContrabandMarker.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "Destroy", new Type[] { })]
    public static class ContrabandDestroyPatch
    {
        public static void Prefix(GameItemElement __instance) { ContrabandMarker.Remove(__instance); }
    }

    [HarmonyPatch(typeof(ContrabandHelper), "InitContrabandItem", new[] { typeof(GameItem), typeof(int) })]
    public static class ContrabandInitPatch
    {
        public static void Postfix(GameItem __0) { ContrabandMarker.Refresh(__0); }
    }

    [HarmonyPatch(typeof(ContrabandHelper), "RemoveContrabandStatus", new[] { typeof(GameItem) })]
    public static class ContrabandRemoveStatusPatch
    {
        public static void Postfix(GameItem __0) { ContrabandMarker.Refresh(__0); }
    }
}
