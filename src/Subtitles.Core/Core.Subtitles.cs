using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace KK_Plugins
{
    /// <summary>
    /// Displays subitles on screen for H scenes and in dialogues
    /// </summary>
    public partial class Subtitles
    {
        public const string GUID = "com.deathweasel.bepinex.subtitles";
        public const string PluginName = "Subtitles";
        public const string Version = "1.6";
        public const string PluginNameInternal = Constants.Prefix + "_Subtitles";

        internal static Subtitles Instance;
        internal static new ManualLogSource Logger;

        internal static Dictionary<string, string> SubtitleDictionary = new Dictionary<string, string>();

        #region ConfigMgr
        public static ConfigEntry<bool> ShowSubtitles { get; private set; }
        public static ConfigEntry<string> FontName { get; private set; }
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

            ShowSubtitles = Config.Bind("Config", "Show Subtitles", true, "Enable or disable showing subtitles.");
            FontName = Config.Bind("Config", "Font Name", "Arial", "Name of the font to use for subtitle text.");
            FontSize = Config.Bind("Config", "Font Size", -5, "Font size of subtitles.");
            FontStyle = Config.Bind("Config", "Font Style", UnityEngine.FontStyle.Bold, "Font style of subtitles, i.e. bold, italic, etc.");
            TextAlign = Config.Bind("Config", "Text Align", TextAnchor.LowerCenter, "Text alignment of subtitles.");
            TextOffset = Config.Bind("Config", "Text Offset", 10, "Distance from edge of the screen.");
            OutlineThickness = Config.Bind("Config", "Outline Thickness", 2, "Outline thickness for subtitle text.");
            TextColor = Config.Bind("Config", "Text Color", ColorUtility.TryParseHtmlString("#FFCCFFFF", out Color color) ? color : Color.magenta, "Subtitle text color.");
            OutlineColor = Config.Bind("Config", "Outline Color", Color.black, "Subtitle text outline color.");
            SubtitleDirectory = Config.Bind("Config", "Subtitle Directory", $"Translation\\en\\{PluginNameInternal}", "Directory containing subtitle xml info, relative to the BepInEx folder.");

#if HS
            if (Application.productName != "HoneySelect") return;
#endif

            LoadSubtitles();

            HarmonyWrapper.PatchAll(typeof(Hooks));
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
