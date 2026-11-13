using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XicesBetterExperience
{
    internal static class NightServiceInformation
    {
        internal static string Text(string key) { return ModHelper.GetLocalized(ModEntry.ModId, key); }
        private static string Color(string text, string color) { return "<color=#" + color + ">" + text + "</color>"; }

        internal static void RefreshOpen()
        {
            var ui = EndOfDayUIManager.Instance;
            if (ui != null && ui.IsOpen) ui.RefreshUI();
        }

        internal static void Refresh(EndOfDayUIManager ui)
        {
            if (ui.UI == null || !PlayerStore.IsInstanceExist()) return;
            var frame = ui.UI.GetComponent<EndOfDayTabletFrame>();
            if (frame == null) return;
            bool enabled = ModEntry.IsEnabled && ExperienceSettings.NightServiceInformation;
            Snapshot snapshot = enabled ? new Snapshot() : null;
            foreach (var row in frame.content.GetComponentsInChildren<NightServiceInfoRow>(true))
                row.Apply(enabled ? snapshot.Description(row.Service) : "");
        }

        // One read-only snapshot per window refresh, including virtual trade allocation.
        internal sealed class Snapshot
        {
            private readonly PlayerStore player = PlayerStore.Instance;
            private readonly List<GameItem> owned;
            private readonly List<GameItem> trash;
            private readonly HashSet<GameItem> traded = new HashSet<GameItem>();
            private readonly HashSet<GameItem> cleaned = new HashSet<GameItem>();
            private readonly GameItem alarm;
            private readonly bool alarmPowered;

            internal Snapshot()
            {
                owned = player.FindAllItem();
                trash = EmporiumEntry.Instance == null || EmporiumEntry.Instance.trashcan == null
                    ? new List<GameItem>() : GraphUtils.FindAllChildrenType<GameItem>(EmporiumEntry.Instance.trashcan);
                ForecastTrades();
                foreach (var item in trash)
                    if (!IsWithin(item, traded) && WillBeCleaned(item)) cleaned.Add(item);
                // Match MachineAlarm.FindAlarmeItem: the first non-broken owned alarm.
                alarm = owned.Find(i => i.IsID("security_alarm") && !i.IsTag("BROKEN_MACHINE_TAG"));
                // Alarms run in the early machine phase, BEFORE trade and trash cleanup.
                alarmPowered = CanRunAlarm(alarm);
            }

            internal string Description(StoreService service)
            {
                if (service.id == "INSURANCE_SERVICE")
                {
                    int rate = NightServiceRules.AlarmRate(alarm == null ? 0 : MachineAlarm.GetAlarmeStopRate(alarm), alarmPowered);
                    return "（" + Text("night_service_alarm") + Color(rate + "%", NightServiceRules.AlarmColor(rate)) + "）";
                }
                if (service.id == "JANITORIAL_SERVICE")
                {
                    foreach (var item in trash)
                    {
                        if (!cleaned.Contains(item) || IsReceiptOrNewspaper(item)) continue;
                        if (item.GetCurrentValue(false, false, true, false, 0L) > 0)
                            return "（" + Color(Text("night_service_trash_warning"), NightServiceRules.Red) + "）";
                    }
                }
                if (service.id == "WILD_FIXER" && owned.Exists(i => i.IsID("intel_sec")))
                    return FixerDescription();
                return "";
            }

            private static bool IsReceiptOrNewspaper(GameItem item)
            {
                return item.IsID("newspaper") || item.IsID("slip") || item.IsID("sec_slip") || item.IsID("procurement_slip");
            }

            private static bool IsWithin(GameItem item, HashSet<GameItem> items)
            {
                for (var current = item; current != null; current = current.parentInventory == null ? null : current.parentInventory.GetParentItem())
                    if (items.Contains(current)) return true;
                return false;
            }

            private static bool KeptInTrash(GameItem item)
            {
                for (var current = item; current != null; current = current.parentInventory == null ? null : current.parentInventory.GetParentItem())
                    if (current.IsID("smuggler_bay_mod")) return true;
                return false;
            }

            private static bool WillBeCleaned(GameItem item)
            {
                if (KeptInTrash(item)) return false;
                // A protected hazardous item can still be destroyed with an unprotected container.
                for (var current = item; current != null; current = current.parentInventory == null ? null : current.parentInventory.GetParentItem())
                    if (!GeneralHelper.IsHazardousWaste(current)) return true;
                return false;
            }

            private void ForecastTrades()
            {
                var exchange = player.exchangeManager;
                if (exchange == null || !exchange.isExchangeUnlocked || StoreReputation.IsPerkUnlocked("BM_DISTRUSTED")) return;
                var offers = new List<ExchangeBarter>(exchange.activeBarter);
                foreach (var offer in exchange.promotionalBarter)
                    if (exchange.activePromotionalBarterIds.Contains(offer.identifier)) offers.Add(offer);
                offers.Sort((a, b) => b.priority.CompareTo(a.priority));
                foreach (var offer in offers)
                {
                    if (offer.barterCounterPerNight <= 0 || offer.conditions.Count == 0) continue;
                    var selected = new HashSet<GameItem>();
                    bool complete = true;
                    foreach (var condition in offer.conditions)
                    {
                        var match = trash.Find(i => !IsWithin(i, traded) && !selected.Contains(i) && condition.IsItemValid(i));
                        if (match == null) { complete = false; break; }
                        selected.Add(match);
                    }
                    if (complete) traded.UnionWith(selected);
                }
            }

            private bool CanRunAlarm(GameItem item)
            {
                if (item == null) return false;
                var slot = MachineHelper.GetBatterySlot(item);
                var source = slot == null ? null : slot.childItem;
                if (source == null) return false;
                if (CurrentConverterHelper.IsCurrentConverter(source))
                    return CurrentConverterHelper.IsOn(source) && !CurrentConverterHelper.IsBlackedOut() &&
                        (StoreService.GetCurrentConverterService()?.IsActive() ?? false);
                // Read-only check: do not consume a battery or change isNightCycle.
                return PowerHelper.CanDrawPowerSource(source, MachineryHelper.GetMachinePowerUsage(item));
            }

            private string FixerDescription()
            {
                var sec = player.secData;
                int tier = NetworkUpgrade.GetWildFixerTier();
                var crimes = new Dictionary<string, int>();
                for (int i = 0; i < sec.crimeTypes.Count; i++) crimes[sec.crimeTypes[i]] = sec.crimeAmount[i];
                var pending = PendingCrimes();
                var reputation = ForecastReputation();
                bool trusted = HasPerk("SEC_TRUSTED", reputation), priority = HasPerk("SEC_PRIORITY", reputation);
                bool watchlist = HasPerk("SEC_WATCHLIST", reputation), petition = HasPerk("UL_PETITION", reputation);
                int without = NightServiceRules.ProjectInvestigation(crimes, pending, sec.evidenceLevel, 0, trusted, priority, watchlist, petition);
                int with = NightServiceRules.ProjectInvestigation(crimes, pending, sec.evidenceLevel, tier, trusted, priority, watchlist, petition);
                string amountColor = NightServiceRules.RemainingAfterFixer(sec.crimeAmount, tier) == 0 ? NightServiceRules.Green : NightServiceRules.Orange;
                string value = TabletUI.Credits(sec.GetTotalCrimeValue());
                return "（" + Text("night_service_crime_value") + Color(value, amountColor) + "；" +
                    Text("night_service_investigation") + Color(sec.evidenceLevel.ToString(), NightServiceRules.InvestigationColor(without, with)) + "）";
            }

            private Dictionary<string, int> PendingCrimes()
            {
                var result = new Dictionary<string, int> { { "FRAUD", 0 }, { "ENDANGERMENT", 0 } };
                // These two sets of deferred sales settle in BeginDay, after the fixer.
                AddPending(result, player.vendingMislabelCount, player.vendingMislabelFraudValue, player.vendingMislabelEndangermentValue);
                AddPending(result, player.requestMislabelCount, player.requestMislabelFraudValue, player.requestMislabelEndangermentValue);
                return result;
            }

            private static void AddPending(Dictionary<string, int> pending, int count, int fraud, int endangerment)
            {
                if (count <= 0) return;
                if (fraud > 0) pending["FRAUD"] += ModifyCrime(fraud);
                if (endangerment > 0) pending["ENDANGERMENT"] += ModifyCrime(endangerment);
            }

            private static int ModifyCrime(int amount)
            {
                if (StartingPerk.IsIntimidating()) amount = (int)Math.Round(amount * 0.6699999570846558);
                if (StartingPerk.IsBlabbermouth()) amount = (int)Math.Round(amount * 1.3300000429153442);
                return amount;
            }

            private Dictionary<string, double> ForecastReputation()
            {
                var result = new Dictionary<string, double>();
                foreach (var faction in player.storeReputations) result[faction.factionId] = faction.GetReputationExact();
                // The alarm's transmitter can cross a perk threshold before the investigation updates.
                if (alarmPowered && (StoreService.GetInsuranceService()?.unlocked ?? false) &&
                    !(player.skipIntro && StoreStation.Instance.dayCounter < player.startingDay + 3))
                {
                    var targets = new HashSet<string>();
                    foreach (var module in MachineHelper.GetModuleInv(alarm).childItems)
                        if (module.IsID("alarm_module_transmitter")) targets.Add(module.GetTagReadonly("TRANSMITTER_TARGET_TAG").GetString());
                    foreach (string target in targets)
                    {
                        double amount;
                        if (!result.TryGetValue(target, out amount)) continue;
                        double gain = 1;
                        if (StartingPerk.IsBlabbermouth() && (target == "FACTION_BLACK_MARKET" || target == "FACTION_REVOLUTION")) gain *= 0.5;
                        if (StartingPerk.IsSycophant() && target == "FACTION_UPPER_LEVEL") gain *= 1.25;
                        result[target] = Math.Max(-200, Math.Min(200, amount + gain * Difficulty.Rules.reputationGainMultiplier));
                    }
                }
                var losses = new Dictionary<string, int>();
                if (player.vendingMislabelCount > 0)
                {
                    int ll = Math.Max(0, (int)GetRep(result, "FACTION_LOWER_LEVEL"));
                    int ul = Math.Max(0, (int)GetRep(result, "FACTION_UPPER_LEVEL"));
                    double share = ll + ul > 0 ? (double)ul / (ll + ul) : 0.5;
                    double low = player.vendingMislabelRepLoss * (1 - share) + player.fountainMislabelRepLossLL;
                    double high = player.vendingMislabelRepLoss * share + player.fountainMislabelRepLossUL;
                    int total = Math.Max(1, Mathf.RoundToInt((float)(low + high)));
                    int highLoss = Mathf.RoundToInt((float)(total * (low + high > 0 ? high / (low + high) : share)));
                    losses["FACTION_LOWER_LEVEL"] = total - highLoss;
                    losses["FACTION_UPPER_LEVEL"] = highLoss;
                }
                if (player.requestMislabelCount > 0)
                    foreach (var loss in player.requestMislabelRepLoss)
                    {
                        int current;
                        losses.TryGetValue(loss.Key, out current);
                        losses[loss.Key] = current + Math.Max(1, Mathf.RoundToInt((float)loss.Value));
                    }
                foreach (var loss in losses)
                {
                    double factor = loss.Key == "FACTION_BLACK_MARKET" ? 2 :
                        loss.Key == "FACTION_SECURITY" && HasPerk("UL_PETITION", result) ? 2 : 1;
                    result[loss.Key] = Math.Max(-200, Math.Min(200, GetRep(result, loss.Key) - Math.Min(20, loss.Value) * factor * Difficulty.Rules.reputationLossMultiplier));
                }
                return result;
            }

            private static double GetRep(Dictionary<string, double> values, string faction)
            {
                double value;
                return values.TryGetValue(faction, out value) ? value : 0;
            }

            private bool HasPerk(string id, Dictionary<string, double> reputation)
            {
                foreach (var faction in player.storeReputations)
                    foreach (var perk in faction.perks)
                        if (perk.perkId == id)
                        {
                            double amount = GetRep(reputation, faction.factionId);
                            return perk.isAlwaysActive || (perk.perkThreshold < 0 ? amount <= perk.perkThreshold : amount >= perk.perkThreshold);
                        }
                return false;
            }
        }
    }

    // Keep the original label and geometry on each native row, never on StoreService.
    // Thus reopening, disabling the mod, and repeated toggles cannot accumulate suffixes.
    public sealed class NightServiceInfoRow : MonoBehaviour
    {
        internal StoreService Service;
        private TextMeshProUGUI label;
        private string original, applied;
        private RectTransform visuals, separator, info;
        private Vector2 labelSize, infoPosition;
        private bool autoSize, wrap;
        private float fontSize;

        internal void Capture(StoreService service, TextMeshProUGUI name)
        {
            Service = service; label = name; original = name.text;
            labelSize = name.rectTransform.sizeDelta; autoSize = name.enableAutoSizing;
            wrap = name.enableWordWrapping; fontSize = name.fontSize;
            visuals = name.transform.parent as RectTransform;
            separator = visuals.Find("Separator") as RectTransform;
            info = visuals.Find("Info") as RectTransform;
            if (info != null) infoPosition = info.anchoredPosition;
        }

        internal void Apply(string suffix)
        {
            if (label == null || applied == suffix) return;
            applied = suffix;
            bool decorated = suffix.Length != 0;
            label.enableAutoSizing = decorated ? false : autoSize;
            label.enableWordWrapping = decorated || wrap;
            label.fontSize = decorated ? 18 : fontSize;
            // Reserve inline space for the native tooltip icon between the name and details.
            label.text = original + (decorated && info != null ? "<space=22>" : "") + suffix;
            if (info != null)
                info.anchoredPosition = decorated
                    ? new Vector2(label.rectTransform.anchoredPosition.x + label.GetPreferredValues(original, 1000, 1000).x + 4, infoPosition.y)
                    : infoPosition;
            float width = decorated ? 312 : labelSize.x;
            float height = decorated ? Mathf.Max(28, Mathf.Ceil(label.GetPreferredValues(label.text, width, 1000).y)) : labelSize.y;
            label.rectTransform.sizeDelta = new Vector2(width, height);
            float rowHeight = Mathf.Max(30, height + 2);
            var root = transform as RectTransform;
            root.sizeDelta = new Vector2(root.sizeDelta.x, rowHeight);
            visuals.sizeDelta = new Vector2(visuals.sizeDelta.x, rowHeight);
            var layout = GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = rowHeight;
            if (separator != null) separator.anchoredPosition = new Vector2(separator.anchoredPosition.x, -(rowHeight - 2));
            LayoutRebuilder.MarkLayoutForRebuild(root.parent as RectTransform);
        }
    }

    [HarmonyPatch(typeof(EndOfDayUIManager), "BuildServiceRow")]
    internal static class NightServiceInformationBuildRow
    {
        private static void Postfix(EndOfDayUIManager __instance, StoreService storeService)
        {
            if (__instance.UI == null) return;
            var frame = __instance.UI.GetComponent<EndOfDayTabletFrame>();
            if (frame == null || frame.content.childCount == 0) return;
            var row = frame.content.GetChild(frame.content.childCount - 1);
            var name = row.Find("Visuals/Name");
            if (name == null) return;
            row.gameObject.AddComponent<NightServiceInfoRow>().Capture(storeService, name.GetComponent<TextMeshProUGUI>());
        }
    }

    [HarmonyPatch(typeof(EndOfDayUIManager), "RefreshUI")]
    internal static class NightServiceInformationRefresh
    {
        private static void Postfix(EndOfDayUIManager __instance)
        {
            try { NightServiceInformation.Refresh(__instance); }
            catch (Exception error) { Debug.LogError("[XicesBetterExperience] Night service information: " + error); }
        }
    }
}
