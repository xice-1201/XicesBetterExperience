using System;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    public static class StolenHeatMarker
    {
        public static bool HasHeat(GameItem item) { return item != null && StolenHelper.IsStolenItem(item) && StolenHelper.GetFoundChance(item) > 0; }
        public static void Refresh(GameItem item) { ItemStatusBackground.Refresh(item); }
        public static void Remove(GameItemElement item) { ItemStatusBackground.Remove(item); }
        public static void Clear() { ItemStatusBackground.Clear(); }
        public static void RefreshLoaded() { ItemStatusBackground.RefreshLoaded(); }
    }

    [HarmonyPatch(typeof(StolenHelper), "SetHeat", new[] { typeof(GameItem), typeof(int) })]
    public static class StolenHeatSetPatch
    {
        public static void Postfix(GameItem __0) { StolenHeatMarker.Refresh(__0); }
    }

    [HarmonyPatch(typeof(StolenHelper), "InitStolenItem", new[] { typeof(GameItem), typeof(int) })]
    public static class StolenHeatInitPatch
    {
        public static void Postfix(GameItem __0) { StolenHeatMarker.Refresh(__0); }
    }

    [HarmonyPatch(typeof(StolenHelper), "UpdateStolenItem", new[] { typeof(GameItem) })]
    public static class StolenHeatDecayPatch
    {
        public static void Postfix(GameItem __0) { StolenHeatMarker.Refresh(__0); }
    }

    [HarmonyPatch(typeof(StolenHelper), "RemoveStolenStatus", new[] { typeof(GameItem) })]
    public static class StolenHeatRemoveStatusPatch
    {
        public static void Postfix(GameItem __0) { StolenHeatMarker.Refresh(__0); }
    }
}
