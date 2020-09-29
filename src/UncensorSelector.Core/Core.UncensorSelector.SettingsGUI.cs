using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Illusion.Extensions;
using KKAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins
{
    internal partial class UncensorSelector
    {
        public interface ISettingsGUI
        {
            bool Visible { get; set; }
            int WindowID { get; }
            void DoOnGUI();
        }

        public class SettingsGUI<T> : ISettingsGUI where T : IUncensorData
        {
            private readonly HashSet<string> SelectedGUIDS = new HashSet<string>();

            private bool _visible;
            private readonly List<Entry> AllValues;
            private Rect GUIRect;
            private readonly bool Initialized;

            private int LastScreenHeight = -1;
            private int LastScreenWidth = -1;
            private int LastAllValuesCount = -1;
            public float LastWindowHeight = -1f;

            private ConfigEntry<string> Setting;

            internal SettingsGUI(IDictionary<string, T> uncensors, byte sex, string part)
            {
                WindowID = $"{GUID}.{nameof(SettingsGUI<T>)}.{sex}.{part}".GetHashCode();
                //Deal with duplicate display names (it happens)
                var displayNames = uncensors.Select(x => x.Value.DisplayName);

                string GetDisplayName(T uncensor)
                {
                    return displayNames.Count(n => n.Equals(uncensor.DisplayName, StringComparison.OrdinalIgnoreCase)) > 1 ? $"{uncensor.DisplayName} ({uncensor.GUID})" : uncensor.DisplayName;
                }

                AllValues = uncensors.Where(x => x.Value.AllowRandom).Select(x => new Entry(x.Key, GetDisplayName(x.Value))).OrderBy(x => x.Name).ToList();
                var msg = $"uncensors to exclude for {(sex == 0 ? "Male" : "Female")} {part}";
                ButtonText = $"Configure {msg}";
                TitleText = msg.ToTitleCase();
                Setting = null;
                Initialized = true;
            }
            public string TitleText { get; }
            public string ButtonText { get; }

            public int WindowID { get; }

            public bool Visible
            {
                get => _visible;
                set
                {
                    if (_visible == value) return;
                    _visible = value;
                    if (!value) return;
                    foreach (var otherGUI in SettingsGUIs.Where(x => x.WindowID != WindowID && x.Visible))
                    {
                        otherGUI.Visible = false;
                    }

                    UpdateRects();
                }
            }

            public void DoOnGUI()
            {
                if (!Initialized || Setting == null || !Visible) return;

                GUILayout.Window(WindowID, GUIRect, DoSettingsGUI, TitleText);
                IMGUIUtils.DrawSolidBox(GUIRect);
                IMGUIUtils.EatInputInRect(GUIRect);
            }

            private void UpdateRects()
            {
                var valueCountChanged = LastAllValuesCount != AllValues.Count;
                // try and detect 
                if (!valueCountChanged && LastScreenHeight == Screen.height && LastScreenWidth == Screen.width &&
                    (LastWindowHeight + 0.1f) >= GUIRect.height && GUIRect.height >= (LastWindowHeight - 0.1f)) return;

                var width = Math.Min(300f, Screen.width / 4f);

                var height = Math.Min(Screen.height * 0.9f, (valueCountChanged || GUIRect.height < 60) ? (AllValues.Count + 4) * 22f : GUIRect.height);
                GUIRect = new Rect(
                    // make sure it's not off screen and try not to overlap settings GUI
                    Screen.width > 1600 ? Screen.width * 0.75f - width * 0.5f : Screen.width - width,
                    Math.Max(0, Screen.height / 2f - height / 2f),
                    width, height);

                LastAllValuesCount = AllValues.Count;
                LastScreenHeight = Screen.height;
                LastScreenWidth = Screen.width;
                LastWindowHeight = height;
            }

            private void DoSettingsGUI(int id)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUILayout.Label("Select uncensors to exclude from random selection");
                GUILayout.EndHorizontal();

                foreach (var value in AllValues)
                {
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    {
                        GUI.changed = false;
                        var newVal = GUILayout.Toggle(SelectedGUIDS.Contains(value.GUID), value.Name, GUILayout.ExpandWidth(true));
                        if (GUI.changed)
                        {
                            if (newVal)
                            {
                                SelectedGUIDS.Add(value.GUID);
                            }
                            else
                            {
                                SelectedGUIDS.Remove(value.GUID);
                            }

                            Setting.Value = string.Join(",", SelectedGUIDS.ToArray());
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                GUI.changed = false;

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                {
                    if (GUILayout.Button("Done"))
                    {
                        Visible = false;
                        var manager = GetConfigurationManager();
                        if (manager != null) manager.DisplayingWindow = true;
                    }
                }
                GUILayout.EndHorizontal();
            }

            private static ConfigurationManager.ConfigurationManager GetConfigurationManager()
            {
                return Chainloader.ManagerObject.transform.GetComponentInChildren<ConfigurationManager.ConfigurationManager>();
            }

            private void UpdateSelecedGUIDS(string value)
            {
                UpdateGuidSet(value, SelectedGUIDS);
            }

            public void DrawSettingsGUI(ConfigEntryBase settingBase)
            {
                if (!Initialized)
                {
                    Logger.LogError("SettingsGUI not configured properly, unable to display");
                    return;
                }

                if (Setting == null)
                {
                    //First run, set things up
                    Setting = (ConfigEntry<string>)settingBase;

                    //Only update these on first run or when value changes 
                    //Otherwise it would recalculate on every OnGUI
                    Setting.SettingChanged += (o, s) => UpdateSelecedGUIDS(Setting.Value);
                    UpdateSelecedGUIDS(Setting.Value);

                    // add existing entries that don't currently map to a known uncensor at the end
                    // only need to do this on initial load
                    var knownGUIDS = new HashSet<string>(AllValues.Select(x => x.GUID));
                    foreach (var val in SelectedGUIDS.OrderBy(x => x))
                    {
                        if (knownGUIDS.Contains(val)) continue;
                        AllValues.Add(new Entry(val));
                    }

                    // close when config manager closes
                    var manager = GetConfigurationManager();
                    if (manager != null)
                    {
                        manager.DisplayingWindowChanged += (s, e) =>
                        {
                            if (Visible && !e.NewValue) Visible = false;
                        };
                    }
                }

                // draw button here
                if (GUILayout.Button(ButtonText))
                {
                    Visible = true;
                }
            }

            internal class Entry
            {
                internal Entry(string guid, string name)
                {
                    GUID = guid;
                    Name = name;
                }

                internal Entry(string guid) : this(guid, guid) { }

                public string GUID { get; }
                public string Name { get; }
            }
        }
    }
}