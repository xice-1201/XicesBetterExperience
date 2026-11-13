using System;
using System.Globalization;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    // Independent global layouts, shared across save slots.
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
        private static bool IsWaterScanner(GameObject root)
        {
            WaterScannerUIManager scanner = WaterScannerUIManager.Instance;
            return root != null && scanner != null && scanner.panel == root;
        }
        private static bool IsSupportedRoot(GameObject root)
        {
            return root != null && (IsWaterScanner(root) || IsSupported(root.GetComponent<StoreUI>()));
        }
        private static string SavedRootLayout(GameObject root)
        {
            return IsWaterScanner(root) ? ExperienceSettings.WaterScannerLayout : SavedLayout(root.GetComponent<StoreUI>());
        }
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
            fixtureDragTargets.Clear();
            Canvas.willRenderCanvases -= FinishRestore;
        }

        internal static void QueueRestore(StoreUI ui)
        {
            if (IsSupported(ui)) QueueRootRestore(ui.gameObject);
        }

        internal static void QueueRootRestore(GameObject root)
        {
            Layout layout;
            if (!Enabled || !IsSupportedRoot(root) || !root.activeInHierarchy || !Layout.TryDecode(SavedRootLayout(root), out layout)) return;
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
            if (!IsSupportedRoot(root) || !Layout.TryDecode(SavedRootLayout(root), out layout)) return;
            RestoreRect(root.transform as RectTransform, layout);
        }

        private static void RestoreRect(RectTransform rect, Layout layout)
        {
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
            var dragTargets = new List<KeyValuePair<TreeNodeRender, PixelWindow>>(fixtureDragTargets);
            Clear();
            foreach (var pair in dragTargets)
                if (FixtureKind(pair.Value) != 0) fixtureDragTargets[pair.Key] = pair.Value;
            NegociationUIManager manager = NegociationUIManager.Instance;
            if (!Enabled) return;
            if (manager != null && manager.uiObject != null && manager.uiObject.activeInHierarchy)
            {
                StoreUI ui = manager.uiObject.GetComponent<StoreUI>();
                if (ui != null) ui.OnEnable();
            }
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (entry != null)
            {
                RestoreFixture(entry.dossier == null ? null : entry.dossier.contentWindow);
                RestoreFixture(entry.trashcan == null ? null : entry.trashcan.contentWindow);
                RestoreFixture(VendingWindow(entry));
            }
            WaterScannerUIManager scanner = WaterScannerUIManager.Instance;
            if (scanner != null) QueueRootRestore(scanner.panel);
            PhoneUIManager phone = PhoneUIManager.Instance;
            if (phone != null && phone.phonePanel != null && phone.phonePanel.activeInHierarchy)
                QueueRestore(phone.phonePanel.GetComponent<StoreUI>());
        }

        internal static void CaptureWaterScanner(GameObject root)
        {
            if (!Enabled || restoring || !IsWaterScanner(root) || !root.activeInHierarchy || pending.ContainsKey(root) ||
                Screen.width <= 0 || Screen.height <= 0) return;
            RectTransform rect = root.transform as RectTransform;
            if (rect == null) return;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(UICamera(rect), rect.position);
            ExperienceSettings.SaveWaterScannerLayout(new Layout
            {
                X = point.x / Screen.width, Y = point.y / Screen.height, Zoom = 1
            }.Encode());
        }

        [HarmonyPatch(typeof(WaterScannerUIManager), "Open", new[] { typeof(GameItem), typeof(GameItem) })]
        public static class WaterScannerOpenPatch
        {
            public static void Postfix(WaterScannerUIManager __instance, GameItem water)
            {
                if (__instance != null && water != null) QueueRootRestore(__instance.panel);
            }
        }
        [HarmonyPatch(typeof(WaterScannerUIManager), "Close", new Type[] { })]
        public static class WaterScannerClosePatch
        {
            public static void Prefix(WaterScannerUIManager __instance)
            {
                if (__instance == null) return;
                CaptureWaterScanner(__instance.panel);
                Cancel(__instance.panel);
            }
        }

        // These machinery-style grid windows have fixed native dimensions. Only position is retained.
        private static readonly Dictionary<TreeNodeRender, PixelWindow> fixtureDragTargets = new Dictionary<TreeNodeRender, PixelWindow>();

        private static PixelWindow VendingWindow(EmporiumEntry entry)
        {
            GameItem machine = entry == null || entry.vendingFountainElement == null ? null : entry.vendingFountainElement.childItem;
            return machine == null ? null : machine.contentWindow;
        }

        private static int FixtureKind(PixelWindow window)
        {
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (window == null || entry == null) return 0;
            if (entry.dossier != null && entry.dossier.contentWindow == window) return 1;
            if (entry.trashcan != null && entry.trashcan.contentWindow == window) return 2;
            if (VendingWindow(entry) == window) return 3;
            return 0;
        }

        internal static void RestoreFixture(PixelWindow window)
        {
            int kind = FixtureKind(window);
            Layout layout;
            if (!Enabled || kind == 0 || !window.IsVisible()) return;
            string saved = kind == 1 ? ExperienceSettings.ArchiveLayout :
                kind == 2 ? ExperienceSettings.TrashLayout : ExperienceSettings.VendingFountainLayout;
            if (Layout.TryDecode(saved, out layout)) RestoreRect(window.rectTransform, layout);
        }

        internal static void CaptureFixture(PixelWindow window)
        {
            int kind = FixtureKind(window);
            if (!Enabled || restoring || kind == 0 || !window.IsVisible() || Screen.width <= 0 || Screen.height <= 0) return;
            RectTransform rect = window.rectTransform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(UICamera(rect), rect.position);
            string value = new Layout { X = point.x / Screen.width, Y = point.y / Screen.height, Zoom = 1 }.Encode();
            if (kind == 1) ExperienceSettings.SaveArchiveLayout(value);
            else if (kind == 2) ExperienceSettings.SaveTrashLayout(value);
            else ExperienceSettings.SaveVendingFountainLayout(value);
        }

        internal static void FixtureOpened(PixelWindow window, TreeNodeRender background)
        {
            int kind = FixtureKind(window);
            if (kind == 0) return;
            // Keep at most one current drag target per fixture, including after save recreation.
            foreach (var pair in new List<KeyValuePair<TreeNodeRender, PixelWindow>>(fixtureDragTargets))
                if (FixtureKind(pair.Value) == 0 || FixtureKind(pair.Value) == kind) fixtureDragTargets.Remove(pair.Key);
            if (background != null) fixtureDragTargets[background] = window;
            RestoreFixture(window);
        }

        [HarmonyPatch(typeof(PixelWindow), "Show", new Type[] { })]
        public static class FixtureShowPatch
        {
            public static void Postfix(PixelWindow __instance, TreeNodeRender ___background) { FixtureOpened(__instance, ___background); }
        }
        [HarmonyPatch(typeof(PixelWindow), "Show", new[] { typeof(Vector2), typeof(Vector2), typeof(bool) })]
        public static class FixtureShowAtPatch
        {
            public static void Postfix(PixelWindow __instance, TreeNodeRender ___background) { FixtureOpened(__instance, ___background); }
        }
        // The storefront click calls Show() then Center(); restore after the final native positioning.
        [HarmonyPatch(typeof(PixelWindow), "Center", new Type[] { })]
        public static class VendingCenterPatch
        {
            public static void Postfix(PixelWindow __instance)
            {
                if (FixtureKind(__instance) == 3) RestoreFixture(__instance);
            }
        }
        [HarmonyPatch(typeof(GameItem), "RestoreContentWindowPosition", new Type[] { })]
        public static class FixtureNativeRestorePatch
        {
            public static void Postfix(GameItem __instance) { RestoreFixture(__instance.contentWindow); }
        }
        [HarmonyPatch(typeof(PixelWindow), "Hide", new Type[] { })]
        public static class FixtureHidePatch
        {
            public static void Prefix(PixelWindow __instance) { CaptureFixture(__instance); }
        }
        [HarmonyPatch(typeof(TreeNodeRender), "OnEndDrag", new[] { typeof(UnityEngine.EventSystems.PointerEventData) })]
        public static class FixtureDragPatch
        {
            public static void Postfix(TreeNodeRender __instance, UnityEngine.EventSystems.PointerEventData eventData)
            {
                PixelWindow window;
                if (eventData != null && eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left &&
                    fixtureDragTargets.TryGetValue(__instance, out window)) CaptureFixture(window);
            }
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
                if (IsWaterScanner(__instance.gameObject))
                {
                    Cancel(__instance.gameObject);
                    CaptureWaterScanner(__instance.gameObject);
                    return;
                }
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
