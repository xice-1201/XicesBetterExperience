using System;
using UnityEngine;

namespace XicesBetterExperience
{
    // One-way orchestration: power takes priority over output overflow.
    internal static class MachineWarningCoordinator
    {
        public static void Refresh(GameItem item)
        {
            var element = item as GameItemElement;
            if (element == null) return;
            try
            {
                bool powerWarningActive = PowerWarningMarker.IsWarningActive(element);
                PowerWarningMarker.RefreshElement(element, powerWarningActive);
                OverflowWarningMarker.RefreshElement(element, powerWarningActive);
            }
            catch (Exception error)
            {
                Debug.LogError("[XicesBetterExperience] Could not refresh machine warnings: " + error);
            }
        }
    }
}
