using System;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace XicesBetterExperience
{
    [HarmonyPatch(typeof(ModConfigWindow), "Open")]
    public static class ModConfigTabs
    {
        private const string GeneralTab = "general";
        private const string IconTab = "icons";
        private const string AutomationTab = "automation";

        public static bool Prefix(LoadedMod mod, ref CustomUIWindow __result)
        {
            if (mod == null || mod.Manifest == null || mod.Manifest.ID != ModEntry.ModId)
                return true;
            __result = Open(mod);
            return false;
        }

        private static CustomUIWindow Open(LoadedMod mod)
        {
            ModConfig config = ModConfig.Find(mod.Manifest.ID);
            if (config == null || !config.HasEntries) return null;
            CustomUIManager manager = CustomUIManager.Instance;
            if (manager == null) return null;
            bool meta = StoreUIManager.Instance != null;
            string layer = meta ? "meta" : "menu";
            CustomUIBuilder b = manager.CreateWindow("mod_config:" + mod.Manifest.ID, mod.Manifest.Name,
                layer, manager.optionUIPrefab).SetCloseOnEscape(!meta);
            CustomUIWindow window = null;
            b.BeginScrollFlexible();
            b.BeginTabs();
            b.BeginTab(Text("config_tab_general"));
            BuildEntries(b, config, GeneralTab);
            b.End();
            b.BeginTab(Text("config_tab_icons"));
            BuildEntries(b, config, IconTab);
            b.End();
            b.BeginTab(Text("config_tab_automation"));
            BuildEntries(b, config, AutomationTab);
            b.End();
            b.EndTabs();
            b.End();
            b.AddSeparator(null, 2);
            b.BeginRow(12f, TextAnchor.MiddleCenter);
            b.AddFlexibleSpace();
            b.AddButton(LocHelper.GetLocalizedUI("ui_modcfg_reset"), delegate { ResetWidgets(window, config); }, "reset");
            b.Last.SetFixedWidth(220f);
            b.AddButton(LocHelper.GetLocalizedMainMenu("option_apply"), delegate { Apply(window, config); }, "apply");
            b.Last.SetFixedWidth(220f);
            b.AddFlexibleSpace();
            b.End();
            b.BeginRow(0f, TextAnchor.MiddleCenter);
            b.AddFlexibleSpace();
            b.AddWarning(LocHelper.GetLocalizedUI("ui_mods_restart_note"), "restart_note");
            b.AddFlexibleSpace();
            b.End();
            window = b.Show();
            window.Get("restart_note")?.SetVisible(false);
            return window;
        }

        private static void BuildEntries(CustomUIBuilder b, ModConfig config, string tab)
        {
            foreach (ConfigEntry entry in config.Entries)
            {
                if (ExperienceSettings.IsInternalSetting(entry.Key)) continue;
                if (ExperienceSettings.SettingsTab(entry.Key) != tab) continue;
                BuildEntry(b, config, entry);
                if (ExperienceSettings.IsAutomationSetting(entry.Key)) AppraisalAutomationSlider.AddExtras(b, config, entry);
            }
        }

        private static void BuildEntry(CustomUIBuilder b, ModConfig config, ConfigEntry entry)
        {
            string label = Safe(entry.Label, entry.Key);
            if (entry.Tooltip != null)
            {
                string link = "modcfg:" + config.ModId + ":" + entry.Key;
                TMPIconHover.CustomLinks[link] = entry.Tooltip;
                label += " <link=\"" + link + "\"><sprite name=\"info\" tint=1></link>";
            }
            b.BeginRow(4f, TextAnchor.MiddleLeft);
            b.AddLabel(label, "label_" + entry.Key);
            b.AddFlexibleSpace();
            Type type = entry.ValueType;
            object value = entry.BoxedValue;
            if (type == typeof(bool)) b.AddToggle("", (bool)value, null, entry.Key);
            else if (IsNumber(type) && entry.HasRange)
            {
                b.AddSlider(entry.Min, entry.Max, Convert.ToSingle(value, CultureInfo.InvariantCulture), null,
                    type == typeof(int), entry.Key);
                b.Last.SetFixedWidth(224f);
            }
            else if (entry.Choices != null && entry.Choices.Length != 0)
            {
                b.AddDropdown(entry.Choices, Mathf.Max(0, Array.IndexOf(entry.Choices, ConfigEntry.Format(value))), null, entry.Key);
                b.Last.SetFixedWidth(245f);
            }
            else
            {
                b.AddInput("", ConfigEntry.Format(value), null, entry.Key);
                b.Last.SetFixedWidth(245f);
            }
            b.End();
        }

        private static void Apply(CustomUIWindow window, ModConfig config)
        {
            if (window == null || !window.IsAlive) return;
            bool changed = false, restart = false;
            foreach (ConfigEntry entry in config.Entries)
            {
                if (ExperienceSettings.IsInternalSetting(entry.Key)) continue;
                CustomUIElement element = window.Get(entry.Key);
                if (element == null || !element.IsAlive) continue;
                object value = ReadWidget(entry, element);
                if (value == null) WriteWidget(entry, element, entry.BoxedValue);
                else if (SetEntry(entry, value))
                {
                    changed = true;
                    restart |= entry.RequiresRestart;
                    WriteWidget(entry, element, entry.BoxedValue);
                }
            }
            if (changed)
            {
                config.Save();
                ModEntry.RefreshAfterConfigApplied(config.ModId);
            }
            if (restart) window.Get("restart_note")?.SetVisible(true);
        }

        private static void ResetWidgets(CustomUIWindow window, ModConfig config)
        {
            if (window == null || !window.IsAlive) return;
            foreach (ConfigEntry entry in config.Entries)
            {
                if (ExperienceSettings.IsInternalSetting(entry.Key)) continue;
                CustomUIElement element = window.Get(entry.Key);
                if (element != null && element.IsAlive) WriteWidget(entry, element, entry.BoxedDefault);
            }
        }

        private static object ReadWidget(ConfigEntry entry, CustomUIElement element)
        {
            Type type = entry.ValueType;
            if (type == typeof(bool)) return element.GetBool();
            if (IsNumber(type) && entry.HasRange)
            {
                float value = element.GetFloat();
                return type == typeof(int) ? (object)Mathf.RoundToInt(value) : value;
            }
            if (entry.Choices != null && entry.Choices.Length != 0)
            {
                int index = element.GetIndex();
                if (index < 0 || index >= entry.Choices.Length) return null;
                return type.IsEnum ? Enum.Parse(type, entry.Choices[index]) : entry.Choices[index];
            }
            string text = element.GetString();
            if (type == typeof(int)) { int value; return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? (object)value : null; }
            if (type == typeof(float)) { float value; return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? (object)value : null; }
            return text;
        }

        private static void WriteWidget(ConfigEntry entry, CustomUIElement element, object value)
        {
            if (entry.ValueType == typeof(bool)) element.SetBool((bool)value);
            else if (IsNumber(entry.ValueType) && entry.HasRange) element.SetFloat(Convert.ToSingle(value, CultureInfo.InvariantCulture));
            else if (entry.Choices != null && entry.Choices.Length != 0) element.SetIndex(Mathf.Max(0, Array.IndexOf(entry.Choices, ConfigEntry.Format(value))));
            else element.SetString(ConfigEntry.Format(value));
        }

        private static bool SetEntry(ConfigEntry entry, object value)
        {
            ConfigEntry<bool> boolean = entry as ConfigEntry<bool>;
            if (boolean != null && value is bool) { bool oldValue = boolean.Value; boolean.Set((bool)value); return oldValue != boolean.Value; }
            ConfigEntry<int> integer = entry as ConfigEntry<int>;
            if (integer != null && value is int) { int oldValue = integer.Value; integer.Set((int)value); return oldValue != integer.Value; }
            ConfigEntry<float> single = entry as ConfigEntry<float>;
            if (single != null && value is float) { float oldValue = single.Value; single.Set((float)value); return oldValue != single.Value; }
            ConfigEntry<string> text = entry as ConfigEntry<string>;
            if (text != null && value is string) { string oldValue = text.Value; text.Set((string)value); return oldValue != text.Value; }
            return false;
        }

        private static bool IsNumber(Type type) { return type == typeof(int) || type == typeof(float); }
        private static string Text(string key) { return ModHelper.GetLocalized(ModEntry.ModId, key); }
        private static T Safe<T>(Func<T> getter, T fallback)
        {
            if (getter == null) return fallback;
            try { return getter(); } catch (Exception error) { Debug.LogError("[ModConfig] Label or tooltip callback threw: " + error); return fallback; }
        }
    }
}
