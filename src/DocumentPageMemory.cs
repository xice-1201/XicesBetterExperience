using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace XicesBetterExperience
{
    public static class DocumentPageMemory
    {
        // Native multi-page guides are shared by book index, not by physical item instance.
        private static readonly Dictionary<int, int> pages = new Dictionary<int, int>();
        private static readonly MethodInfo showPage = typeof(MultiPageUIManager).GetMethod(
            "ShowPage", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo updateButtons = typeof(MultiPageUIManager).GetMethod(
            "UpdateButtons", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static bool Active { get { return ModEntry.IsEnabled && ExperienceSettings.RetainDocumentPage; } }

        public static int GetPage(int bookIndex)
        {
            int page;
            return Active && pages.TryGetValue(bookIndex, out page) ? page : -1;
        }

        public static void Remember(int bookIndex, int page)
        {
            if (Active && bookIndex >= 0 && page >= 0) pages[bookIndex] = page;
        }

        public static void Restore(MultiPageUIManager ui, int bookIndex, int saved, ref int currentPage)
        {
            if (!Active || saved < 0 || ui == null || bookIndex < 0 || bookIndex >= ui.books.Count ||
                ui.books[bookIndex] == null || showPage == null || updateButtons == null) return;
            int count = ui.books[bookIndex].transform.childCount;
            if (count <= 0) return;
            int page = Math.Min(saved, count - 1);
            try
            {
                currentPage = page;
                showPage.Invoke(ui, new object[] { page });
                updateButtons.Invoke(ui, null);
                Remember(bookIndex, page);
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("[XicesBetterExperience] Could not restore document page: " + error);
            }
        }
    }

    [HarmonyPatch(typeof(MultiPageUIManager), "OpenUI", new[] { typeof(int) })]
    public static class DocumentPageOpenPatch
    {
        public static void Prefix(int __0, out int __state)
        {
            // Capture before native ShowPage(0) overwrites the remembered page.
            __state = DocumentPageMemory.GetPage(__0);
        }

        public static void Postfix(MultiPageUIManager __instance, int __0, int __state, ref int ___currentPage)
        {
            DocumentPageMemory.Restore(__instance, __0, __state, ref ___currentPage);
        }
    }

    [HarmonyPatch(typeof(MultiPageUIManager), "ShowPage", new[] { typeof(int) })]
    public static class DocumentPageShownPatch
    {
        public static void Postfix(int __0, int ___currentBookIndex)
        {
            DocumentPageMemory.Remember(___currentBookIndex, __0);
        }
    }

    [HarmonyPatch(typeof(MultiPageUIManager), "CloseUI", new Type[] { })]
    public static class DocumentPageClosePatch
    {
        public static void Prefix(MultiPageUIManager __instance, int ___currentBookIndex, int ___currentPage)
        {
            if (__instance.panel != null && __instance.panel.activeSelf)
                DocumentPageMemory.Remember(___currentBookIndex, ___currentPage);
        }
    }
}
