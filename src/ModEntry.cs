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
            ModHook.OnGameLoadedLate += StorageNameOverlay.RefreshLoaded;
            ModHook.OnGameLoadedLate += StolenHeatMarker.RefreshLoaded;
            ModHook.OnGameLoadedLate += ContrabandMarker.RefreshLoaded;
            ModHook.OnGameLoadedLate += PowerWarningMarker.RefreshLoaded;
            ModHook.OnGameLoadedLate += MarketChangeMarker.RefreshLoaded;
            ModHook.OnModConfigApplied += OnConfigApplied;
            StorageNameOverlay.RefreshLoaded();
            StolenHeatMarker.RefreshLoaded();
            ContrabandMarker.RefreshLoaded();
            PowerWarningMarker.RefreshLoaded();
            MarketChangeMarker.RefreshLoaded();
        }

        public void OnDisable()
        {
            if (!IsEnabled) return;
            IsEnabled = false;
            ModHook.OnGameLoadedLate -= StorageNameOverlay.RefreshLoaded;
            ModHook.OnGameLoadedLate -= StolenHeatMarker.RefreshLoaded;
            ModHook.OnGameLoadedLate -= ContrabandMarker.RefreshLoaded;
            ModHook.OnGameLoadedLate -= PowerWarningMarker.RefreshLoaded;
            ModHook.OnGameLoadedLate -= MarketChangeMarker.RefreshLoaded;
            ModHook.OnModConfigApplied -= OnConfigApplied;
            StorageNameOverlay.Clear();
            StolenHeatMarker.Clear();
            ContrabandMarker.Clear();
            PowerWarningMarker.Clear();
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
            StorageNameOverlay.RefreshLoaded();
            StolenHeatMarker.RefreshLoaded();
            ContrabandMarker.RefreshLoaded();
            PowerWarningMarker.RefreshLoaded();
            MarketChangeMarker.RefreshLoaded();
        }
    }
}
