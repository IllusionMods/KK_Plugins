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

namespace KK_Plugins
{
    /// <summary>
    /// Displays subitles on screen for H scenes and in dialogues
    /// </summary>
#if KK
    [BepInProcess(Constants.MainGameProcessNameSteam)]
#endif
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.subtitles";
        public const string PluginName = "Subtitles";
        public const string Version = "1.6.1";
        public const string PluginNameInternal = Constants.Prefix + "_Subtitles";

        internal static Subtitles Instance;
        internal static new ManualLogSource Logger;

        internal static Dictionary<string, string> SubtitleDictionary = new Dictionary<string, string>();

#if KK
        internal static ActionGame.Communication.Info ActionGameInfoInstance;
        internal static HSceneProc HSceneProcInstance;
#elif HS2
        internal static HScene HSceneInstance;
#endif

        #region Config
        public static ConfigEntry<bool> ShowSubtitles { get; private set; }
        public static ConfigEntry<int> FontSize { get; private set; }
        public static ConfigEntry<FontStyle> FontStyle { get; private set; }
        public static ConfigEntry<TextAnchor> TextAlign { get; private set; }
        public static ConfigEntry<int> TextOffset { get; private set; }
        public static ConfigEntry<int> OutlineThickness { get; private set; }
        public static ConfigEntry<Color> TextColor { get; private set; }
        public static ConfigEntry<Color> OutlineColor { get; private set; }
        public static ConfigEntry<string> SubtitleDirectory { get; private set; }
        #endregion

        internal void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            ShowSubtitles = Config.Bind("Config", "Show Subtitles", true, new ConfigDescription("Enable or disable showing subtitles.", null, new ConfigurationManagerAttributes { Order = 10 }));
            SubtitleDirectory = Config.Bind("Config", "Subtitle Directory", $"Translation\\{PluginNameInternal}", new ConfigDescription("Directory containing subtitle xml info, relative to the BepInEx folder.", null, new ConfigurationManagerAttributes { Order = 9 }));
            FontSize = Config.Bind("Config", "Font Size", -5, new ConfigDescription("Font size of subtitles.", null, new ConfigurationManagerAttributes { Order = 8 }));
            FontStyle = Config.Bind("Config", "Font Style", UnityEngine.FontStyle.Bold, new ConfigDescription("Font style of subtitles, i.e. bold, italic, etc.", null, new ConfigurationManagerAttributes { Order = 7 }));
            TextAlign = Config.Bind("Config", "Text Align", TextAnchor.LowerCenter, new ConfigDescription("Text alignment of subtitles.", null, new ConfigurationManagerAttributes { Order = 6 }));
            TextOffset = Config.Bind("Config", "Text Offset", 10, new ConfigDescription("Distance from edge of the screen.", null, new ConfigurationManagerAttributes { Order = 5 }));
            OutlineThickness = Config.Bind("Config", "Outline Thickness", 2, new ConfigDescription("Outline thickness for subtitle text.", null, new ConfigurationManagerAttributes { Order = 4 }));
            TextColor = Config.Bind("Config", "Text Color", ColorUtility.TryParseHtmlString("#FFCCFFFF", out Color color) ? color : Color.magenta, new ConfigDescription("Subtitle text color.", null, new ConfigurationManagerAttributes { Order = 3 }));
            OutlineColor = Config.Bind("Config", "Outline Color", Color.black, new ConfigDescription("Subtitle text outline color.", null, new ConfigurationManagerAttributes { Order = 2 }));

            TextAlign.SettingChanged += TextAlign_SettingChanged;

            LoadSubtitles();

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private void TextAlign_SettingChanged(object sender, EventArgs e)
        {
            if (Caption.Pane == null) return;
            var vlg = Caption.Pane.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) return;
            vlg.childAlignment = TextAlign.Value;
        }

        private void LoadSubtitles()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.CharaMakerSubs.xml"))
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
