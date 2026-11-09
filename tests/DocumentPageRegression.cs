using System;
using XicesBetterExperience;

public static class DocumentPageRegression
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run(ModEntry entry)
    {
        entry.OnEnable();
        Check(ExperienceSettings.RetainDocumentPage, "Page setting defaults on");
        var ui = new MultiPageUIManager();
        for (int i = 0; i < 8; i++)
        {
            ui.RememberedOpen(i); Check(ui.Page == 0, "Unseen guide starts at first page");
            ui.TurnTo(i % 5 + 1); ui.RememberedClose();
        }
        for (int i = 0; i < 8; i++)
        {
            int buttons = ui.ButtonsUpdated;
            ui.RememberedOpen(i);
            Check(ui.Page == i % 5 + 1 && ui.DisplayedPage == ui.Page && ui.ButtonsUpdated == buttons + 1,
                "Per-document page restored after native initial page without losing memory; display/buttons synchronized");
        }
        ui.RememberedOpen(6); ui.TurnTo(5);
        ui.RememberedOpen(4); ui.TurnTo(3);
        ui.RememberedOpen(6); Check(ui.Page == 5, "Direct switching preserves separate pages");
        ui.RememberedClose(); ui.RememberedClose(); ui.RememberedOpen(6);
        Check(ui.Page == 5, "Repeated close does not erase page");
        var newScene = new MultiPageUIManager(); newScene.RememberedOpen(6);
        Check(newScene.Page == 5, "Scene recreation retains session memory");
        newScene.books[6].transform.childCount = 3; newScene.RememberedOpen(6);
        Check(newScene.Page == 2 && newScene.DisplayedPage == 2, "Clamp reduced page count");
        ModConfig.Instance.PageEntry.Set(false); newScene.RememberedOpen(6);
        Check(newScene.Page == 0, "Disabled setting restores native first-page behavior");
        newScene.TurnTo(1); newScene.RememberedClose();
        ModConfig.Instance.PageEntry.Set(true); newScene.RememberedOpen(6);
        Check(newScene.Page == 2, "Disabled setting pauses recording");
        entry.OnDisable(); newScene.RememberedOpen(6);
        Check(newScene.Page == 0, "Disabled mod restores native behavior");
        entry.OnEnable(); newScene.RememberedOpen(6);
        Check(newScene.Page == 2, "Enable restores retained memory");
        int page = 0; newScene.books[6].transform.childCount = 0;
        DocumentPageMemory.Restore(newScene, 6, 2, ref page);
        Check(page == 0, "Empty document safely ignored");
        DocumentPageMemory.Restore(null, 6, 2, ref page);
        DocumentPageMemory.Restore(newScene, 99, 2, ref page);
        Check(page == 0, "Missing UI and invalid book ignored");
        Console.WriteLine("PASS: all eight guides, page/display/button restoration, close/reopen, switching, scene recreation, page-count bounds and independent setting/mod gates.");
    }
}
