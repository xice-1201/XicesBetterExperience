using System;
using HarmonyLib;

namespace XicesBetterExperience
{
    internal static class AutoRenew
    {
        internal static readonly long[] Numbers = { 8815, 56371, 4615, 3319 };
        private static bool ordering;
        private static bool HasStore { get { return PlayerStore.IsInstanceExist() && PlayerStore.Instance.PhoneClientDict != null; } }
        internal static bool Enabled { get { return ModEntry.IsEnabled && ExperienceSettings.AutoRenew; } }
        internal static bool Supported(long number) { return Threshold(number) != 0; }
        private static int Threshold(long number)
        {
            return number == 8815 || number == 3319 ? 400 : number == 56371 ? 350 : number == 4615 ? 500 : 0;
        }
        internal static long Number(StorePhoneClient client)
        {
            if (HasStore)
                foreach (long number in Numbers)
                    if (StorePhoneClient.GetPhoneClientByNumber(number) == client) return number;
            return 0;
        }
        private static string Key(long number, string field) { return "auto_renew_v1_" + number + "_" + field; }
        internal static bool Active(long number) { return HasStore && Supported(number) && ModHelper.GetModDataBool(ModEntry.ModId, Key(number, "active"), false); }
        private static void SetActive(long number, bool value) { ModHelper.SetModDataBool(ModEntry.ModId, Key(number, "active"), value); }
        private static bool Large(long number, StorePhoneClient client) { return client.amountSpent >= Threshold(number); }
        private static bool Blocked(StorePhoneClient client)
        {
            return client.phoneState == StorePhoneClient.PhoneState.None ||
                client.phoneState == StorePhoneClient.PhoneState.NotResponding ||
                StoreReputation.IsPerkUnlocked("LLAVOIDED");
        }
        internal static string Text(string key) { return ModHelper.GetLocalized(ModEntry.ModId, key); }
        private static void Notice(StorePhoneClient client, string key)
        {
            PlayerStore.Instance.AddNightLog(client.GetLocalizedDisplayName() + ": " + Text(key), "c98c45");
        }
        internal static void Validate(long number)
        {
            if (!ModEntry.IsEnabled || !Active(number)) return;
            StorePhoneClient client = StorePhoneClient.GetPhoneClientByNumber(number);
            if (client == null) { SetActive(number, false); return; }
            if (Blocked(client))
            {
                SetActive(number, false);
                Notice(client, "auto_renew_blocked");
            }
            else if (Large(number, client) && !ModHelper.GetModDataBool(ModEntry.ModId, Key(number, "large_seen"), false))
            {
                SetActive(number, false);
                Notice(client, "auto_renew_unlocked");
            }
        }
        internal static void ValidateAll()
        {
            if (!ModEntry.IsEnabled || !HasStore) return;
            foreach (long number in Numbers) Validate(number);
        }
        internal static void Resume()
        {
            ValidateAll();
            if (Enabled && HasStore)
                foreach (long number in Numbers) TryOrder(number);
        }
        internal static void Toggle(long number)
        {
            if (!Enabled || !Supported(number) || !HasStore) return;
            if (Active(number)) { SetActive(number, false); return; }
            StorePhoneClient client = StorePhoneClient.GetPhoneClientByNumber(number);
            if (client == null || Blocked(client) ||
                (client.phoneState != StorePhoneClient.PhoneState.Regular && client.phoneState != StorePhoneClient.PhoneState.Cooldown)) return;
            ModHelper.SetModDataBool(ModEntry.ModId, Key(number, "large_seen"), Large(number, client));
            SetActive(number, true);
            TryOrder(number);
        }
        internal static Dialogue Orders(Dialogue root)
        {
            for (int depth = 0; root != null && depth < 12; depth++, root = root.nextDialogue)
                if (root.choices.Count >= 2) return root;
            return null;
        }
        // Wrap only real order choices. The final choice is always "end call".
        internal static void TrackManual(long number, Dialogue root)
        {
            Dialogue orders = Orders(root);
            if (orders == null) return;
            for (int i = 0; i < orders.choices.Count - 1 && i < 2; i++)
            {
                int index = i;
                var choice = orders.choices[i];
                Action original = choice.Item3;
                choice.Item3 = delegate
                {
                    original?.Invoke();
                    StorePhoneClient client = StorePhoneClient.GetPhoneClientByNumber(number);
                    if (!ordering && ModEntry.IsEnabled && client != null && client.phoneState == StorePhoneClient.PhoneState.Cooldown)
                        ModHelper.SetModDataInt(ModEntry.ModId, Key(number, "choice"), index);
                };
                orders.choices[i] = choice;
            }
        }
        internal static bool TryOrder(long number)
        {
            if (!Enabled || ordering || !Active(number)) return false;
            Validate(number);
            if (!Active(number)) return false;
            StorePhoneClient client = StorePhoneClient.GetPhoneClientByNumber(number);
            if (client == null || PhoneUIManager.Instance == null || client.phoneState != StorePhoneClient.PhoneState.Regular || !PhoneUIManager.WillAnswerCall(number)) return false;
            ordering = true;
            try
            {
                Dialogue orders = Orders(client.GetCallDialog());
                if (orders == null) return false;
                int index = ModHelper.GetModDataInt(ModEntry.ModId, Key(number, "choice"), 1);
                if (index != 1 || !Large(number, client) || orders.choices.Count < 3) index = 0;
                if (!orders.IsChoiceEnabledAt(index) || orders.choices[index].Item3 == null) return false;
                int before = PlayerStore.Instance.futurStoreClientIdQueue.Count;
                orders.choices[index].Item3();
                return client.phoneState == StorePhoneClient.PhoneState.Cooldown && PlayerStore.Instance.futurStoreClientIdQueue.Count == before + 1;
            }
            catch (Exception error)
            {
                SetActive(number, false);
                UnityEngine.Debug.LogError("[XicesBetterExperience] Automatic reorder stopped: " + error);
                return false;
            }
            finally { ordering = false; }
        }

