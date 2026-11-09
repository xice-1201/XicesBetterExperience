using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    public static class PowerWarningMarker
    {
        private sealed class Overlay
        {
            public TreeNodeRender Normal;
            public TreeNodeRender Drag;
        }

        private static readonly Dictionary<GameItemElement, Overlay> overlays =
            new Dictionary<GameItemElement, Overlay>();

        public static bool NeedsWarning(GameItem item)
        {
            if (item == null || !GeneralHelper.IsItemOwned(item) || !item.IsTag("MACHINE_RECHARGABLE_TAG")) return false;
            GameSlotInventory battery = MachineHelper.GetBatterySlot(item);
            if (battery == null) return false;
            if (!item.IsTag("machinery")) return !MachineHelper.CanPower(item, battery);
            if (!MachineryHelper.IsMachineryToggleOn(item)) return true;
            return !MachineHelper.CanPower(item, battery);
        }

        public static void Refresh(GameItem item)
        {
            GameItemElement element = item as GameItemElement;
            if (element == null) return;
            try { RefreshElement(element); }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not refresh power warning: " + error); }
        }

        private static void RefreshElement(GameItemElement item)
        {
            if (item.IsDestroyed() || item.handler == null) { Remove(item); return; }
            Overlay overlay;
            bool active = ModEntry.IsEnabled && ExperienceSettings.PowerWarning && NeedsWarning(item);
            if (!overlays.TryGetValue(item, out overlay))
            {
                if (!active) return;
                // Keep the alert in the item's stable, unrotated bounds. Parenting it to
                // rotator makes Unity apply the item's corner pivot to a centered child;
                // on multi-cell rotated items that shifts the alert toward neighboring cells.
                // A power warning is a property of the stored item. Keep one render
                // node only; the native fake item can coexist with the source item
                // during inventory transitions and would otherwise duplicate it.
                overlay = new Overlay { Normal = Create(item.middle), Drag = null };
                overlays.Add(item, overlay);
            }
            // During a drag the source background may remain enabled while the fake
            // background is also enabled in the overlay canvas. Treat the two render
            // paths as mutually exclusive so the warning cannot be drawn twice.
            Set(overlay.Normal, item.background.image.enabled && active);
        }

        private static TreeNodeRender Create(TreeNodeRender parent)
        {
            if (parent == null) return null;
            TreeNodeRender node = TreeNodeRender.Instantiate().SetParent(parent);
            node.gameObject.name = "XicesBetterExperience.PowerWarning";
            node.image.sprite = CustomUIManager.Instance == null ? null : CustomUIManager.Instance.spriteWarning;
            // Render the warning as a plain sprite. The shared render-node prefab
            // may carry a sliced/filled Image state; applying that state to the
            // alert sprite produces a second-looking perimeter around the triangle.
            node.image.type = UnityEngine.UI.Image.Type.Simple;
            node.image.preserveAspect = true;
            node.image.fillAmount = 1f;
            node.image.raycastTarget = false;
            node.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            node.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            node.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            node.rectTransform.sizeDelta = new Vector2(26f, 26f);
            node.rectTransform.anchoredPosition = Vector2.zero;
            return node;
        }

        private static void Set(TreeNodeRender node, bool visible)
        {
            if (node == null) return;
            node.rectTransform.SetAsLastSibling();
            node.image.color = new Color(1f, 0.78f, 0.12f, 0.58f);
            node.image.enabled = visible && node.image.sprite != null;
        }

        public static void Remove(GameItemElement item)
        {
            Overlay overlay;
            if (!overlays.TryGetValue(item, out overlay)) return;
            overlays.Remove(item);
            Destroy(overlay.Normal); Destroy(overlay.Drag);
        }

        private static void Destroy(TreeNodeRender node)
        {
            if (node == null) return;
            node.image.enabled = false;
            UnityEngine.Object.Destroy(node.gameObject);
        }

        public static void Clear()
        {
            foreach (GameItemElement item in new List<GameItemElement>(overlays.Keys)) Remove(item);
        }

        public static void RefreshLoaded()
        {
            foreach (GameItemElement item in new List<GameItemElement>(overlays.Keys)) Refresh(item);
            if (!ModEntry.IsEnabled || !ExperienceSettings.PowerWarning || !PlayerStore.IsInstanceExist() || EmporiumEntry.Instance == null) return;
            foreach (GameItem item in PlayerStore.Instance.FindAllItem(false)) Refresh(item);
        }
    }

    [HarmonyPatch(typeof(GameItemElement), "ValidateSelect", new[] { typeof(bool) })]
    public static class PowerWarningValidatePatch { public static void Postfix(GameItemElement __instance) { PowerWarningMarker.Refresh(__instance); } }

    [HarmonyPatch(typeof(GameItemElement), "ToggleBackground", new[] { typeof(bool), typeof(float), typeof(float) })]
    public static class PowerWarningVisibilityPatch { public static void Postfix(GameItemElement __instance) { PowerWarningMarker.Refresh(__instance); } }

    [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", new[] { typeof(bool), typeof(Vector4) })]
    public static class PowerWarningDragPatch { public static void Postfix(GameItemElement __instance) { PowerWarningMarker.Refresh(__instance); } }

    [HarmonyPatch(typeof(GameItemElement), "UpdateRectTransform", new Type[] { })]
    public static class PowerWarningRotationPatch { public static void Postfix(GameItemElement __instance) { PowerWarningMarker.Refresh(__instance); } }

    [HarmonyPatch(typeof(GameItemElement), "Destroy", new Type[] { })]
    public static class PowerWarningDestroyPatch { public static void Prefix(GameItemElement __instance) { PowerWarningMarker.Remove(__instance); } }

    // Battery slots do not necessarily rebuild their parent item element when their
    // contents change. Refresh the owning machine directly after insert/remove.
    [HarmonyPatch(typeof(GameSlotInventory), "UncheckedAccept", new[] { typeof(GameItem) })]
    public static class PowerWarningBatteryInsertPatch
    {
        public static void Postfix(GameSlotInventory __instance)
        {
            try { PowerWarningMarker.Refresh(__instance == null ? null : __instance.GetParentItem()); }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not refresh power warning after battery insert: " + error); }
        }
    }

    [HarmonyPatch(typeof(GameSlotInventory), "Expel", new[] { typeof(GameItem) })]
    public static class PowerWarningBatteryRemovePatch
    {
        public static void Postfix(GameSlotInventory __instance)
        {
            try { PowerWarningMarker.Refresh(__instance == null ? null : __instance.GetParentItem()); }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not refresh power warning after battery removal: " + error); }
        }
    }

}
