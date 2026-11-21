using System;
using System.Collections.Generic;
using HarmonyLib;
using XicesBetterExperience;

namespace UnityEngine { public static class Debug { public static void LogError(object value) { } } }
namespace XicesBetterExperience
{
    public static class ModEntry { public const string ModId = "test"; public static bool IsEnabled = true; }
    public static class ExperienceSettings { public static bool AutoRenew = true; }
}
public class GameItem { }
public class Dialogue
{
    public Dialogue nextDialogue;
    public List<(GameItem, Dialogue, Action)> choices = new List<(GameItem, Dialogue, Action)>();
    public bool IsChoiceEnabledAt(int index) { return index >= 0 && index < choices.Count && !PhoneDialogList.DisabledChoice; }
}
public static class ModHelper
{
    public static bool GetModDataBool(string mod, string key, bool fallback) { string s; return PlayerStore.Instance.data.TryGetValue(key, out s) ? s == "true" : fallback; }
    public static void SetModDataBool(string mod, string key, bool value) { PlayerStore.Instance.data[key] = value ? "true" : "false"; }
    public static int GetModDataInt(string mod, string key, int fallback) { string s; return PlayerStore.Instance.data.TryGetValue(key, out s) ? int.Parse(s) : fallback; }
    public static void SetModDataInt(string mod, string key, int value) { PlayerStore.Instance.data[key] = value.ToString(); }
    public static string GetLocalized(string mod, string key) { return key; }
}
public class PlayerStore
{
    public static PlayerStore Instance;
    public static bool IsInstanceExist() { return Instance != null; }
    public Dictionary<long, StorePhoneClient> PhoneClientDict { get { return clients; } }
    public Dictionary<string, string> data = new Dictionary<string, string>();
    public Dictionary<long, StorePhoneClient> clients = new Dictionary<long, StorePhoneClient>();
    public List<string> nightLogs = new List<string>();
    public List<string> futurStoreClientIdQueue = new List<string>();
    public List<int> delays = new List<int>();
    public bool Morning;
    public void AddNightLog(string message, string color) { nightLogs.Add(message); }
    public void QueueFuturClient(string id, int delay) { futurStoreClientIdQueue.Add(id); delays.Add(delay + (Morning ? 1 : 0)); }
    public void DismissCurrentClient() { PhoneUIManager.InStore = false; }
}
public class StoreReputation
{
    public static bool Block;
    public static bool IsPerkUnlocked(string id) { return Block; }
    public static void UpdateReputation() { }
}
public class StoreClient { public Action onSale; public void OnItemSold() { onSale?.Invoke(); } }
public class PhoneUIManager
{
    public static PhoneUIManager Instance = new PhoneUIManager();
    public static bool InStore;
    public static bool WillAnswerCall(long number) { return !InStore && !StoreReputation.Block; }
}
public class StorePhoneClient
{
    public enum PhoneState { None, NotResponding, Introduction, Regular, Cooldown }
    public PhoneState phoneState = PhoneState.Regular;
    public int amountSpent, stockType, currentCooldown, cooldownDuration;
    public long number;
    public string GetLocalizedDisplayName() { return number.ToString(); }
    public static StorePhoneClient GetPhoneClientByNumber(long number) { StorePhoneClient c; return PlayerStore.Instance.clients.TryGetValue(number, out c) ? c : null; }
    public void UseService() { phoneState = PhoneState.Cooldown; currentCooldown = cooldownDuration; }
    public Dialogue GetCallDialog()
    {
        switch (number) { case 8815: return PhoneDialogList.RedImpRepRegular(); case 56371: return PhoneDialogList.MiningRegular(); case 4615: return PhoneDialogList.PharmaRegular(); default: return PhoneDialogList.ChemistRegular(); }
    }
    public void OnNewDay()
    {
        if (phoneState == PhoneState.Cooldown && currentCooldown > 0) currentCooldown--;
        if (phoneState == PhoneState.Cooldown && currentCooldown <= 0)
        {
            PlayerStore.Instance.nightLogs.Add("ready:" + number);
            phoneState = PhoneState.Regular;
        }
    }
}
public static class PhoneDialogList
{
    public static bool DisabledChoice, ThrowOrder;
    public static int threshold(long n) { return n == 56371 ? 350 : n == 4615 ? 500 : 400; }
    private static Dialogue Make(long number)
    {
        StorePhoneClient c = StorePhoneClient.GetPhoneClientByNumber(number);
        Dialogue orders = new Dialogue();
        int count = c.amountSpent >= threshold(number) ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            int type = i;
            orders.choices.Add((new GameItem(), new Dialogue(), delegate
            {
                if (ThrowOrder) throw new InvalidOperationException("native failure");
                c.stockType = type; c.UseService(); PlayerStore.Instance.QueueFuturClient(number.ToString(), type + 1);
            }));
        }
        orders.choices.Add((new GameItem(), new Dialogue(), () => { }));
        // Exercise the pharma two-line greeting and the single-line greetings.
        return new Dialogue { nextDialogue = number == 4615 ? new Dialogue { nextDialogue = orders } : orders };
    }
    public static Dialogue RedImpRepRegular() { return Make(8815); }
    public static Dialogue MiningRegular() { return Make(56371); }
    public static Dialogue PharmaRegular() { return Make(4615); }
    public static Dialogue ChemistRegular() { return Make(3319); }
}
public static class AutoRenewRegression
{
    static int checks;
    static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    static StorePhoneClient Reset(long number, bool large)
    {
        PlayerStore.Instance = new PlayerStore();
        StoreReputation.Block = false; PhoneUIManager.Instance = new PhoneUIManager(); PhoneUIManager.InStore = false;
        PhoneDialogList.DisabledChoice = false; PhoneDialogList.ThrowOrder = false;
        ModEntry.IsEnabled = true; ExperienceSettings.AutoRenew = true;
        var c = new StorePhoneClient { number = number, amountSpent = large ? PhoneDialogList.threshold(number) : 0,
            cooldownDuration = number == 8815 || number == 3319 ? 4 : number == 56371 ? 5 : 7 };
        PlayerStore.Instance.clients[number] = c;
        return c;
    }
    static void Ready(StorePhoneClient c) { c.phoneState = StorePhoneClient.PhoneState.Regular; c.currentCooldown = 0; }
    public static void Main()
    {
        new Harmony("test.autorenew").PatchAll(typeof(AutoRenew).Assembly);
        foreach (long number in AutoRenew.Numbers)
        {
            var c = Reset(number, false);
            AutoRenew.Resume(); Check(PlayerStore.Instance.futurStoreClientIdQueue.Count == 0, "Default config must not opt suppliers in");
            AutoRenew.Toggle(number);
            Check(AutoRenew.Active(number) && c.stockType == 0 && c.currentCooldown == c.cooldownDuration, "Ordinary fallback and native cooldown");
            for (int i = 0; i < 8; i++) AutoRenew.Resume();
            Check(PlayerStore.Instance.futurStoreClientIdQueue.Count == 1, "No duplicates on refresh/load");
            c.currentCooldown = 1; c.OnNewDay();
            Check(PlayerStore.Instance.futurStoreClientIdQueue.Count == 2 && c.currentCooldown == c.cooldownDuration, "Exactly one reorder on nightly readiness");
            Check(PlayerStore.Instance.nightLogs.Count == 1 && PlayerStore.Instance.nightLogs[0] == "ready:" + number + " auto_renew_done", "Report suffix follows the original readiness message");
            AutoRenew.Toggle(number); Ready(c); AutoRenew.Resume();
            Check(!AutoRenew.Active(number) && PlayerStore.Instance.futurStoreClientIdQueue.Count == 2, "Stop preserves existing orders");

            c = Reset(number, true); AutoRenew.Toggle(number);
            Check(c.stockType == 1 && PlayerStore.Instance.delays[0] == 2, "Unrecorded choice defaults to available large order");
            AutoRenew.Toggle(number); Ready(c);
            var choices = AutoRenew.Orders(c.GetCallDialog()); choices.choices[0].Item3();
            Ready(c); AutoRenew.Toggle(number);
            Check(c.stockType == 0, "Manual ordinary preference wins over large default");
            c.currentCooldown = 1; c.OnNewDay(); Check(c.stockType == 0, "Automatic orders preserve manual preference");
            AutoRenew.Toggle(number); Ready(c); AutoRenew.Orders(c.GetCallDialog()).choices[1].Item3();
            Ready(c); AutoRenew.Toggle(number); Check(c.stockType == 1, "New manual large preference recorded");

            c = Reset(number, false); AutoRenew.Toggle(number);
            new StoreClient { onSale = () => c.amountSpent = PhoneDialogList.threshold(number) }.OnItemSold();
            Check(!AutoRenew.Active(number) && PlayerStore.Instance.nightLogs[0].Contains("auto_renew_unlocked"), "New large unlock stops immediately after purchase");
            Ready(c); AutoRenew.Toggle(number); c.currentCooldown = 1; c.OnNewDay();
            Check(AutoRenew.Active(number) && c.stockType == 1, "Re-enabling after unlock remains enabled");
            StoreReputation.Block = true; StoreReputation.UpdateReputation();
            Check(!AutoRenew.Active(number), "Reputation blacklist cancels");
            StoreReputation.Block = false; StoreReputation.UpdateReputation(); Ready(c); AutoRenew.Resume();
            Check(!AutoRenew.Active(number) && c.phoneState == StorePhoneClient.PhoneState.Regular, "Recovery never re-enables canceled automation");

            c = Reset(number, false); c.UseService(); AutoRenew.Toggle(number);
            Check(AutoRenew.Active(number) && PlayerStore.Instance.futurStoreClientIdQueue.Count == 0, "Opt in during cooldown only waits");
            ExperienceSettings.AutoRenew = false; c.currentCooldown = 1; c.OnNewDay();
            Check(AutoRenew.Active(number) && c.phoneState == StorePhoneClient.PhoneState.Regular, "Global toggle pauses without losing opt in");
            ExperienceSettings.AutoRenew = true; AutoRenew.Resume(); Check(c.phoneState == StorePhoneClient.PhoneState.Cooldown, "Global enable resumes ready order");
            c.phoneState = StorePhoneClient.PhoneState.NotResponding; AutoRenew.ValidateAll(); Check(!AutoRenew.Active(number), "Native refusal cancels");

            c = Reset(number, true); PlayerStore.Instance.Morning = true; AutoRenew.Toggle(number);
            Check(PlayerStore.Instance.delays[0] == 3, "Native morning delivery compensation preserved");
            var data = PlayerStore.Instance.data;
            PlayerStore.Instance = new PlayerStore(); PlayerStore.Instance.clients[number] = c;
            Check(!AutoRenew.Active(number), "New save isolated");
            PlayerStore.Instance.data = data; Check(AutoRenew.Active(number), "Save data restores opt in");

            c = Reset(number, false); PhoneUIManager.InStore = true; AutoRenew.Toggle(number);
            Check(AutoRenew.Active(number) && c.phoneState == StorePhoneClient.PhoneState.Regular, "Temporarily busy waits");
            PlayerStore.Instance.DismissCurrentClient(); Check(c.phoneState == StorePhoneClient.PhoneState.Cooldown, "Supplier leaving retries ready order");
            Ready(c); PhoneDialogList.ThrowOrder = true; AutoRenew.Resume();
            Check(!AutoRenew.Active(number) && PlayerStore.Instance.futurStoreClientIdQueue.Count == 1, "Native failure stops without duplicate");
        }
        var last = Reset(8815, false); ModEntry.IsEnabled = false; AutoRenew.Toggle(8815);
        Check(!AutoRenew.Active(8815), "Disabled mod cannot opt in");
        ModEntry.IsEnabled = true; PhoneDialogList.DisabledChoice = true; AutoRenew.Toggle(8815);
        Check(PlayerStore.Instance.futurStoreClientIdQueue.Count == 0, "Unavailable native choice is not executed");
        PhoneDialogList.DisabledChoice = false; PhoneUIManager.Instance = null; AutoRenew.Resume();
        Check(AutoRenew.Active(8815), "Missing UI initialization does not cancel saved opt in");
        PhoneUIManager.Instance = new PhoneUIManager(); AutoRenew.Resume();
        Check(last.phoneState == StorePhoneClient.PhoneState.Cooldown, "Ready order resumes after initialization");
        Check(!AutoRenew.Supported(8376) && !AutoRenew.Supported(51189), "Non-order contacts excluded");
        PlayerStore.Instance = null; AutoRenew.Resume();
        Check(!AutoRenew.Active(8815), "Main menu never creates or reads a store");
        Console.WriteLine("PASS: automatic reordering native callbacks, manual preferences, unlock/refusal cancellation, nightly logs, save isolation, toggles and duplicate prevention (" + checks + " checks).");
    }
}
