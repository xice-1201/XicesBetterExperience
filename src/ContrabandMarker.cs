using System;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    public static class ContrabandMarker
    {
        public static bool IsContraband(GameItem item) { return item != null && ContrabandHelper.IsContraband(item); }
        public static void Refresh(GameItem item) { ItemStatusBackground.Refresh(item); }
        public static void Remove(GameItemElement item) { ItemStatusBackground.Remove(item); }
        public static void Clear() { ItemStatusBackground.Clear(); }
        public static void RefreshLoaded() { ItemStatusBackground.RefreshLoaded(); }
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
