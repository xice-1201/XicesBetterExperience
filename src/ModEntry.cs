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
            ModuleAffixMarker.Clear();
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
        private sealed class VisualSettings
        {
            public bool StorageNaming = ExperienceSettings.StorageNaming;
            public bool StorageCapacity = ExperienceSettings.StorageCapacity;
            public bool StolenMarker = ExperienceSettings.StolenMarker;
            public bool ContrabandMarker = ExperienceSettings.ContrabandMarker;
            public bool PowerWarning = ExperienceSettings.PowerWarning;
            public bool OverflowWarning = ExperienceSettings.OverflowWarning;
            public bool MarketChangeMarker = ExperienceSettings.MarketChangeMarker;
            public bool ModuleAffixMarker = ExperienceSettings.ModuleAffixMarker;
        }

        private static VisualSettings lastVisualSettings;

        private static void RefreshLoaded() { RefreshVisuals(true); }

        private static void RefreshVisuals(bool force)
        {
            var settings = new VisualSettings();
            var previous = lastVisualSettings;
            bool all = force || previous == null;
            bool storageNaming = all || settings.StorageNaming != previous.StorageNaming;
            bool storageCapacity = all || settings.StorageCapacity != previous.StorageCapacity;
            bool statusBackground = all || settings.StolenMarker != previous.StolenMarker ||
                settings.ContrabandMarker != previous.ContrabandMarker;
            bool powerWarning = all || settings.PowerWarning != previous.PowerWarning;
            bool overflowWarning = all || settings.OverflowWarning != previous.OverflowWarning;
            bool marketChange = all || settings.MarketChangeMarker != previous.MarketChangeMarker;
            bool moduleAffix = all || settings.ModuleAffixMarker != previous.ModuleAffixMarker;
            lastVisualSettings = settings;

            if (!storageNaming && !storageCapacity && !statusBackground && !powerWarning && !overflowWarning && !marketChange && !moduleAffix) return;
            var snapshot = PlayerStore.IsInstanceExist() && EmporiumEntry.Instance != null
                ? PlayerStore.Instance.FindAllItem(false) : new System.Collections.Generic.List<GameItem>();
            if (storageNaming) StorageNameOverlay.RefreshLoaded(snapshot);
            if (storageCapacity) StorageCapacityOverlay.RefreshLoaded(snapshot);
            if (statusBackground) ItemStatusBackground.RefreshLoaded(snapshot);
            if (powerWarning) PowerWarningMarker.RefreshLoaded(snapshot);
            if (powerWarning || overflowWarning) OverflowWarningMarker.RefreshLoaded(snapshot);
            if (marketChange) MarketChangeMarker.RefreshLoaded(snapshot);
            if (moduleAffix) ModuleAffixMarker.RefreshLoaded(snapshot);
        }
    }
}
