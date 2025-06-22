using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using System;
using System.IO;
using System.Reflection;
using KKAPI.Utilities;
using Screencap;
using UILib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_Plugins.StudioCustomMasking
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(StudioSceneSettings.StudioSceneSettings.GUID, StudioSceneSettings.StudioSceneSettings.Version)]
    [BepInDependency(ScreenshotManager.GUID, ScreenshotManager.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class StudioCustomMasking : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studiocustommasking";
        public const string PluginName = "Studio Custom Masking";
        public const string PluginNameInternal = Constants.Prefix + "_StudioCustomMasking";
        public const string Version = "1.1.1";

        internal static new ManualLogSource Logger;
        public static StudioCustomMasking Instance;

        /// <summary>
        /// If true, lines are not rendered. For overriding line drawing during screenshots, scene thumbnail generation, etc.
        /// </summary>
        public static bool HideLines = false;

#if KK || KKS
        internal const int ColliderLayer = 11;
#elif HS2 || AI
        internal const int ColliderLayer = 19;
#endif

        public static ConfigEntry<Color> ColliderColor { get; private set; }
        public static ConfigEntry<bool> AddNewMaskToSelected { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            Instance = this;
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

            SceneManager.sceneLoaded += InitStudioUI;
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(GUID);

            ColliderColor = Config.Bind("Config", "Collider Color", Color.green, "Color of the collider box drawn when one is selected");
            AddNewMaskToSelected = Config.Bind("Config", "Add New Masks To Selected Object", true, "When enabled, newly created masks will be added to the currently selected object rather than added without a parent object.");

            ColliderColor.SettingChanged += ColliderColor_SettingChanged;

            ScreenshotManager.OnPreCapture += () => HideLines = true;
            ScreenshotManager.OnPostCapture += () => HideLines = false;
        }

        private static void ColliderColor_SettingChanged(object sender, EventArgs e)
        {
            var objects = FindObjectsOfType<DrawColliderLines>();
            for (var i = 0; i < objects.Length; i++)
            {
                var colliderLines = objects[i];
                colliderLines.LineMaterial = null;
            }
        }

        /// <summary>
        /// Add the button for creating the masking folders
        /// </summary>
        private static void InitStudioUI(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name != "Studio") return;
            SceneManager.sceneLoaded -= InitStudioUI;

            RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar/Button Route").GetComponent<RectTransform>();
            Button colliderFolderButton = Instantiate(original.gameObject).GetComponent<Button>();
            Transform colliderFolderButtonTransform = colliderFolderButton.transform;
            RectTransform colliderFolderButtonRectTransform = colliderFolderButtonTransform as RectTransform;
            colliderFolderButtonTransform.SetParent(original.parent, true);
            colliderFolderButtonTransform.localScale = original.localScale;
            colliderFolderButtonRectTransform.SetRect(original.anchorMin, original.anchorMax, original.offsetMin, original.offsetMax);
            colliderFolderButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(-96f, 0f);

            Texture2D texture2D = new Texture2D(32, 32);
            texture2D.LoadImage(LoadIcon());
            var icon = colliderFolderButton.targetGraphic as Image;
            icon.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
            icon.color = Color.white;

            colliderFolderButton.onClick = new Button.ButtonClickedEvent();
            colliderFolderButton.onClick.AddListener(() =>
            {
                SceneControllerInstance.CreateMaskingFolder();
                if (!StudioSceneSettings.SceneController.MapMasking.Value)
                    Logger.LogMessage("The mask will not work until you turn on [system\\Scene Effects\\Map Masking]!");
            });

            Camera.main.gameObject.AddComponent<DrawColliderLines>();
        }

        /// <summary>
        /// Load the button icon
        /// </summary>
        private static byte[] LoadIcon()
        {
            return ResourceUtils.GetEmbeddedResource("CustomMaskingIcon.png");
        }

        private static SceneController _SceneControllerInstance;
        public static SceneController SceneControllerInstance
        {
            get
            {
                if (_SceneControllerInstance == null)
                    _SceneControllerInstance = Chainloader.ManagerObject.transform.GetComponentInChildren<SceneController>();
                return _SceneControllerInstance;
            }
        }
    }
}
