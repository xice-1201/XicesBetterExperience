using System;
using System.Collections.Generic;

namespace XicesBetterExperience
{
    // Pure forecasts: never run the game's settlement callbacks to preview a night.
    internal static class NightServiceRules
    {
        internal const string Red = "9c3738", Orange = "cc7755", Green = "5c6f46";

        internal static int AlarmRate(int rate, bool powered)
        {
            return powered ? Math.Max(0, Math.Min(100, rate)) : 0;
        }

        internal static string AlarmColor(int rate)
        {
            return rate <= 0 ? Red : rate >= 100 ? Green : Orange;
        }

        internal static int AfterFixer(int amount, int tier)
        {
            if (tier < 1 || tier > 3) return Math.Max(0, amount);
            int percent = tier == 1 ? 15 : tier == 2 ? 20 : 25;
            int flat = tier == 1 ? 50 : tier == 2 ? 150 : 400;
            // Match SecData.OnFixerUsed's float multiplication and truncation.
            return Math.Max(0, amount - (int)(amount * (percent / 100f)) - flat);
        }

        internal static int RemainingAfterFixer(IEnumerable<int> crimes, int tier)
        {
            int total = 0;
            foreach (int crime in crimes) total += AfterFixer(crime, tier);
            return total;
        }

        internal static int InvestigationIncrease(int total, bool trusted, bool priority, bool watchlist, bool petition)
        {
            int increase = total < 100 ? 0 : total < 300 ? 2 : total < 850 ? 6 :
                total < 1200 ? 9 : total < 1500 ? 12 : total < 2500 ? 20 : 33;
            if (trusted) increase /= 2;
            if (priority) increase = (int)Math.Round(increase * 1.5);
            else if (watchlist) increase = (int)Math.Round(increase * 1.2000000476837158);
            if (petition) increase = (int)Math.Round(increase * 1.5);
            return increase;
        }

        internal static int ProjectInvestigation(IDictionary<string, int> crimes, IDictionary<string, int> pending,
            int evidence, int tier, bool trusted, bool priority, bool watchlist, bool petition)
        {
            var next = new Dictionary<string, int>();
            foreach (var crime in crimes) next[crime.Key] = AfterFixer(crime.Value, tier);
            // Mislabelled sales settle AFTER the fixer and BEFORE SecData.OnNewDay.
            foreach (var crime in pending)
            {
                int current;
                next.TryGetValue(crime.Key, out current);
                next[crime.Key] = current + crime.Value;
            }
            int total = 0;
            foreach (int amount in next.Values) total += Math.Max(0, amount - (evidence < 80 ? 20 : 0));
            return Math.Min(100, evidence + InvestigationIncrease(total, trusted, priority, watchlist, petition));
        }

        internal static string InvestigationColor(int withoutFixer, int withFixer)
        {
            return withFixer >= 100 ? Red : withoutFixer < 100 ? Green : Orange;
        }
    }
}
