using System;
using System.Globalization;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    // Independent global layouts for the tablet and phone, shared across save slots.
    public static class WindowLayoutMemory
    {
        private const string TabletId = "negociation_ui";
        private static int generation;
        private sealed class RestoreJob { public GameObject Root; public int Generation; public bool Ready; }
        private static readonly Dictionary<GameObject, RestoreJob> pending = new Dictionary<GameObject, RestoreJob>();
        private static bool restoring;
        private static bool Enabled { get { return ModEntry.IsEnabled && ExperienceSettings.RetainWindowLayout; } }

        internal struct Layout
        {
            public float X, Y;
            public int Zoom;
            public string Encode()
            {
                return X.ToString("R", CultureInfo.InvariantCulture) + ";" +
                    Y.ToString("R", CultureInfo.InvariantCulture) + ";" + Zoom.ToString(CultureInfo.InvariantCulture);
            }
            public static bool TryDecode(string value, out Layout layout)
            {
                layout = new Layout();
                string[] parts = (value ?? "").Split(';');
                return parts.Length == 3 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out layout.X) &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out layout.Y) &&
                    int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out layout.Zoom) &&
                    !float.IsNaN(layout.X) && !float.IsInfinity(layout.X) && !float.IsNaN(layout.Y) && !float.IsInfinity(layout.Y) &&
                    layout.X >= -1f && layout.X <= 2f && layout.Y >= -1f && layout.Y <= 2f && layout.Zoom >= 1 && layout.Zoom <= 3;
            }
        }

        private static bool IsTablet(StoreUI ui) { return ui != null && ui.identifier == TabletId; }
        private static bool IsPhone(StoreUI ui)
        {
            PhoneUIManager phone = PhoneUIManager.Instance;
            return ui != null && phone != null && phone.phonePanel == ui.gameObject;
        }
        private static bool IsSupported(StoreUI ui) { return IsTablet(ui) || IsPhone(ui); }
        private static string SavedLayout(StoreUI ui) { return IsTablet(ui) ? ExperienceSettings.TabletLayout : ExperienceSettings.PhoneLayout; }
        private static void Cancel(GameObject root)
        {
            if (root != null) pending.Remove(root);
            if (pending.Count == 0) Canvas.willRenderCanvases -= FinishRestore;
        }
        private static Camera UICamera(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        public static void Clear()
        {
            generation++;
            pending.Clear();
            Canvas.willRenderCanvases -= FinishRestore;
        }

        internal static void QueueRestore(StoreUI ui)
        {
            Layout layout;
            if (!Enabled || !IsSupported(ui) || !ui.gameObject.activeInHierarchy || !Layout.TryDecode(SavedLayout(ui), out layout)) return;
            GameObject root = ui.gameObject;
            RestoreJob job = new RestoreJob { Root = root, Generation = generation };
            pending[root] = job;
            // Native OnEnable docks next update, then reclamps the next update.
            // Restore before rendering only after both operations have completed.
            LetterboxManager.RunNextUpdate(() => LetterboxManager.RunNextUpdate(() =>
            {
                RestoreJob current;
                if (job.Generation == generation && root != null && root.activeInHierarchy && Enabled &&
                    pending.TryGetValue(root, out current) && current == job)
                {
                    job.Ready = true;
                    Canvas.willRenderCanvases -= FinishRestore;
                    Canvas.willRenderCanvases += FinishRestore;
                }
                else if (root != null && pending.TryGetValue(root, out current) && current == job) Cancel(root);
            }));
        }

        private static void FinishRestore()
        {
            Canvas.willRenderCanvases -= FinishRestore;
            foreach (RestoreJob job in new List<RestoreJob>(pending.Values))
            {
                if (!job.Ready) continue;
                pending.Remove(job.Root);
                Restore(job.Root);
            }
        }

        private static void Restore(GameObject root)
        {
            Layout layout;
            if (!Enabled || root == null || !root.activeInHierarchy) return;
            StoreUI ui = root.GetComponent<StoreUI>();
            if (!IsSupported(ui) || !Layout.TryDecode(SavedLayout(ui), out layout)) return;
            RectTransform rect = root.transform as RectTransform;
            RectTransform parent = rect == null ? null : rect.parent as RectTransform;
            if (parent == null || Screen.width <= 0 || Screen.height <= 0) return;
            try
            {
                restoring = true;
                Camera camera = UICamera(rect);
                Vector3 world;
                if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent,
                    new Vector2(layout.X * Screen.width, layout.Y * Screen.height), camera, out world)) return;
                rect.position = world;
                // Keep the header reachable after changing display dimensions.
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                float dx = bottomLeft.x > Screen.width - 40f ? Screen.width - 40f - bottomLeft.x :
                    topRight.x < 40f ? 40f - topRight.x : 0f;
                float dy = topRight.y > Screen.height ? Screen.height - topRight.y :
                    topRight.y < 40f ? 40f - topRight.y : 0f;
                if ((dx != 0f || dy != 0f) && RectTransformUtility.ScreenPointToWorldPointInRectangle(parent,
                    new Vector2(layout.X * Screen.width + dx, layout.Y * Screen.height + dy), camera, out world)) rect.position = world;
            }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not restore window layout: " + error); }
            finally { restoring = false; }
        }

        internal static void Capture(StoreUI ui, bool zoomChanged)
        {
            if (!Enabled || restoring || !IsSupported(ui) || !ui.gameObject.activeInHierarchy || Screen.width <= 0 || Screen.height <= 0) return;
            if (!zoomChanged && pending.ContainsKey(ui.gameObject)) return;
            RectTransform rect = ui.transform as RectTransform;
            if (rect == null) return;
            try
            {
                Layout old;
                int zoom = Layout.TryDecode(SavedLayout(ui), out old) ? old.Zoom : 1;
                int nativeZoom;
                if (IsTablet(ui) && (zoomChanged || String.IsNullOrEmpty(ExperienceSettings.TabletLayout)) && PlayerStore.IsInstanceExist() &&
                    PlayerStore.Instance.storeUIZoomLevels != null && PlayerStore.Instance.storeUIZoomLevels.TryGetValue(TabletId, out nativeZoom))
                    zoom = Math.Max(1, Math.Min(3, nativeZoom));
                Vector2 point = RectTransformUtility.WorldToScreenPoint(UICamera(rect), rect.position);
                Layout layout = new Layout { X = point.x / Screen.width, Y = point.y / Screen.height, Zoom = zoom };
                if (zoomChanged) Cancel(ui.gameObject);
                if (IsTablet(ui)) ExperienceSettings.SaveTabletLayout(layout.Encode());
                else ExperienceSettings.SavePhoneLayout(layout.Encode());
            }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not save window layout: " + error); }
        }

        public static void ConfigApplied()
        {
            Clear();
            NegociationUIManager manager = NegociationUIManager.Instance;
            if (!Enabled) return;
            if (manager != null && manager.uiObject != null && manager.uiObject.activeInHierarchy)
            {
                StoreUI ui = manager.uiObject.GetComponent<StoreUI>();
                if (ui != null) ui.OnEnable();
            }
            PhoneUIManager phone = PhoneUIManager.Instance;
            if (phone != null && phone.phonePanel != null && phone.phonePanel.activeInHierarchy)
                QueueRestore(phone.phonePanel.GetComponent<StoreUI>());
        }

        [HarmonyPatch(typeof(StoreUI), "GetSavedZoom", new Type[] { })]
        public static class ZoomReadPatch
        {
            public static void Postfix(StoreUI __instance, ref int __result)
            {
                Layout layout;
                if (Enabled && IsTablet(__instance) && Layout.TryDecode(ExperienceSettings.TabletLayout, out layout)) __result = layout.Zoom;
            }
        }
        [HarmonyPatch(typeof(StoreUI), "OnEnable", new Type[] { })]
        public static class OpenPatch { public static void Postfix(StoreUI __instance) { QueueRestore(__instance); } }
        [HarmonyPatch(typeof(StoreUI), "ToggleZoom", new Type[] { })]
        public static class ZoomChangedPatch { public static void Postfix(StoreUI __instance) { Capture(__instance, true); } }
        [HarmonyPatch(typeof(UIDragHandler), "OnEndDrag", new[] { typeof(UnityEngine.EventSystems.PointerEventData) })]
        public static class DragEndPatch
        {
            public static void Postfix(UIDragHandler __instance, UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (__instance == null || eventData == null || eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left) return;
                StoreUI ui = __instance.GetComponent<StoreUI>();
                if (!IsSupported(ui)) return;
                Cancel(ui.gameObject);
                Capture(ui, false);
            }
        }
        [HarmonyPatch(typeof(NegociationUIManager), "CloseUI", new Type[] { })]
        public static class ClosePatch
        {
            public static void Prefix(NegociationUIManager __instance)
            {
                if (__instance == null || __instance.uiObject == null) return;
                Capture(__instance.uiObject.GetComponent<StoreUI>(), false);
                Cancel(__instance.uiObject);
            }
        }
        [HarmonyPatch(typeof(PhoneUIManager), "OpenUI", new Type[] { })]
        public static class PhoneOpenPatch
        {
            public static void Postfix(PhoneUIManager __instance)
            {
                if (__instance != null && __instance.phonePanel != null)
                    QueueRestore(__instance.phonePanel.GetComponent<StoreUI>());
            }
        }
        [HarmonyPatch(typeof(PhoneUIManager), "CloseUI", new Type[] { })]
        public static class PhoneClosePatch
        {
            public static void Prefix(PhoneUIManager __instance)
            {
                if (__instance == null || __instance.phonePanel == null) return;
                Capture(__instance.phonePanel.GetComponent<StoreUI>(), false);
                Cancel(__instance.phonePanel);
            }
        }
    }
}
