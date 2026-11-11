using System;
using XicesBetterExperience;

namespace XicesBetterExperience
{
    public static class ModEntry { public static bool IsEnabled = true; public const string ModId = "test"; }
    public static class ExperienceSettings { public static bool SingleCellValue = true; }
}
public class GameItemElement
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public string GetTooltipBasic()
    {
        var builder = new RichTextBuilder();
        builder.AddLine("Unrelated line");
        builder.AddLine(String.Format("Estimate {0}, base {1}", Estimate, Base));
        if (ThrowDuringTooltip) throw new InvalidOperationException("native tooltip failure");
        return builder.Last;
    }
    public bool ThrowDuringTooltip;
    public long Estimate = 19, Base = 10;
    public int Cells = 6, ValueReads, ShapeReads;
    public long GetCurrentValue(bool a, bool b, bool c, bool d, long e) { ValueReads++; return Estimate; }
    public long GetBaseValue() { return Base; }
    public int GetModifiedItemSize() { ShapeReads++; return Cells; }
}
public class RichTextBuilder
{
    public string Last;
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void AddLine(string text) { Last = text; }
}
public static class LocHelper
{
    public static int Reads;
    public static string GetLocalizedMechanic(string key, object[] args)
    { Reads++; return String.Format("Estimate {0}, base {1}", args); }
}
public static class ModHelper
{
    public static string GetLocalized(string id, string key) { return ", per cell {0}"; }
}
public static class SingleCellValueRegression
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Line(string value) { SingleCellValueTooltip.EstimateLinePatch.Prefix(ref value); return value; }
    public static void Main()
    {
        // Exercise the real game's Harmony version, including reserved lifecycle names.
        var harmony = new HarmonyLib.Harmony("xice.tests.tooltip");
        harmony.PatchAll(typeof(SingleCellValueTooltip).Assembly);
        var smoke = new GameItemElement();
        Check(smoke.GetTooltipBasic() == "Estimate 19, per cell 3, base 10", "Real Harmony installs and executes tooltip patches");
        smoke.ThrowDuringTooltip = true;
        bool threw = false;
        try { smoke.GetTooltipBasic(); } catch (InvalidOperationException) { threw = true; }
        Check(threw && Line("Estimate 19, base 10") == "Estimate 19, base 10", "Real Harmony finalizer restores context on exception");
        harmony.UnpatchAll("xice.tests.tooltip");
        LocHelper.Reads = 0;
        var item = new GameItemElement();
        SingleCellValueTooltip.Context previous;
        SingleCellValueTooltip.Prefix(item, out previous);
        for (int i = 0; i < 100; i++) Check(Line("Unrelated line") == "Unrelated line", "Other lines unchanged");
        Check(item.ValueReads == 1 && item.ShapeReads == 1 && LocHelper.Reads == 1, "Prepare matching information once per tooltip");
        Check(Line("Estimate 19, base 10") == "Estimate 19, per cell 3, base 10", "Sparse six-cell pipe uses full estimate with one separator");
        Check(Line("Estimate 19, base 10") == "Estimate 19, base 10", "Stop after first estimate replacement");
        SingleCellValueTooltip.Finalizer(null, previous);
        item.Estimate = 340;
        SingleCellValueTooltip.Prefix(item, out previous);
        Check(Line("Estimate 340, base 10") == "Estimate 340, per cell 56, base 10", "New tooltip observes current price and floors division");
        SingleCellValueTooltip.Finalizer(null, previous);

        SingleCellValueTooltip.Prefix(item, out previous);
        var nested = new GameItemElement { Estimate = 226, Cells = 9 };
        SingleCellValueTooltip.Context outer;
        SingleCellValueTooltip.Prefix(nested, out outer);
        Check(Line("Estimate 226, base 10") == "Estimate 226, per cell 25, base 10", "Nested tooltip has independent context");
        var error = new Exception("native failure");
        Check(SingleCellValueTooltip.Finalizer(error, outer) == error, "Preserve native exceptions");
        Check(Line("Estimate 340, base 10") == "Estimate 340, per cell 56, base 10", "Exception restores enclosing tooltip context");
        SingleCellValueTooltip.Finalizer(null, previous);
        Check(Line("Estimate 340, base 10") == "Estimate 340, base 10", "Context never leaks beyond tooltip");
        ExperienceSettings.SingleCellValue = false;
        int reads = item.ValueReads;
        SingleCellValueTooltip.Prefix(item, out previous);
        Line("Estimate 340, base 10");
        SingleCellValueTooltip.Finalizer(null, previous);
        Check(item.ValueReads == reads, "Disabled feature performs no valuation");
        Console.WriteLine("PASS: tooltip preparation once, sparse occupancy, rounding, separators, fresh prices, nesting, exception cleanup and disabled gate.");
    }
}
