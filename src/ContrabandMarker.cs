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

    [HarmonyPatch(typeof(GameItemElement), "ValidateSelect", new[] { typeof(bool) })]
    public static class ContrabandValidatePatch
    {
        public static void Postfix(GameItemElement __instance) { ContrabandMarker.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleBackground", new[] { typeof(bool), typeof(float), typeof(float) })]
    public static class ContrabandVisibilityPatch
    {
        public static void Postfix(GameItemElement __instance) { ContrabandMarker.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", new[] { typeof(bool), typeof(Vector4) })]
    public static class ContrabandDragPatch
    {
        public static void Postfix(GameItemElement __instance) { ContrabandMarker.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "UpdateRectTransform", new Type[] { })]
    public static class ContrabandRotationPatch
    {
        public static void Postfix(GameItemElement __instance) { ContrabandMarker.Refresh(__instance); }
    }

    [HarmonyPatch(typeof(GameItemElement), "Destroy", new Type[] { })]
    public static class ContrabandDestroyPatch
    {
        public static void Prefix(GameItemElement __instance) { ContrabandMarker.Remove(__instance); }
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
