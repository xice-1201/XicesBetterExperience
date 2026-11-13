using System;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    internal static class ScenePowerWarningMarker
    {
        private static GameObject vendingOverlay, fountainOverlay;
        private static bool queued;

        internal static void QueueRefresh()
        {
            if (queued || !ModEntry.IsEnabled || !ExperienceSettings.PowerWarning) return;
            queued = true;
            Canvas.willRenderCanvases += Flush;
        }

        private static void Flush()
        {
            Canvas.willRenderCanvases -= Flush;
            queued = false;
            RefreshAll();
        }

        private static void Refresh(GameItem machine, SpriteRenderer host, ref GameObject overlay)
        {
            bool active = ModEntry.IsEnabled && ExperienceSettings.PowerWarning && host != null &&
                PowerWarningMarker.NeedsWarning(machine) && CustomUIManager.Instance != null &&
                CustomUIManager.Instance.spriteWarning != null;
            if (!active)
            {
                if (overlay != null) overlay.SetActive(false);
                return;
            }
            if (overlay == null)
            {
                overlay = new GameObject("XicesBetterExperience.ScenePowerWarning");
                overlay.transform.SetParent(host.transform, false);
                overlay.AddComponent<SpriteRenderer>();
            }
            SpriteRenderer renderer = overlay.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomUIManager.Instance.spriteWarning;
            renderer.color = new Color(1f, .78f, .12f, .58f);
            renderer.sortingLayerID = host.sortingLayerID;
            renderer.sortingOrder = host.sortingOrder + 1;
            // Use sprite-local bounds: the parent's scene scale applies equally
            // to the machine and its warning. No colliders or UI raycast targets.
            float width = host.sprite == null ? 1f : host.sprite.bounds.size.x;
            float spriteSize = Math.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y);
            float scale = spriteSize > 0f ? width * .28f / spriteSize : 1f;
            overlay.transform.localScale = new Vector3(scale, scale, 1f);
            Vector3 center = host.sprite == null ? Vector3.zero : host.sprite.bounds.center;
            overlay.transform.localPosition = new Vector3(center.x, center.y, -.01f);
            overlay.SetActive(true);
        }

        internal static void RefreshAll()
        {
            try
            {
                var entry = EmporiumEntry.Instance;
                if (entry == null) return;
                Refresh(entry.vendingMachineElement == null ? null : entry.vendingMachineElement.childItem,
                    StoreVendingMachine.Instance == null ? null : StoreVendingMachine.Instance.image, ref vendingOverlay);
                Refresh(entry.vendingFountainElement == null ? null : entry.vendingFountainElement.childItem,
                    StoreVendingFountain.Instance == null ? null : StoreVendingFountain.Instance.image, ref fountainOverlay);
            }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not refresh scene power warnings: " + error); }
        }

        internal static void NotifyTagChanged(GameItem item, string tag)
        {
            if (item == null || tag == null) return;
            if (tag == "power_source_item_energy" || tag == "CURRENT_CONVERTER_ON_BOOL" ||
                tag == "CURRENT_CONVERTER_RATE_FLOAT" || tag == "HYPERTECH_BATTERY_OUTPUT_INT" ||
                tag == "machinery_drawn_power" || tag == "is_machinery_powered" || tag == "IS_OWNED_TAG")
                QueueRefresh();
        }

        internal static void Clear()
        {
            Canvas.willRenderCanvases -= Flush;
            queued = false;
            if (vendingOverlay != null) UnityEngine.Object.Destroy(vendingOverlay);
            if (fountainOverlay != null) UnityEngine.Object.Destroy(fountainOverlay);
            vendingOverlay = null; fountainOverlay = null;
        }
    }

    [HarmonyPatch(typeof(StoreVendingMachine), "Start")]
    internal static class ScenePowerWarningVendingPatch
    { public static void Postfix() { ScenePowerWarningMarker.QueueRefresh(); } }

    [HarmonyPatch(typeof(StoreVendingFountain), "Start")]
    internal static class ScenePowerWarningFountainPatch
    { public static void Postfix() { ScenePowerWarningMarker.QueueRefresh(); } }

    [HarmonyPatch(typeof(GameSlotInventory), "UncheckedAccept", new[] { typeof(GameItem) })]
    internal static class ScenePowerWarningVendingUpdatePatch
    { public static void Postfix() { ScenePowerWarningMarker.QueueRefresh(); } }

    [HarmonyPatch(typeof(GameSlotInventory), "Expel", new[] { typeof(GameItem) })]
    internal static class ScenePowerWarningFountainUpdatePatch
    { public static void Postfix() { ScenePowerWarningMarker.QueueRefresh(); } }

    [HarmonyPatch(typeof(GameItem), "ModifyTag", new[] { typeof(string), typeof(Action<TagState>), typeof(bool) })]
    internal static class ScenePowerWarningTagPatch
    { public static void Postfix(GameItem __instance, string tagName) { ScenePowerWarningMarker.NotifyTagChanged(__instance, tagName); } }

    [HarmonyPatch(typeof(ModEntry), "RefreshAfterConfigApplied")]
    internal static class ScenePowerWarningConfigPatch
    { public static void Postfix() { ScenePowerWarningMarker.RefreshAll(); } }

    [HarmonyPatch(typeof(ModEntry), "OnDisable")]
    internal static class ScenePowerWarningDisablePatch
    { public static void Postfix() { ScenePowerWarningMarker.Clear(); } }

    [HarmonyPatch(typeof(ModEntry), "OnEnable")]
    internal static class ScenePowerWarningEnablePatch
    { public static void Postfix() { ScenePowerWarningMarker.QueueRefresh(); } }

    [HarmonyPatch(typeof(EmporiumEntry), "OnArriveStore", new[] { typeof(bool) })]
    internal static class ScenePowerWarningArrivalPatch
    { public static void Postfix() { ScenePowerWarningMarker.QueueRefresh(); } }
}
