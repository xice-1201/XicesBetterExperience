using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    /// <summary>Shows output-limit warnings for supported player-owned machines.</summary>
    public static class OverflowWarningMarker
    {
        private static readonly Dictionary<GameItemElement, TreeNodeRender> overlays =
            new Dictionary<GameItemElement, TreeNodeRender>();
        private static readonly Dictionary<TreeNodeRender, MarkerGraphic> graphics = new Dictionary<TreeNodeRender, MarkerGraphic>();
        private static readonly HashSet<GameItemElement> machines = new HashSet<GameItemElement>();
        private static bool refreshQueued;
        private static int runningFarmCount = -1;
        private static bool farmCountDirty = true;

        private static bool IsSupported(GameItem item)
        {
            return item != null && (item.identifier == "moisture_farm" || item.identifier == "water_purifier" || item.identifier == "evaporator");
        }

        // Subscribe only after a relevant mutation, and unsubscribe before doing
        // any work. Multiple liquid/tag/module changes share one render batch.
        private static void QueueRefresh()
        {
            if (refreshQueued || !ModEntry.IsEnabled || !ExperienceSettings.OverflowWarning) return;
            refreshQueued = true;
            Canvas.willRenderCanvases += FlushChanges;
        }

        private static void CancelRefresh()
        {
            Canvas.willRenderCanvases -= FlushChanges;
            refreshQueued = false;
        }

        private static int CountRunningFarms()
        {
            return PlayerStore.IsInstanceExist() && EmporiumEntry.Instance != null ? MachineMoistureFarm.CountRunningFarms() : 0;
        }

        private static void FlushChanges()
        {
            CancelRefresh();
            if (!ModEntry.IsEnabled || !ExperienceSettings.OverflowWarning || machines.Count == 0) return;
            try
            {
                // Do not use the native tooltip's time-based count cache: an
                // insertion and removal in the same 0.5s window must be visible.
                // Purifier-only mutations do not invalidate shared-air output.
                if (farmCountDirty && new List<GameItemElement>(machines).Exists(item => item.identifier == "moisture_farm"))
                {
                    runningFarmCount = CountRunningFarms();
                    farmCountDirty = false;
                }
                foreach (GameItemElement machine in new List<GameItemElement>(machines)) PowerWarningMarker.Refresh(machine);
            }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not refresh changed machines: " + error); }
        }

        public static void NotifyChanged(GameItem item)
        {
            if (!ModEntry.IsEnabled || !ExperienceSettings.OverflowWarning || item == null) return;
            if (IsSupported(item))
            {
                GameItemElement machine = item as GameItemElement;
                if (machine != null && !machine.IsDestroyed()) machines.Add(machine);
                if (item.identifier == "moisture_farm") farmCountDirty = true;
                QueueRefresh();
                return;
            }
            GameItem parent = item.parentInventory == null ? null : item.parentInventory.GetParentItem();
            if (IsSupported(parent)) NotifyChanged(parent);
        }

        internal static void NotifyTagChanged(GameItem item, string tag)
        {
            if (!ModEntry.IsEnabled || !ExperienceSettings.OverflowWarning || item == null || tag == null) return;
            if (IsSupported(item))
            {
                switch (tag)
                {
                    case "IS_OWNED_TAG": case "BROKEN_MACHINE_TAG": case "is_machinery_powered":
                    case "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT": case "TOTAL_PERCENTAGE_POWER_BONUS_INT":
                    case "TOTAL_PERCENTAGE_QUALITY_BONUS_INT":
                    case "machinery_drawn_power": case "LOW_POWER_MODE_BOOL":
                        NotifyChanged(item); break;
                }
            }
            else if (tag.StartsWith("CURRENT_PART_", StringComparison.Ordinal) ||
                tag == "LIQUID_CONTAINER_CAPACITY" || tag == "power_source_item_energy" ||
                tag == "CURRENT_CONVERTER_ON_BOOL" || tag == "CURRENT_CONVERTER_RATE_FLOAT" ||
                tag == "HYPERTECH_BATTERY_OUTPUT_INT") NotifyChanged(item);
        }

        public static bool NeedsWarning(GameItem item)
        {
            if (!IsSupported(item) || !GeneralHelper.IsItemOwned(item)) return false;
            if (item.identifier == "evaporator")
            {
                int harvest = EstimateEvaporatorHarvest(item);
                if (harvest <= 0) return false;
                GameSlotInventory outputSlot = MachineHelper.GetSlot(item, 3) as GameSlotInventory;
                GameItem output = outputSlot == null ? null : outputSlot.childItem;
                return output == null || WaterHelper.GetFreeCapacity(output) < harvest;
            }
            GameSlotInventory waterSlot = MachineHelper.GetSlot(item, 2) as GameSlotInventory;
            GameItem container = waterSlot == null ? null : waterSlot.childItem;
            if (item.identifier == "water_purifier")
            {
                return container != null && WaterHelper.GetWaterPurity(container) >=
                    MachinePurifier.GetMaxPurityCap(MachineryHelper.GetCurrentQualityBonus(item));
            }
            if (container == null) return true;
            int freeCapacity = WaterHelper.GetFreeCapacity(container);
            if (runningFarmCount < 0) runningFarmCount = CountRunningFarms();
            int sharingCount = MachineMoistureFarm.CanRunTonight(item) ? Math.Max(1, runningFarmCount) : runningFarmCount + 1;
            int nightlyOutput = MachineMoistureFarm.ApplySharedAir(
                MachineMoistureFarm.GetOutputVolume(item),
                sharingCount);
            return freeCapacity < nightlyOutput;
        }

        // Predict the native cycle without consuming input, power or modules.
        // Allocate evaporation across ALL volatile liquids before applying the
        // solvent filter, including the native last-liquid remainder and rounding.
        internal static int EstimateEvaporatorHarvest(GameItem machine)
        {
            GameSlotInventory inputSlot = MachineHelper.GetSlot(machine, 2) as GameSlotInventory;
            GameItem input = inputSlot == null ? null : inputSlot.childItem;
            if (input == null) return 0;
            int quality = Math.Max(0, Math.Min(100, MachineryHelper.GetCurrentQualityBonus(machine)));
            if (quality == 0) return 0;
            float multiplier = Math.Max(0f, 1f + (float)MachineryHelper.GetCurrentPerformanceBonus(machine) / 100f);
            int evaporation = (int)Math.Round(300000f * multiplier);
            if (evaporation <= 0) return 0;
            int available = 0, remainingKinds = 0;
            foreach (Liquid liquid in Liquid.Liquids)
            {
                if (!liquid.isEvaporative) continue;
                int amount = input.GetTagReadonly(Liquid.GetCurrentPartFromId(liquid.identifier)).GetInt();
                available += amount;
                if (amount > 0) remainingKinds++;
            }
            if (available <= 0) return 0;
            evaporation = Math.Min(evaporation, available);
            bool alcoholOnly = false;
            foreach (GameItem module in MachineHelper.GetModules(machine))
                if (module.identifier == "evaporator_module_solvent_recovery") alcoholOnly = true;
            int allocated = 0, harvest = 0;
            foreach (Liquid liquid in Liquid.Liquids)
            {
                if (!liquid.isEvaporative) continue;
                int amount = input.GetTagReadonly(Liquid.GetCurrentPartFromId(liquid.identifier)).GetInt();
                if (amount <= 0) continue;
                remainingKinds--;
                int share = remainingKinds == 0 ? evaporation - allocated : (int)((long)evaporation * amount / available);
                share = Math.Max(0, Math.Min(amount, share));
                allocated += share;
                if (!alcoholOnly || liquid.IsLiquid("alcohol"))
                    harvest += (int)Math.Round(share * ((double)quality / 100.0));
            }
            return harvest;
        }

        public static void Refresh(GameItem item)
        {
            GameItemElement element = item as GameItemElement;
            if (element == null) return;
            try { RefreshElement(element); }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not refresh overflow warning: " + error); }
        }

        private static void RefreshElement(GameItemElement item)
        {
            if (item.IsDestroyed() || item.handler == null) { Remove(item); return; }
            if (IsSupported(item) && machines.Add(item))
            {
                if (item.identifier == "moisture_farm") farmCountDirty = true;
                QueueRefresh();
            }
            TreeNodeRender overlay;
            bool active = ModEntry.IsEnabled && ExperienceSettings.OverflowWarning && NeedsWarning(item) &&
                !PowerWarningMarker.IsWarningActive(item);
            if (!overlays.TryGetValue(item, out overlay))
            {
                if (!active) return;
                overlay = Create(item.middle);
                if (overlay == null) return;
                overlays.Add(item, overlay);
            }
            Set(overlay, item.background.image.enabled && active);
        }

        private static TreeNodeRender Create(TreeNodeRender parent)
        {
            if (parent == null) return null;
            TreeNodeRender node = TreeNodeRender.Instantiate().SetParent(parent);
            node.gameObject.name = "XicesBetterExperience.OverflowWarning";
            MarkerGraphic graphic = MarkerGraphic.Attach(node);
            graphics.Add(node, graphic);
            graphic.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            graphic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            graphic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            graphic.rectTransform.sizeDelta = new Vector2(30f, 30f);
            graphic.UpdateGeometry(BuildIcon(30f));
            node.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            node.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            node.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            node.rectTransform.sizeDelta = new Vector2(30f, 30f);
            node.rectTransform.anchoredPosition = Vector2.zero;
            return node;
        }

        // Vector tool chest with a separate top-right X badge. Coordinates are
        // normalized to a square so the marker remains sharp at any UI scale.
        internal static List<MarkerTriangle> BuildIcon(float size)
        {
            var result = new List<MarkerTriangle>();
            Color red = new Color(0.95f, 0.12f, 0.10f, 0.78f);
            Color dark = new Color(0.22f, 0.06f, 0.05f, 0.78f);
            Color light = new Color(1f, 0.58f, 0.32f, 0.78f);
            // Partition the chest instead of painting translucent details on
            // top of its body: overlapping alpha makes the details look opaque.
            AddRect(result, 0.12f, 0.16f, 0.88f, 0.28f, red, size);
            AddRect(result, 0.12f, 0.28f, 0.43f, 0.43f, red, size);
            AddRect(result, 0.57f, 0.28f, 0.88f, 0.43f, red, size);
            AddRect(result, 0.12f, 0.43f, 0.88f, 0.50f, red, size);
            AddRect(result, 0.08f, 0.50f, 0.92f, 0.63f, dark, size);
            AddRect(result, 0.12f, 0.63f, 0.34f, 0.68f, red, size);
            AddRect(result, 0.66f, 0.63f, 0.88f, 0.68f, red, size);
            AddRect(result, 0.40f, 0.63f, 0.60f, 0.66f, red, size);
            AddRect(result, 0.34f, 0.63f, 0.40f, 0.84f, dark, size);
            AddRect(result, 0.60f, 0.63f, 0.66f, 0.84f, dark, size);
            AddRect(result, 0.40f, 0.78f, 0.60f, 0.84f, dark, size);
            AddRect(result, 0.40f, 0.66f, 0.60f, 0.78f, light, size);
            AddRect(result, 0.43f, 0.28f, 0.57f, 0.43f, light, size);
            // One X silhouette, without a backing tile or overlapping strokes.
            AddCross(result, red, size);
            return result;
        }

        private static void AddRect(List<MarkerTriangle> list, float x0, float y0, float x1, float y1, Color color, float size)
        {
            Vector2 a = Point(x0, y0, size), b = Point(x1, y0, size), c = Point(x1, y1, size), d = Point(x0, y1, size);
            list.Add(new MarkerTriangle(a, b, c, color)); list.Add(new MarkerTriangle(a, c, d, color));
        }

        private static void AddCross(List<MarkerTriangle> list, Color color, float size)
        {
            const float length = 0.105f, thickness = 0.035f;
            Vector2[] outline = {
                new Vector2(-length, length - thickness), new Vector2(-length + thickness, length),
                new Vector2(0f, thickness), new Vector2(length - thickness, length),
                new Vector2(length, length - thickness), new Vector2(thickness, 0f),
                new Vector2(length, -length + thickness), new Vector2(length - thickness, -length),
                new Vector2(0f, -thickness), new Vector2(-length + thickness, -length),
                new Vector2(-length, -length + thickness), new Vector2(-thickness, 0f)
            };
            Vector2 center = Point(0.835f, 0.825f, size);
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % outline.Length];
                list.Add(new MarkerTriangle(center, Point(0.835f + a.x, 0.825f + a.y, size),
                    Point(0.835f + b.x, 0.825f + b.y, size), color));
            }
        }

        private static Vector2 Point(float x, float y, float size) { return new Vector2((x - 0.5f) * size, (y - 0.5f) * size); }

        private static void Set(TreeNodeRender node, bool visible)
        {
            node.rectTransform.SetAsLastSibling();
            graphics[node].enabled = visible;
        }

        public static void Remove(GameItemElement item)
        {
            if (machines.Remove(item))
            {
                if (item.identifier == "moisture_farm") farmCountDirty = true;
                QueueRefresh();
            }
            TreeNodeRender node;
            if (!overlays.TryGetValue(item, out node)) return;
            overlays.Remove(item);
            graphics[node].enabled = false;
            graphics.Remove(node);
            if (node != null) UnityEngine.Object.Destroy(node.gameObject);
        }

        public static void Clear()
        {
            foreach (GameItemElement item in new List<GameItemElement>(overlays.Keys)) Remove(item);
            machines.Clear();
            CancelRefresh();
            runningFarmCount = -1;
            farmCountDirty = true;
        }

        public static void RefreshLoaded()
        {
            CancelRefresh();
            runningFarmCount = -1;
            farmCountDirty = true;
            foreach (GameItemElement item in new List<GameItemElement>(overlays.Keys)) Refresh(item);
            if (!ModEntry.IsEnabled || !ExperienceSettings.OverflowWarning || !PlayerStore.IsInstanceExist() || EmporiumEntry.Instance == null) return;
            foreach (GameItem item in PlayerStore.Instance.FindAllItem(false)) Refresh(item);
        }

        // Liquid mutations notify the owning machine without an inventory scan.
        private static void RefreshContainer(GameItem container)
        {
            try
            {
                GameInventory inventory = container == null ? null : container.parentInventory;
                GameItem parent = inventory == null ? null : inventory.GetParentItem();
                if (IsSupported(parent)) NotifyChanged(parent);
            }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Could not refresh overflow warning after liquid change: " + error); }
        }

        [HarmonyPatch(typeof(GameItemElement), "ValidateSelect", new[] { typeof(bool) })]
        public static class ValidatePatch { public static void Postfix(GameItemElement __instance) { Refresh(__instance); } }
        [HarmonyPatch(typeof(GameItemElement), "ToggleBackground", new[] { typeof(bool), typeof(float), typeof(float) })]
        public static class VisibilityPatch { public static void Postfix(GameItemElement __instance) { Refresh(__instance); } }
        [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", new[] { typeof(bool), typeof(Vector4) })]
        public static class DragPatch { public static void Postfix(GameItemElement __instance) { Refresh(__instance); } }
        [HarmonyPatch(typeof(GameItemElement), "UpdateRectTransform", new Type[] { })]
        public static class RotationPatch { public static void Postfix(GameItemElement __instance) { Refresh(__instance); } }
        [HarmonyPatch(typeof(GameItemElement), "Destroy", new Type[] { })]
        public static class DestroyPatch { public static void Prefix(GameItemElement __instance) { Remove(__instance); } }

        [HarmonyPatch(typeof(WaterHelper), "TransferLiquid", new[] { typeof(GameItem), typeof(GameItem) })]
        public static class TransferPatch { public static void Postfix(GameItem sourceContainer, GameItem targetContainer) { RefreshContainer(sourceContainer); RefreshContainer(targetContainer); } }
        [HarmonyPatch(typeof(WaterHelper), "EmptyContainer", new[] { typeof(GameItem) })]
        public static class EmptyPatch { public static void Postfix(GameItem gameItem) { RefreshContainer(gameItem); } }
        [HarmonyPatch(typeof(WaterHelper), "AddLiquid", new[] { typeof(GameItem), typeof(string), typeof(int) })]
        public static class AddPatch { public static void Postfix(GameItem waterContainer) { RefreshContainer(waterContainer); } }
        [HarmonyPatch(typeof(WaterHelper), "RemoveMl", new[] { typeof(GameItem), typeof(int) })]
        public static class RemoveMlPatch { public static void Postfix(GameItem container) { RefreshContainer(container); } }
        [HarmonyPatch(typeof(WaterHelper), "Remove", new[] { typeof(GameItem), typeof(int) })]
        public static class RemovePatch { public static void Postfix(GameItem container) { RefreshContainer(container); } }
        [HarmonyPatch(typeof(WaterHelper), "RemoveAndReturn", new[] { typeof(GameItem), typeof(int) })]
        public static class RemoveAndReturnPatch { public static void Postfix(GameItem container) { RefreshContainer(container); } }

        [HarmonyPatch(typeof(GameSlotInventory), "UncheckedAccept", new[] { typeof(GameItem) })]
        public static class SlotInsertPatch
        {
            public static void Postfix(GameSlotInventory __instance)
            {
                GameItem machine = __instance == null ? null : __instance.GetParentItem();
                if (IsSupported(machine)) NotifyChanged(machine);
            }
        }
        [HarmonyPatch(typeof(GameSlotInventory), "Expel", new[] { typeof(GameItem) })]
        public static class SlotRemovePatch
        {
            public static void Postfix(GameSlotInventory __instance)
            {
                GameItem machine = __instance == null ? null : __instance.GetParentItem();
                if (IsSupported(machine)) NotifyChanged(machine);
            }
        }
        [HarmonyPatch(typeof(GameGridInventory), "UncheckedAccept", new[] { typeof(GameItem) })]
        public static class GridInsertPatch
        {
            public static void Postfix(GameGridInventory __instance, GameItem __0)
            { NotifyChanged(__0); NotifyChanged(__instance == null ? null : __instance.GetParentItem()); }
        }
        [HarmonyPatch(typeof(GameGridInventory), "Expel", new[] { typeof(GameItem) })]
        public static class GridRemovePatch
        {
            public static void Postfix(GameGridInventory __instance, GameItem __0)
            { NotifyChanged(__0); NotifyChanged(__instance == null ? null : __instance.GetParentItem()); }
        }
        [HarmonyPatch(typeof(GameItem), "ModifyTag", new[] { typeof(string), typeof(Action<TagState>), typeof(bool) })]
        public static class TagValuePatch
        { public static void Postfix(GameItem __instance, string tagName) { NotifyTagChanged(__instance, tagName); } }
        [HarmonyPatch(typeof(GameItem), "EnableTag", new[] { typeof(string), typeof(bool) })]
        public static class TagEnablePatch
        { public static void Postfix(GameItem __instance, string tagName, bool __result) { if (__result) NotifyTagChanged(__instance, tagName); } }
        [HarmonyPatch(typeof(GameItem), "DisableTag", new[] { typeof(string), typeof(bool) })]
        public static class TagDisablePatch
        { public static void Postfix(GameItem __instance, string tagName, bool __result) { if (__result) NotifyTagChanged(__instance, tagName); } }
    }
}