        [HarmonyPatch(typeof(PhoneDialogList), "RedImpRepRegular")]
        internal static class BeerChoices { static void Postfix(Dialogue __result) { TrackManual(8815, __result); } }
        [HarmonyPatch(typeof(PhoneDialogList), "MiningRegular")]
        internal static class MiningChoices { static void Postfix(Dialogue __result) { TrackManual(56371, __result); } }
        [HarmonyPatch(typeof(PhoneDialogList), "PharmaRegular")]
        internal static class PharmaChoices { static void Postfix(Dialogue __result) { TrackManual(4615, __result); } }
        [HarmonyPatch(typeof(PhoneDialogList), "ChemistRegular")]
        internal static class ChemChoices { static void Postfix(Dialogue __result) { TrackManual(3319, __result); } }

        [HarmonyPatch(typeof(StorePhoneClient), "OnNewDay")]
        internal static class NewDay
        {
            internal static void Prefix(out int __state) { __state = PlayerStore.Instance.nightLogs.Count; }
            internal static void Postfix(StorePhoneClient __instance, int __state)
            {
                long number = Number(__instance);
                if (!Supported(number)) return;
                Validate(number);
                if (TryOrder(number) && PlayerStore.Instance.nightLogs.Count > __state)
                    PlayerStore.Instance.nightLogs[__state] += " " + Text("auto_renew_done");
            }
        }
        [HarmonyPatch(typeof(StoreReputation), "UpdateReputation")]
        internal static class ReputationChanged { static void Postfix() { ValidateAll(); } }
        [HarmonyPatch(typeof(PlayerStore), "DismissCurrentClient")]
        internal static class ClientLeft { static void Postfix() { Resume(); } }
        [HarmonyPatch(typeof(StoreClient), "OnItemSold")]
        internal static class Purchase { static void Postfix() { ValidateAll(); } }
    }
}
