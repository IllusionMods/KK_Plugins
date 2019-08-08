using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using System;
using System.IO;
using System.Xml.Linq;
using UnityEngine;

namespace Subtitles
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BaseUnityPlugin
    {
        public const string PluginNameInternal = "HS_Subtitles";

        private static ManualLogSource _logsource;
        private readonly string XMLPath = Paths.PluginPath + $@"\HS_Plugins\{PluginNameInternal}";

        #region ConfigMgr
        public static ConfigWrapper<bool> showSubtitles { get; private set; }
        internal static bool ShowSubtitles => showSubtitles.Value;
        public static ConfigWrapper<string> fontName { get; private set; }
        internal static string FontName => fontName.Value;
        public static ConfigWrapper<int> fontSize { get; private set; }
        internal static int FontSize => fontSize.Value;
        //public static ConfigWrapper<FontStyle> fontStyle { get; private set; }
        internal static FontStyle FontStyle => FontStyle.Normal;
        //public static ConfigWrapper<TextAnchor> textAlign { get; private set; }
        internal static TextAnchor TextAlign => TextAnchor.LowerCenter;
        public static ConfigWrapper<int> textOffset { get; private set; }
        internal static int TextOffset => textOffset.Value;
        public static ConfigWrapper<int> outlineThickness { get; private set; }
        internal static int OutlineThickness => outlineThickness.Value;
        //public static ConfigWrapper<Color> textColor { get; private set; }
        internal static Color TextColor => ColorUtility.TryParseHtmlString("#FFCCFFFF", out Color color) ? color : Color.magenta;
        internal static Color OutlineColor => Color.black;
        #endregion

        private void Main()
        {
            _logsource = Logger;
            HarmonyWrapper.PatchAll(typeof(Hooks));

            showSubtitles = Config.Wrap("Config", "Show Untranslated Text", "Whether or not to show untranslated text.", true);
            fontName = Config.Wrap("Config", "Font Name", "Name of the font to use for subtitle text.", "Arial");
            fontSize = Config.Wrap("Config", "Font Size", "Font size of subtitles.", -5);
            //fontStyle = Config.Wrap("Config", "Font Style", "Font style of subtitles, i.e. bold, italic, etc..", FontStyle.Bold);
            //textAlign = Config.Wrap("Config", "Text Align", "Text alignment of subtitles.", TextAnchor.LowerCenter);
            textOffset = Config.Wrap("Config", "Text Offset", "Distance from edge of the screen.", 10);
            outlineThickness = Config.Wrap("Config", "Outline Thickness", "Outline thickness for subtitle text.", 2);
            //textColor = Config.Wrap("Config", "Outline Thickness", "Subtitle text color.", Color.magenta);
            //outlineColor = Config.Wrap("Config", "Outline Thickness", "Subtitle text outline color.", Color.black);

            LoadSubtitles();
        }

        private void LoadSubtitles()
        {
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
                        Logger.Log(LogLevel.Error, $"Failed to load {PluginNameInternal} xml file.");
                        Logger.Log(LogLevel.Error, ex);
                    }
                }
            }
        }

        public static void Log(LogLevel level, object text) => _logsource.Log(level, text);
        public static void Log(object text) => _logsource.Log(LogLevel.Info, text);
        public static void LogDebug(object text) => _logsource.Log(LogLevel.Debug, text);
    }
}
