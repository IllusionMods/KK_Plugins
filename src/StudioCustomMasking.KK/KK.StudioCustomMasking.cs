using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using System.IO;
using System.Reflection;
using UILib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BepInEx.Configuration;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class StudioCustomMasking : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studiocustommasking";
        public const string PluginName = "Studio Custom Masking";
        public const string PluginNameInternal = Constants.Prefix + "_StudioCustomMasking";
        public const string Version = "1.0";

        internal static new ManualLogSource Logger;
        public static StudioCustomMasking Instance;
        internal static bool SavingInProgress = false;

        public static ConfigEntry<Color> ColliderColor { get; private set; }
        public static ConfigEntry<bool> AddNewMaskToSelected { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(Hooks));

            SceneManager.sceneLoaded += (s, lsm) => InitStudioUI(s.name);
            StudioSaveLoadApi.RegisterExtraBehaviour<StudioCustomMaskingSceneController>(GUID);

            ColliderColor = Config.Bind("Config", "Collider Color", Color.green, "Color of the collider box drawn when one is selected");
            AddNewMaskToSelected = Config.Bind("Config", "Add New Masks To Selected Object", true, "When enabled, newly created masks will be added to the currently selected object rather than added without a parent object.");
        }

        /// <summary>
        /// Add the button for creating the masking folders
        /// </summary>
        private void InitStudioUI(string sceneName)
        {
            if (sceneName != "Studio") return;
            SceneManager.sceneLoaded -= (s, lsm) => InitStudioUI(s.name);

            RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar/Button Route").GetComponent<RectTransform>();
            Button colliderFolderButton = Instantiate(original.gameObject).GetComponent<Button>();
            RectTransform colliderFolderButtonRectTransform = colliderFolderButton.transform as RectTransform;
            colliderFolderButton.transform.SetParent(original.parent, true);
            colliderFolderButton.transform.localScale = original.localScale;
            colliderFolderButtonRectTransform.SetRect(original.anchorMin, original.anchorMax, original.offsetMin, original.offsetMax);
            colliderFolderButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(-96f, 0f);

            Texture2D texture2D = new Texture2D(32, 32);
            texture2D.LoadImage(LoadIcon());
            var icon = colliderFolderButton.targetGraphic as Image;
            icon.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
            icon.color = Color.white;

            colliderFolderButton.onClick = new Button.ButtonClickedEvent();
            colliderFolderButton.onClick.AddListener(() => { SceneControllerInstance.CreateMaskingFolder(); });

            Camera.main.gameObject.AddComponent<DrawColliderLines>();
        }

        /// <summary>
        /// Load the button icon
        /// </summary>
        private byte[] LoadIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.CustomMaskingIcon.png"))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                return bytesInStream;
            }
        }

        private static StudioCustomMaskingSceneController _SceneControllerInstance;
        public static StudioCustomMaskingSceneController SceneControllerInstance
        {
            get
            {
                if (_SceneControllerInstance == null)
                    _SceneControllerInstance = Chainloader.ManagerObject.transform.GetComponentInChildren<StudioCustomMaskingSceneController>();
                return _SceneControllerInstance;
            }
        }
    }
}
