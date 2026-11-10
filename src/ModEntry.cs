using System;

namespace XicesBetterExperience
{
    public sealed class ModEntry : IMod
    {
        public const string ModId = "xice.betterexperience";
        public const string SupportedGameVersion = "0.46D";
        public static bool IsEnabled { get; private set; }
        internal static ModLog Logger { get; private set; }

        public void Init(ModManifest manifest)
        {
            if (manifest == null || manifest.ID != ModId)
                throw new ArgumentException("Unexpected XicesBetterExperience manifest ID.");
            string gameVersion = UnityEngine.Application.version;
            if (!String.Equals(gameVersion, SupportedGameVersion, StringComparison.Ordinal))
                throw new NotSupportedException(
                    "XicesBetterExperience supports Probably Stolen " + SupportedGameVersion +
                    " only. Current game version: " + gameVersion);
            Logger = new ModLog(manifest);
            Logger.Log("XicesBetterExperience initialized.");
            ExperienceSettings.Register();
        }

        public void OnEnable()
        {
            if (IsEnabled) return;
            IsEnabled = true;
            ModHook.OnGameLoadedLate += AugIdentificationAutomation.Clear;
            ModHook.OnGameLoadedLate += RefreshLoaded;
            ModHook.OnModConfigApplied += OnConfigApplied;
            RefreshLoaded();
        }

        public void OnDisable()
        {
            if (!IsEnabled) return;
            IsEnabled = false;
            WindowLayoutMemory.Clear();
            AugIdentificationAutomation.Clear();
            InjectorInspectionAutomation.Restore();
            ModHook.OnGameLoadedLate -= AugIdentificationAutomation.Clear;
            ModHook.OnGameLoadedLate -= RefreshLoaded;
            ModHook.OnModConfigApplied -= OnConfigApplied;
            StorageNameOverlay.Clear();
            StorageCapacityOverlay.Clear();
            ItemStatusBackground.Clear();
            PowerWarningMarker.Clear();
            OverflowWarningMarker.Clear();
            MarketChangeMarker.Clear();
        }

        private void OnConfigApplied(string modId)
        {
            if (modId != ModId) return;
            RefreshAfterConfigApplied(modId);
        }

        internal static void RefreshAfterConfigApplied(string modId)
        {
            if (modId != ModId || !IsEnabled) return;
            WindowLayoutMemory.ConfigApplied();
            AugIdentificationAutomation.ConfigApplied();
            RefreshVisuals(false);
        }
        private static bool[] lastVisualSettings;
        private static void RefreshLoaded() { RefreshVisuals(true); }
        private static void RefreshVisuals(bool force)
        {
            bool[] settings = { ExperienceSettings.StorageNaming, ExperienceSettings.StorageCapacity,
                ExperienceSettings.StolenMarker, ExperienceSettings.ContrabandMarker,
                ExperienceSettings.PowerWarning, ExperienceSettings.OverflowWarning, ExperienceSettings.MarketChangeMarker };
            bool[] changed = new bool[settings.Length];
            bool any = false;
            for (int i = 0; i < settings.Length; i++) { changed[i] = force || lastVisualSettings == null || settings[i] != lastVisualSettings[i]; any |= changed[i]; }
            lastVisualSettings = settings;
            if (!any) return;
            var snapshot = PlayerStore.IsInstanceExist() && EmporiumEntry.Instance != null
                ? PlayerStore.Instance.FindAllItem(false) : new System.Collections.Generic.List<GameItem>();
            if (changed[0]) StorageNameOverlay.RefreshLoaded(snapshot);
            if (changed[1]) StorageCapacityOverlay.RefreshLoaded(snapshot);
            if (changed[2] || changed[3]) ItemStatusBackground.RefreshLoaded(snapshot);
            if (changed[4]) PowerWarningMarker.RefreshLoaded(snapshot);
            if (changed[4] || changed[5]) OverflowWarningMarker.RefreshLoaded(snapshot);
            if (changed[6]) MarketChangeMarker.RefreshLoaded(snapshot);
        }

    }
}
