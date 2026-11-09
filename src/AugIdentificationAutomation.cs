using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using EPOOutline;

namespace XicesBetterExperience
{
    public static class AugIdentificationAutomation
    {
        private static StoreClient tracked;
        private static bool identified, approached, stood;
        private static readonly HashSet<string> spoken = new HashSet<string>();
        private static readonly HashSet<string> suspiciousLines = new HashSet<string>();
        private sealed class OriginalOutfit { internal string Sprite; }
        private static readonly ConditionalWeakTable<StoreClient, OriginalOutfit> originalOutfits = new ConditionalWeakTable<StoreClient, OriginalOutfit>();
        private static SpriteDict catalogOwner;
        private static readonly HashSet<string> augSprites = new HashSet<string>();
        private static readonly Dictionary<string, string> outfitGroups = new Dictionary<string, string>();
        private static Outlinable outline;
        private static bool nativeEnabled;
        private static Color nativeColor;

        private static StoreClient Current
        { get { return PlayerStore.IsInstanceExist() && PlayerStore.Instance.currentClientInstance != null ? PlayerStore.Instance.currentClientInstance.GetClientBlueprint() : null; } }
        private static bool Eligible(StoreClient client)
        {
            return PlayerStore.IsInstanceExist() && client != null &&
                AugIdentificationRules.Active(ModEntry.IsEnabled && ExperienceSettings.AugIdentification,
                    PlayerStore.Instance.isAugIntroduced, PlayerStore.Instance.isAugEnded, client.isAug) &&
                StoreUIManager.Instance != null && !StoreUIManager.Instance.isShot;
        }

        private static void RestoreOutline()
        {
            if (outline != null)
            {
                outline.enabled = nativeEnabled;
                outline.OutlineParameters.Color = nativeColor;
            }
            outline = null;
        }
        public static void Clear()
        {
            RestoreOutline(); tracked = null; identified = approached = stood = false; spoken.Clear();
            lastIdleClient = null; lastFrozen = lastShaking = false;
        }
        private static void Sync()
        {
            StoreClient client = Current;
            if (client != tracked) { Clear(); tracked = client; }
            if (!Eligible(client)) { identified = false; RestoreOutline(); }
        }
        private static void Paint()
        {
            if (!identified || !Eligible(Current)) { RestoreOutline(); return; }
            GameObject gun = StoreUIManager.Instance.shotgun;
            if (gun == null) return;
            Outlinable target = gun.GetComponent<Outlinable>();
            if (target == null) return;
            if (outline != target)
            {
                RestoreOutline(); outline = target;
                nativeEnabled = target.enabled; nativeColor = target.OutlineParameters.Color;
            }
            outline.enabled = true;
            outline.OutlineParameters.Color = new Color(1f, 0.15f, 0.1f, 0.65f);
        }
        private static void Identify() { identified = true; Paint(); }

        private static void BuildCatalog()
        {
            if (catalogOwner == SpriteDict.Instance) return;
            catalogOwner = SpriteDict.Instance; augSprites.Clear(); outfitGroups.Clear();
            if (catalogOwner == null) return;
            foreach (string prefix in new[] { "augLLMale", "augLLFemale", "augBMMale", "augBMFemale", "augULMale", "augULFemale" })
                foreach (string key in SpriteDict.GetPool(prefix))
                    if (catalogOwner.spriteDictionary.ContainsKey(key)) augSprites.Add(key);
            foreach (string group in new[] { "Upper", "Lower", "BM", "Scav", "Farmer", "Chef", "Med", "Eng", "Merc", "Sci" })
                foreach (string sex in new[] { "male", "female" })
                    foreach (string key in SpriteDict.GetPool(sex + group))
                        if (catalogOwner.spriteDictionary.ContainsKey(key)) outfitGroups[key] = group;
        }
        private static bool VisualTell(StoreClient client)
        {
            BuildCatalog();
            string sprite = client.spriteName ?? "";
            if (augSprites.Contains(sprite)) return true;
            string actual;
            if (!outfitGroups.TryGetValue(sprite, out actual)) return false;
            OriginalOutfit original;
            if (originalOutfits.TryGetValue(client, out original))
            {
                string expectedGroup;
                return outfitGroups.TryGetValue(original.Sprite ?? "", out expectedGroup) && actual != expectedGroup;
            }
            // Event customers can legitimately wear a professional outfit within a core faction.
            // Only the dedicated Aug buyers use the advertised faction as their outfit rule.
            if (client.identifier != "aug_med" && client.identifier != "aug_material") return false;
            string faction = client.clientFaction;
            // Native dedicated Aug buyers advertise their faction through their display name.
            if (String.IsNullOrEmpty(faction) || faction == "NONE")
            {
                if (client.displayName == LocHelper.GetLocalizedClientName("name_upper_level_citizen")) faction = "FACTION_UPPER_LEVEL";
                else if (client.displayName == LocHelper.GetLocalizedClientName("name_lower_levels_citizen")) faction = "FACTION_LOWER_LEVEL";
                else if (client.displayName == LocHelper.GetLocalizedClientName("name_thief")) faction = "FACTION_BLACK_MARKET";
            }
            string expected = faction == "FACTION_UPPER_LEVEL" ? "Upper" : faction == "FACTION_LOWER_LEVEL" ? "Lower" : faction == "FACTION_BLACK_MARKET" ? "BM" : null;
            return expected != null && actual != expected;
        }
        private static bool Suspicious(string text)
        {
            foreach (string line in suspiciousLines)
                if (text == AugIdentificationRules.Normalize(LocGenderHelper.ResolveForCurrentClient(line))) return true;
            return false;
        }
        public static void ConfigApplied()
        {
            Sync();
            if (!Eligible(tracked)) return;
            if ((approached && VisualTell(tracked)) || (stood && HasIdleTell()) ) Identify();
            foreach (string text in spoken) if (Suspicious(text)) { Identify(); break; }
            Paint();
        }
        private static bool HasIdleTell()
        {
            StoreClientMono mono = StoreClientMono.Instance;
            return mono != null && lastIdleClient == tracked && (lastFrozen || lastShaking);
        }
        private static StoreClient lastIdleClient;
        private static bool lastFrozen, lastShaking;
        private static void Safe(Action action)
        {
            try { action(); }
            catch (Exception error) { Clear(); Debug.LogError("[XicesBetterExperience] Aug identification: " + error); }
        }

