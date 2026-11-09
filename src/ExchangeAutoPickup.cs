using HarmonyLib;
using System;
using System.Collections.Generic;

namespace XicesBetterExperience
{
    internal static class ExchangeAutoPickupState
    {
        private const string Key = "xice.exchange.pending.ids";
        private static readonly List<GameItem> Pending = new List<GameItem>();
        internal static void Capture(ExchangeManager manager)
        {
            if (!ModEntry.IsEnabled || !ExperienceSettings.ExchangeAutoPickup || manager == null ||
                manager.itemsBuffer == null || PlayerStore.Instance == null)
            {
                return;
            }
            int captured = 0;
            foreach (GameItem item in manager.itemsBuffer)
            {
                int id;
                if (item != null)
                {
                    if (!Pending.Contains(item)) Pending.Add(item);
                    if (TryGetUniqueId(item, out id)) AddId(id);
                    captured++;
                }
            }
        }

        internal static void MoveFromTrashcan()
        {
            if (!ModEntry.IsEnabled || !ExperienceSettings.ExchangeAutoPickup ||
                EmporiumEntry.Instance == null || PlayerStore.Instance == null)
            {
                return;
            }
            // The visible counter is populated by the game's native
            // TransferBackInvToCounter routine. Stage rewards in the persistent
            // back inventory first so scene refresh/save logic keeps them.
            GameGridInventory destination = EmporiumEntry.Instance.backInvinvElement;
            GameGridInventory counter = EmporiumEntry.Instance.backInvinvElementCounter;
            // TransferBuffer delivers into the trashcan GameItem itself. The
            // trashcanInvElement is only its UI inventory and is not the native
            // delivery root on the current game build.
            GraphNodeStorage trash = EmporiumEntry.Instance.trashcan;
            if (trash == null) trash = EmporiumEntry.Instance.trashcanInvElement;
            if (destination == null || trash == null)
            {
                return;
            }
            HashSet<int> ids = ReadIds();
            if (ids.Count == 0 && Pending.Count == 0)
            {
                return;
            }
            List<GameItem> all = FindItems(trash);
            var found = new List<GameItem>();
            foreach (GameItem item in all)
            {
                int id;
                if (item != null && (Pending.Contains(item) ||
                    (TryGetUniqueId(item, out id) && ids.Contains(id))))
                    if (!found.Contains(item)) found.Add(item);
            }
            foreach (GameItem item in found)
            {
                GameInventory source = item.parentInventory;
                if (source == null)
                {
                    continue;
                }
                if (!source.Expel(item))
                {
                    continue;
                }
                bool accepted = destination.UncheckedAccept(item);
                int id;
                if (accepted && item.parentInventory == destination)
                {
                    if (TryGetUniqueId(item, out id)) RemoveId(id);
                    Pending.Remove(item);
                }
                else if (item.parentInventory == null)
                {
                    source.UncheckedAccept(item);
                }
            }
        }

        internal static void TransferStagedToCounter()
        {
            if (!ModEntry.IsEnabled || !ExperienceSettings.ExchangeAutoPickup ||
                EmporiumEntry.Instance == null || MapUIManager.Instance == null) return;
            GameGridInventory back = EmporiumEntry.Instance.backInvinvElement;
            if (back == null || back.childItems.Count == 0) return;
            MapUIManager.Instance.TransferBackInvToCounter();
        }

        private static List<GameItem> FindItems(GraphNodeStorage node)
        {
            var result = new List<GameItem>();
            if (node == null) return result;
            foreach (GraphNodeStorage child in node.children)
            {
                GameItem item = child as GameItem;
                if (item != null && !result.Contains(item)) result.Add(item);
                result.AddRange(FindItems(child));
            }
            return result;
        }

        private static bool TryGetUniqueId(GameItem item, out int id)
        {
            id = 0;
            id = item.uniqueId;
            return id != 0;
        }

        private static Dictionary<string, string> Data
        {
            get
            {
                return PlayerStore.Instance == null ? null : PlayerStore.Instance.modData;
            }
        }

        private static HashSet<int> ReadIds()
        {
            var result = new HashSet<int>();
            Dictionary<string, string> data = Data;
            string value = data == null || !data.ContainsKey(Key) ? null : data[Key];
            if (String.IsNullOrEmpty(value)) return result;
            foreach (string part in value.Split(','))
            {
                int id;
                if (Int32.TryParse(part, out id) && id != 0) result.Add(id);
            }
            return result;
        }

        private static void AddId(int id)
        {
            HashSet<int> ids = ReadIds();
            if (!ids.Add(id)) return;
            Dictionary<string, string> data = Data;
            if (data != null) data[Key] = String.Join(",", ids);
        }

        private static void RemoveId(int id)
        {
            Dictionary<string, string> data = Data;
            if (data == null) return;
            HashSet<int> ids = ReadIds();
            ids.Remove(id);
            if (ids.Count == 0) data.Remove(Key);
            else data[Key] = String.Join(",", ids);
        }

        private static bool IsOutside()
        {
            if (MapUIManager.Instance == null) return false;
            return MapUIManager.Instance.isOutside;
        }
    }

    [HarmonyPatch(typeof(ExchangeManager), "TransferBuffer")]
    internal static class ExchangeAutoPickup
    {
        public static bool Prefix(ExchangeManager __instance)
        {
            ExchangeAutoPickupState.Capture(__instance);
            return true;
        }

        // TransferBuffer is called after ClearTrash in HandleJanitorial. The
        // native method is what actually inserts the rewards into the trashcan,
        // so pickup must run after that insertion has completed.
        public static void Postfix()
        {
            ExchangeAutoPickupState.MoveFromTrashcan();
        }
    }

    [HarmonyPatch(typeof(ExchangeManager), "HandleExchange")]
    internal static class ExchangeAutoPickupAfterExchangeHandled
    {
        public static void Postfix(ExchangeManager __instance)
        {
            ExchangeAutoPickupState.Capture(__instance);
        }
    }

[HarmonyPatch(typeof(PlayerStore), "BeginDay")]
internal static class ExchangeAutoPickupAtBeginDay
{
    public static void Postfix() { ExchangeAutoPickupState.MoveFromTrashcan(); }
    }

[HarmonyPatch(typeof(EmporiumEntry), "OnArriveStore")]
internal static class ExchangeAutoPickupAtStoreArrival
{
    public static void Postfix()
    {
        ExchangeAutoPickupState.TransferStagedToCounter();
    }
}

// HandleJanitorial is the complete overnight settlement entry point. Some
// game builds invoke the exchange buffer through an internal path where the
// TransferBuffer patch is not reached; this postfix is deliberately placed at
// the outer boundary, after all native reward insertion has finished.
[HarmonyPatch(typeof(PlayerStore), "HandleJanitorial")]
internal static class ExchangeAutoPickupAfterJanitorial
{
    public static void Postfix()
    {
        ExchangeAutoPickupState.MoveFromTrashcan();
    }
}

}
