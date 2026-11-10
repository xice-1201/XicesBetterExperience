using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace XicesBetterExperience
{
    public static class ItemStatusBackground
    {
        private sealed class Layer
        {
            public TreeNodeRender Node;
            public MarkerGraphic Graphic;
            public Layer(TreeNodeRender parent)
            {
                Node = TreeNodeRender.Instantiate().SetParent(parent);
                Node.gameObject.name = "XicesBetterExperience.StatusBackground";
                Graphic = MarkerGraphic.Attach(Node);
            }
        }
        private sealed class Overlay { public Layer Normal, Drag; public int[] ShapeMask; public int ShapeWidth, ShapeHeight; public float Grid; public bool Stolen, Contraband; public bool HasGeometry; }
        private static readonly Dictionary<GameItemElement, Overlay> overlays = new Dictionary<GameItemElement, Overlay>();
        private static readonly Color yellow = new Color(1f, 0.82f, 0.08f, 0.25f);
        private static readonly Color red = new Color(1f, 0.12f, 0.12f, 0.25f);

        public static void Refresh(GameItem item)
        {
            GameItemElement element = item as GameItemElement;
            if (element == null) return;
            try { RefreshElement(element); }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Status background refresh failed: " + error); }
        }

        private static void RefreshElement(GameItemElement item)
        {
            if (item.IsDestroyed() || item.handler == null) { Remove(item); return; }
            bool stolen = ModEntry.IsEnabled && ExperienceSettings.StolenMarker && StolenHeatMarker.HasHeat(item);
            bool contraband = ModEntry.IsEnabled && ExperienceSettings.ContrabandMarker && ContrabandMarker.IsContraband(item);
            Overlay overlay;
            if (!overlays.TryGetValue(item, out overlay))
            {
                if (!stolen && !contraband) return;
                overlay = new Overlay { Normal = new Layer(item.middle), Drag = new Layer(item.fakeMiddle) };
                overlays.Add(item, overlay);
            }
            float grid = UISettings.current.gridSize;
            var shape = item.modifiedShape;
            int width = shape.maxX - shape.minX + 1, height = shape.maxY - shape.minY + 1;
            bool shapeChanged = !overlay.HasGeometry || overlay.ShapeWidth != width || overlay.ShapeHeight != height || overlay.ShapeMask == null;
            if (!shapeChanged)
            {
                int maskIndex = 0;
                for (int y = shape.minY; y <= shape.maxY && !shapeChanged; y++)
                    for (int x = shape.minX; x <= shape.maxX; x++, maskIndex++)
                        if (overlay.ShapeMask[maskIndex] != shape.Get(x, y)) { shapeChanged = true; break; }
            }
            bool rebuild = shapeChanged || overlay.Grid != grid || overlay.Stolen != stolen || overlay.Contraband != contraband;
            List<MarkerTriangle> triangles = rebuild ? Build(item, stolen, contraband) : null;
            if (rebuild)
            {
                overlay.ShapeWidth = width; overlay.ShapeHeight = height;
                overlay.ShapeMask = new int[width * height];
                int maskIndex = 0;
                for (int y = shape.minY; y <= shape.maxY; y++)
                    for (int x = shape.minX; x <= shape.maxX; x++) overlay.ShapeMask[maskIndex++] = shape.Get(x, y);
                overlay.Grid = grid; overlay.Stolen = stolen; overlay.Contraband = contraband; overlay.HasGeometry = true;
            }
            Vector2 size = new Vector2((item.modifiedShape.maxX - item.modifiedShape.minX + 1) * UISettings.current.gridSize,
                (item.modifiedShape.maxY - item.modifiedShape.minY + 1) * UISettings.current.gridSize);
            overlay.Normal.Node.rectTransform.sizeDelta = size;
            overlay.Drag.Node.rectTransform.sizeDelta = size;
            Set(overlay.Normal, triangles, item.background.image.enabled, rebuild);
            Set(overlay.Drag, triangles, item.fakeBackground.image.enabled, rebuild);
        }

        internal static List<MarkerTriangle> Build(GameItemElement item, bool stolen, bool contraband)
        {
            var result = new List<MarkerTriangle>();
            if (!stolen && !contraband) return result;
            var shape = item.modifiedShape;
            float grid = UISettings.current.gridSize;
            float width = (shape.maxX - shape.minX + 1) * grid;
            float height = (shape.maxY - shape.minY + 1) * grid;
            for (int y = shape.minY; y <= shape.maxY; y++)
                for (int x = shape.minX; x <= shape.maxX; x++)
                {
                    if (shape.Get(x, y) == 0) continue;
                    float left = (x - shape.minX) * grid, top = (y - shape.minY) * grid;
                    var cell = new List<Vector2> { new Vector2(left, top), new Vector2(left + grid, top),
                        new Vector2(left + grid, top + grid), new Vector2(left, top + grid) };
                    if (stolen && contraband)
                    {
                        // Split the whole current bounding rectangle from top-right
                        // to bottom-left, clipping each occupied cell to that line.
                        AddPolygon(result, Clip(cell, width, height, true), yellow);
                        AddPolygon(result, Clip(cell, width, height, false), red);
                    }
                    else AddPolygon(result, cell, stolen ? yellow : red);
                }
            return result;
        }

        private static List<Vector2> Clip(List<Vector2> polygon, float width, float height, bool first)
        {
            var output = new List<Vector2>();
            Vector2 a = polygon[polygon.Count - 1];
            float da = a.x / width + a.y / height - 1f;
            foreach (Vector2 b in polygon)
            {
                float db = b.x / width + b.y / height - 1f;
                bool insideA = first ? da <= 0f : da >= 0f;
                bool insideB = first ? db <= 0f : db >= 0f;
                if (insideA != insideB)
                {
                    float t = da / (da - db);
                    output.Add(new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t));
                }
                if (insideB) output.Add(b);
                a = b; da = db;
            }
            return output;
        }

        private static void AddPolygon(List<MarkerTriangle> target, List<Vector2> polygon, Color color)
        {
            for (int i = 1; i + 1 < polygon.Count; i++)
                target.Add(new MarkerTriangle(new Vector2(polygon[0].x, -polygon[0].y),
                    new Vector2(polygon[i].x, -polygon[i].y), new Vector2(polygon[i + 1].x, -polygon[i + 1].y), color));
        }

        private static void Set(Layer layer, List<MarkerTriangle> triangles, bool visible, bool rebuild)
        {
            layer.Node.rectTransform.SetAsFirstSibling();
            if (rebuild) layer.Graphic.UpdateGeometry(triangles);
            layer.Graphic.enabled = visible && (rebuild ? triangles.Count > 0 : layer.Graphic.Triangles.Count > 0);
        }

        public static void Remove(GameItemElement item)
        {
            Overlay overlay;
            if (!overlays.TryGetValue(item, out overlay)) return;
            overlays.Remove(item);
            foreach (Layer layer in new[] { overlay.Normal, overlay.Drag })
            {
                layer.Graphic.enabled = false;
                UnityEngine.Object.Destroy(layer.Node.gameObject);
            }
        }
        public static void Clear() { foreach (var item in new List<GameItemElement>(overlays.Keys)) Remove(item); }
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
    public static class StatusBackgroundValidatePatch { public static void Postfix(GameItemElement __instance) { ItemStatusBackground.Refresh(__instance); } }
    [HarmonyPatch(typeof(GameItemElement), "ToggleBackground", new[] { typeof(bool), typeof(float), typeof(float) })]
    public static class StatusBackgroundVisibilityPatch { public static void Postfix(GameItemElement __instance) { ItemStatusBackground.Refresh(__instance); } }
    [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", new[] { typeof(bool), typeof(Vector4) })]
    public static class StatusBackgroundDragPatch { public static void Postfix(GameItemElement __instance) { ItemStatusBackground.Refresh(__instance); } }
    [HarmonyPatch(typeof(GameItemElement), "UpdateRectTransform", new Type[] { })]
    public static class StatusBackgroundRotationPatch { public static void Postfix(GameItemElement __instance) { ItemStatusBackground.Refresh(__instance); } }
    [HarmonyPatch(typeof(GameItemElement), "Destroy", new Type[] { })]
    public static class StatusBackgroundDestroyPatch { public static void Prefix(GameItemElement __instance) { ItemStatusBackground.Remove(__instance); } }

}
