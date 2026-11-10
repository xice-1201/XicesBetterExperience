using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace XicesBetterExperience
{
    public static class StorageNameOverlay
    {
        private sealed class Overlay
        {
            public TreeNodeRender Normal, Shadow, Drag, DragShadow;
        }

        private static readonly Dictionary<GameItemElement, Overlay> overlays = new Dictionary<GameItemElement, Overlay>();

        internal static bool IsStorage(GameItem item)
        {
            if (item == null) return false;
            switch (item.identifier)
            {
                case "makeshift_storage_bay":
                case "storage_bay":
                case "storage_bay_large":
                case "storage_bay_gun":
                case "storage_bay_chem":
                case "machine_bay":
                case "machine_bay_ext":
                case "fridge":
                case "smuggler_bay_mini":
                case "smuggler_bay":
                case "smuggler_bay_mod": return true;
                default: return false;
            }
        }

        public static void Refresh(GameItem item)
        {
            var element = item as GameItemElement;
            if (element == null) return;
            try { RefreshElement(element); }
            catch (Exception error)
            {
                Debug.LogError("[XicesBetterExperience] Could not refresh storage name: " + error);
            }
        }

        private static void RefreshElement(GameItemElement item)
        {
            if (item.IsDestroyed() || item.handler == null) { Remove(item); return; }
            string name = ModEntry.IsEnabled && ExperienceSettings.StorageNaming && !item.isDebugMenu &&
                IsStorage(item) && GeneralHelper.IsItemOwned(item) && GeneralHelper.HasCustomName(item) ? GeneralHelper.GetCustomName(item) : null;
            if (String.IsNullOrWhiteSpace(name)) { Remove(item); return; }
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
            Set(overlay.Shadow, name, width, height, item.background.image.enabled, true);
            Set(overlay.Normal, name, width, height, item.background.image.enabled, false);
            Set(overlay.DragShadow, name, width, height, item.fakeBackground.image.enabled, true);
            Set(overlay.Drag, name, width, height, item.fakeBackground.image.enabled, false);
        }

        private static TreeNodeRender Create(TreeNodeRender parent, bool shadow)
        {
            TreeNodeRender node = TreeNodeRender.InstantiateText().SetParent(parent);
            node.gameObject.name = "XicesBetterExperience.StorageName" + (shadow ? ".Shadow" : "");
            node.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            node.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            node.rectTransform.pivot = new Vector2(0.5f, 0f);
            node.text.raycastTarget = false;
            node.text.richText = false;
            node.text.fontStyle = FontStyles.Normal;
            node.text.alignment = TextAlignmentOptions.Bottom;
            node.text.enableWordWrapping = true;
            node.text.enableAutoSizing = true;
            node.text.fontSizeMin = 6;
            node.text.fontSizeMax = 10;
            node.text.overflowMode = TextOverflowModes.Ellipsis;
            node.text.color = shadow ? Color.black : Color.white;
            return node;
        }

        private static void Set(TreeNodeRender node, string name, int width, int height, bool visible, bool shadow)
        {
            node.rectTransform.sizeDelta = new Vector2(width, height);
            node.rectTransform.anchoredPosition = shadow ? new Vector2(0.5f, 1) : new Vector2(0, 2);
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
    public static class StorageNameValidatePatch
    {
        public static void Postfix(GameItemElement __instance) { StorageNameOverlay.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GeneralHelper), "SetCustomName", new[] { typeof(GameItem), typeof(string) })]
    public static class StorageNameSetPatch
    {
        public static void Postfix(GameItem __0) { StorageNameOverlay.Refresh(__0); }
    }

    [HarmonyPatch(typeof(GeneralHelper), "RemoveCustomName", new[] { typeof(GameItem) })]
    public static class StorageNameClearPatch
    {
        public static void Postfix(GameItem __0) { StorageNameOverlay.Refresh(__0); }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleBackground", new[] { typeof(bool), typeof(float), typeof(float) })]
    public static class StorageNameVisibilityPatch
    {
        public static void Postfix(GameItemElement __instance) { StorageNameOverlay.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", new[] { typeof(bool), typeof(Vector4) })]
    public static class StorageNameDragPatch
    {
        public static void Postfix(GameItemElement __instance) { StorageNameOverlay.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "Destroy", new Type[] { })]
    public static class StorageNameDestroyPatch
    {
        public static void Prefix(GameItemElement __instance) { StorageNameOverlay.Remove(__instance); }
    }
}
