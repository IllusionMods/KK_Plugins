using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        public const string GUID = "com.deathweasel.bepinex.subtitles";
        public const string PluginName = "Subtitles";
        public const string Version = "1.5.1";
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
        #endregion

        internal void Awake()
        {
            Logger = base.Logger;

            ShowSubtitles = Config.AddSetting("Config", "Show Subtitles", true, "Enable or disable showing subtitles.");
            FontName = Config.AddSetting("Config", "Font Name", "Arial", "Name of the font to use for subtitle text.");
            FontSize = Config.AddSetting("Config", "Font Size", -5, "Font size of subtitles.");
            FontStyle = Config.AddSetting("Config", "Font Style", UnityEngine.FontStyle.Bold, "Font style of subtitles, i.e. bold, italic, etc.");
            TextAlign = Config.AddSetting("Config", "Text Align", TextAnchor.LowerCenter, "Text alignment of subtitles.");
            TextOffset = Config.AddSetting("Config", "Text Offset", 10, "Distance from edge of the screen.");
            OutlineThickness = Config.AddSetting("Config", "Outline Thickness", 2, "Outline thickness for subtitle text.");
            TextColor = Config.AddSetting("Config", "Text Color", ColorUtility.TryParseHtmlString("#FFCCFFFF", out Color color) ? color : Color.magenta, "Subtitle text color.");
            OutlineColor = Config.AddSetting("Config", "Outline Color", Color.black, "Subtitle text outline color.");

#if HS
            if (Application.productName != "HoneySelect") return;
#endif

            LoadSubtitles();

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
