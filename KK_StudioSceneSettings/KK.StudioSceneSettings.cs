using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class StudioSceneSettings : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studioscenesettings";
        public const string PluginName = "StudioSceneSettings";
        public const string PluginNameInternal = "KK_StudioSceneSettings";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        internal const int CameraMapMaskingLayer = 26;
        internal const float NearClipIncrementValue = 0.1f;
        internal const float FarClipIncrementValue = 1f;

        public static float CameraNearClipPlaneDefault { get; private set; }
        public static float CameraFarClipPlaneDefault { get; private set; }
        public static int CameraLayerDefault { get; private set; }

        public static ConfigEntry<KeyboardShortcut> HotkeyNearClipPlanePlus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> HotkeyNearClipPlaneMinus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> HotkeyNearClipPlaneReset { get; private set; }
        public static ConfigEntry<KeyboardShortcut> HotkeyFarClipPlanePlus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> HotkeyFarClipPlaneMinus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> HotkeyFarClipPlanePlusTen { get; private set; }
        public static ConfigEntry<KeyboardShortcut> HotkeyFarClipPlaneMinusTen { get; private set; }
        public static ConfigEntry<KeyboardShortcut> HotkeyFarClipPlaneReset { get; private set; }
        public static ConfigEntry<KeyboardShortcut> HotkeyToggleMapMasking { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            HotkeyNearClipPlanePlus = Config.Bind("Keyboard Shortcuts", "Near Clip Plane Plus", new KeyboardShortcut(KeyCode.M, KeyCode.LeftControl), "Modifies the nearClipPlane");
            HotkeyNearClipPlaneMinus = Config.Bind("Keyboard Shortcuts", "Near Clip Plane Minus", new KeyboardShortcut(KeyCode.N, KeyCode.LeftControl), "Modifies the nearClipPlane");
            HotkeyNearClipPlaneReset = Config.Bind("Keyboard Shortcuts", "Near Clip Plane Reset", new KeyboardShortcut(KeyCode.B, KeyCode.LeftControl), "Modifies the nearClipPlane");
            HotkeyFarClipPlanePlus = Config.Bind("Keyboard Shortcuts", "Far Clip Plane Plus", new KeyboardShortcut(KeyCode.M, KeyCode.LeftAlt), "Modifies the farClipPlane");
            HotkeyFarClipPlaneMinus = Config.Bind("Keyboard Shortcuts", "Far Clip Plane Minus", new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt), "Modifies the farClipPlane");
            HotkeyFarClipPlaneReset = Config.Bind("Keyboard Shortcuts", "Far Clip Plane Reset", new KeyboardShortcut(KeyCode.B, KeyCode.LeftAlt), "Modifies the farClipPlane");
            HotkeyFarClipPlanePlusTen = Config.Bind("Keyboard Shortcuts", "Far Clip Plane Plus x10", new KeyboardShortcut(KeyCode.M, KeyCode.LeftAlt, KeyCode.LeftControl), "Modifies the farClipPlane");
            HotkeyFarClipPlaneMinusTen = Config.Bind("Keyboard Shortcuts", "Far Clip Plane Minus x10", new KeyboardShortcut(KeyCode.N, KeyCode.LeftAlt, KeyCode.LeftControl), "Modifies the farClipPlane");
            HotkeyToggleMapMasking = Config.Bind("Keyboard Shortcuts", "Toggle Map Masking", new KeyboardShortcut(KeyCode.M), "Toggles map masking");

            StudioSaveLoadApi.RegisterExtraBehaviour<StudioSceneSettingsSceneController>(GUID);
            SceneManager.sceneLoaded += (s, lsm) => StudioStart(s.name);
        }

        private void StudioStart(string sceneName)
        {
            if (sceneName != "Studio") return;

            CameraNearClipPlaneDefault = Camera.main.nearClipPlane;
            CameraFarClipPlaneDefault = Camera.main.farClipPlane;
            CameraLayerDefault = Camera.main.gameObject.layer;
        }

        /// <summary>
        /// Returns the instance of the scene controller
        /// </summary>
        /// <returns></returns>
        public static StudioSceneSettingsSceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<StudioSceneSettingsSceneController>();

        public class StudioSceneSettingsSceneController : SceneCustomFunctionController
        {
            protected override void OnSceneSave()
            {
                var data = new PluginData();
                if (NearClipPlane == CameraNearClipPlaneDefault)
                    data.data[$"NearClipPlane"] = null;
                else
                    data.data[$"NearClipPlane"] = NearClipPlane;

                if (FarClipPlane == CameraFarClipPlaneDefault)
                    data.data[$"FarClipPlane"] = null;
                else
                    data.data[$"FarClipPlane"] = FarClipPlane;

                data.data[$"MapMasking"] = MapMasking;

                SetExtendedData(data);
            }

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                if (operation == SceneOperationKind.Load)
                {
                    SetDefaults();
                    var data = GetExtendedData();
                    if (data?.data != null)
                    {
                        if (data.data.TryGetValue("NearClipPlane", out var nearClipPlane) && nearClipPlane != null)
                            NearClipPlane = (float)nearClipPlane;
                        if (data.data.TryGetValue("FarClipPlane", out var farClipPlane) && farClipPlane != null)
                            NearClipPlane = (float)farClipPlane;
                        if (data.data.TryGetValue("MapMasking", out var mapMasking) && mapMasking != null)
                            MapMasking = (bool)mapMasking;
                    }
                }
                else if (operation == SceneOperationKind.Clear)
                    SetDefaults();
                else //Do not import saved data, keep current settings
                    return;
            }

            internal void Update()
            {
                if (HotkeyToggleMapMasking.Value.IsDown())
                {
                    MapMasking = !MapMasking;
                    Logger.LogMessage($"MapMasking:{MapMasking}");
                }
                else if (HotkeyNearClipPlanePlus.Value.IsDown())
                {
                    NearClipPlane += NearClipIncrementValue;
                    Logger.LogMessage($"NearClipPlane:{NearClipPlane:0.00}");
                }
                else if (HotkeyNearClipPlaneMinus.Value.IsDown())
                {
                    NearClipPlane -= NearClipIncrementValue;
                    Logger.LogMessage($"NearClipPlane:{NearClipPlane:0.00}");
                }
                else if (HotkeyNearClipPlaneReset.Value.IsDown())
                {
                    NearClipPlane = CameraNearClipPlaneDefault;
                    Logger.LogMessage($"NearClipPlane:{NearClipPlane:0.00} (reset)");
                }
                else if (HotkeyFarClipPlanePlus.Value.IsDown())
                {
                    FarClipPlane += FarClipIncrementValue;
                    Logger.LogMessage($"FarClipPlane:{FarClipPlane:0.00}");
                }
                else if (HotkeyFarClipPlaneMinus.Value.IsDown())
                {
                    FarClipPlane -= FarClipIncrementValue;
                    Logger.LogMessage($"FarClipPlane:{FarClipPlane:0.00}");
                }
                else if (HotkeyFarClipPlanePlusTen.Value.IsDown())
                {
                    FarClipPlane += FarClipIncrementValue * 10;
                    Logger.LogMessage($"FarClipPlane:{FarClipPlane:0.00}");
                }
                else if (HotkeyFarClipPlaneMinusTen.Value.IsDown())
                {
                    FarClipPlane -= FarClipIncrementValue * 10;
                    Logger.LogMessage($"FarClipPlane:{FarClipPlane:0.00}");
                }
                else if (HotkeyFarClipPlaneReset.Value.IsDown())
                {
                    FarClipPlane = CameraFarClipPlaneDefault;
                    Logger.LogMessage($"FarClipPlane:{FarClipPlane:0.00} (reset)");
                }
            }

            private void SetDefaults()
            {
                NearClipPlane = CameraNearClipPlaneDefault;
                FarClipPlane = CameraFarClipPlaneDefault;
                MapMasking = false;
            }

            /// <summary>
            /// Modify the NearClipPlane of the MainCamera
            /// </summary>
            public float NearClipPlane
            {
                get => Camera.main.nearClipPlane;
                set => Camera.main.nearClipPlane = value <= 0f ? 0f : value;
            }

            /// <summary>
            /// Modify the farClipPlane of the MainCamera
            /// </summary>
            public float FarClipPlane
            {
                get => Camera.main.farClipPlane;
                set => Camera.main.farClipPlane = value <= 0 ? 0f : value;
            }

            /// <summary>
            /// Turn on or off map masking by setting the MainCamera to the proper layer
            /// </summary>
            public bool MapMasking
            {
                get => Camera.main.gameObject.layer != CameraLayerDefault;
                set => Camera.main.gameObject.layer = value ? CameraMapMaskingLayer : CameraLayerDefault;
            }
        }
    }
}