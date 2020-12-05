using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;

#if !PC
using UnityEngine.SceneManagement;
#endif

namespace KK_Plugins
{
    /// <summary>
    /// Displays subtitles on screen for H scenes and in dialogues
    /// </summary>
#if KK
    [BepInProcess(Constants.MainGameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
#elif HS2
    [BepInProcess(Constants.VRProcessName)]
#endif
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.subtitles";
        public const string PluginName = "Subtitles";
        public const string Version = "2.2";
        public const string PluginNameInternal = Constants.Prefix + "_Subtitles";

        internal static Subtitles Instance;
        internal static new ManualLogSource Logger;

        internal static Dictionary<string, string> SubtitleDictionary = new Dictionary<string, string>();

#if KK || EC || HS || PC
        internal const float WorldScale = 1f;
#elif AI || HS2
        internal const float WorldScale = 10f;
#endif

#if KK
        internal static ActionGame.Communication.Info ActionGameInfoInstance;
        internal static Type HSceneType;
        internal static UnityEngine.Object HSceneInstance;
#elif HS2
        internal static HScene HSceneInstance;
#endif

        #region Config
        public static ConfigEntry<bool> ShowSubtitles { get; private set; }
        public static ConfigEntry<int> FontSize { get; private set; }
        public static ConfigEntry<FontStyle> FontStyle { get; private set; }
        public static ConfigEntry<TextAnchor> TextAlign { get; private set; }
        public static ConfigEntry<int> TextVerticalOffset { get; private set; }
        public static ConfigEntry<int> TextHorizontalOffset { get; private set; }
        public static ConfigEntry<int> OutlineThickness { get; private set; }
        public static ConfigEntry<string> SubtitleDirectory { get; private set; }
#if !PC
        public static ConfigEntry<Color> TextColor { get; private set; }
        public static ConfigEntry<Color> OutlineColor { get; private set; }
        public static ConfigEntry<Vector3> VRTextOffset { get; private set; }
        public static ConfigEntry<Vector3> VRText2Offset { get; private set; }
#endif
        #endregion

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            ShowSubtitles = Config.Bind("Config", "Show Subtitles", true, new ConfigDescription("Enable or disable showing subtitles.", null, new ConfigurationManagerAttributes { Order = 11 }));
            SubtitleDirectory = Config.Bind("Config", "Subtitle Directory", $"Translation\\{PluginNameInternal}", new ConfigDescription("Directory containing subtitle xml info, relative to the BepInEx folder.", null, new ConfigurationManagerAttributes { Order = 10 }));
            FontSize = Config.Bind("Config", "Font Size", -5, new ConfigDescription("Font size of subtitles.", null, new ConfigurationManagerAttributes { Order = 9 }));
            FontStyle = Config.Bind("Config", "Font Style", UnityEngine.FontStyle.Bold, new ConfigDescription("Font style of subtitles, i.e. bold, italic, etc.", null, new ConfigurationManagerAttributes { Order = 8 }));
            TextAlign = Config.Bind("Config", "Text Align", TextAnchor.LowerCenter, new ConfigDescription("Text alignment of subtitles.", null, new ConfigurationManagerAttributes { Order = 7 }));
            TextVerticalOffset = Config.Bind("Config", "Text Vertical Offset", 10, new ConfigDescription("Distance from top and bottom edges of the screen.", null, new ConfigurationManagerAttributes { Order = 5 }));
            TextHorizontalOffset = Config.Bind("Config", "Text Horizontal Offset", 10, new ConfigDescription("Distance from left and right edges of the screen.", null, new ConfigurationManagerAttributes { Order = 6 }));
            OutlineThickness = Config.Bind("Config", "Outline Thickness", 2, new ConfigDescription("Outline thickness for subtitle text.", null, new ConfigurationManagerAttributes { Order = 4 }));
#if !PC
            TextColor = Config.Bind("Config", "Text Color", UnityEngine.ColorUtility.TryParseHtmlString("#FFCCFFFF", out Color color) ? color : Color.magenta, new ConfigDescription("Subtitle text color.", null, new ConfigurationManagerAttributes { Order = 3 }));
            OutlineColor = Config.Bind("Config", "Outline Color", Color.black, new ConfigDescription("Subtitle text outline color.", null, new ConfigurationManagerAttributes { Order = 2 }));
            VRTextOffset = Config.Bind("VR", "VR Text Offset", new Vector3(-0.1f * WorldScale, -0.1f * WorldScale, 0.5f * WorldScale), new ConfigDescription("Subtitle text position in VR.", null, new ConfigurationManagerAttributes { Order = 1 }));
            VRText2Offset = Config.Bind("VR", "VR Text 2 Offset", new Vector3(0.1f * WorldScale, -0.2f * WorldScale, 0.5f * WorldScale), new ConfigDescription("Subtitle text position in VR. For 3P when two subtitles may be displayed at once.", null, new ConfigurationManagerAttributes { Order = 0 }));
#endif
            TextAlign.SettingChanged += TextAlign_SettingChanged;

            LoadSubtitles();

            Harmony.CreateAndPatchAll(typeof(Hooks));

#if !HS && !PC
            SceneManager.sceneLoaded += Caption.SceneLoaded;
            SceneManager.sceneUnloaded += Caption.SceneUnloaded;
#endif
        }

        private static void TextAlign_SettingChanged(object sender, EventArgs e)
        {
            if (Caption.Pane == null) return;
            var vlg = Caption.Pane.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) return;
            vlg.childAlignment = TextAlign.Value;
        }

        private static void LoadSubtitles()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.EmbeddedSubs.xml"))
                if (stream != null)
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XDocument doc = XDocument.Load(reader);
                        foreach (var element in doc.Element(PluginNameInternal).Elements("Sub"))
                            SubtitleDictionary[element.Attribute("Asset").Value] = element.Attribute("Text").Value;
                    }

            string XMLPath = Path.Combine(Paths.BepInExRootPath, SubtitleDirectory.Value);
            if (Directory.Exists(XMLPath))
            {
                foreach (var fileName in Directory.GetFiles(XMLPath))
                {
                    try
                    {
                        XDocument doc = XDocument.Load(fileName);
                        foreach (var element in doc.Element(PluginNameInternal).Elements("Sub"))
                            SubtitleDictionary[element.Attribute("Asset").Value] = element.Attribute("Text").Value;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to load {PluginNameInternal} xml file.");
                        Logger.LogError(ex);
                    }
                }
            }
        }
    }
}