        [HarmonyPatch(typeof(AugHelper), "MakeDisguisedAug", new[] { typeof(StoreClient), typeof(int) })]
        public static class DisguisePatch
        {
            public static void Prefix(StoreClient storeClient, out string __state) { __state = storeClient == null ? null : storeClient.spriteName; }
            public static void Postfix(StoreClient storeClient, string __state)
            {
                if (storeClient == null) return;
                originalOutfits.Remove(storeClient);
                originalOutfits.Add(storeClient, new OriginalOutfit { Sprite = __state });
            }
        }
        [HarmonyPatch(typeof(StoreClientMono), "StartApprochingStore", new Type[] { })]
        public static class ApproachPatch
        {
            public static void Postfix() { Safe(() => { Clear(); Sync(); approached = true; if (Eligible(tracked) && VisualTell(tracked)) Identify(); }); }
        }
        [HarmonyPatch(typeof(StoreClientMono), "PlayIdleForCurrentClient", new Type[] { })]
        public static class IdlePatch
        {
            public static void Postfix(StoreClient ___idleDecisionClient, bool ___idleSuppressed, bool ___idleShakeHorizontal)
            {
                Safe(() => { Sync(); lastIdleClient = ___idleDecisionClient; lastFrozen = ___idleSuppressed; lastShaking = ___idleShakeHorizontal;
                    stood = true; if (Eligible(tracked) && HasIdleTell()) Identify(); });
            }
        }
        [HarmonyPatch(typeof(LocHelper), "GetLocalizedDialogue", new[] { typeof(string), typeof(object[]) })]
        public static class LocalizationPatch
        {
            public static void Postfix(string key, string __result)
            { if (AugIdentificationRules.IsSuspiciousKey(key) && !String.IsNullOrEmpty(__result) && __result != key) suspiciousLines.Add(__result); }
        }
        [HarmonyPatch(typeof(DialogUIManager), "DisplayClientText", new[] { typeof(Dialogue) })]
        public static class SpeechPatch
        {
            public static void Postfix(Dialogue dialogue)
            {
                Safe(() => { Sync(); if (tracked == null || dialogue == null) return;
                    string text = AugIdentificationRules.Normalize(dialogue.plainText); spoken.Add(text);
                    if (Eligible(tracked) && Suspicious(text)) Identify(); });
            }
        }
        [HarmonyPatch(typeof(PlayerStore), "DismissCurrentClient", new[] { typeof(bool), typeof(bool), typeof(bool) })]
        public static class LeavePatch { public static void Prefix() { Clear(); } }
        [HarmonyPatch(typeof(StoreUIManager), "Shoot", new Type[] { })]
        public static class ShootPatch { public static void Prefix() { Clear(); } }
        [HarmonyPatch(typeof(Shotgun), "LoadShotgun", new Type[] { })]
        public static class GunPatch { public static void Postfix() { Safe(() => { Sync(); Paint(); }); } }
        [HarmonyPatch(typeof(StoreClientManager), "HandleAugClient", new[] { typeof(bool) })]
        public static class CrisisPatch { public static void Postfix() { Safe(() => { Sync(); Paint(); }); } }
        [HarmonyPatch(typeof(EscapeUIManager), "MainMenuWithAnalytics", new Type[] { })]
        public static class MenuPatch { public static void Prefix() { Clear(); } }

        // Keep the native hover/selection state underneath the temporary red outline.
        [HarmonyPatch(typeof(Interactable), "OnPointerEnter", new[] { typeof(UnityEngine.EventSystems.PointerEventData) })]
        public static class HoverPatch
        {
            public static void Prefix(Interactable __instance)
            { if (outline != null && __instance != null && __instance.gameObject == outline.gameObject) RestoreOutline(); }
            public static void Postfix(Interactable __instance)
            { Safe(() => { if (StoreUIManager.Instance != null && __instance != null && __instance.gameObject == StoreUIManager.Instance.shotgun) { Sync(); Paint(); } }); }
        }
        [HarmonyPatch(typeof(Interactable), "OnPointerExit", new[] { typeof(UnityEngine.EventSystems.PointerEventData) })]
        public static class HoverExitPatch
        {
            public static void Prefix(Interactable __instance) { HoverPatch.Prefix(__instance); }
            public static void Postfix(Interactable __instance) { HoverPatch.Postfix(__instance); }
        }
        [HarmonyPatch(typeof(Interactable), "OnDeselected", new Type[] { })]
        public static class DeselectPatch
        {
            public static void Prefix(Interactable __instance) { HoverPatch.Prefix(__instance); }
            public static void Postfix(Interactable __instance) { HoverPatch.Postfix(__instance); }
        }
    }
}
