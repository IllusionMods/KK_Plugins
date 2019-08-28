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
        public const string Version = "1.5";
        internal static new ManualLogSource Logger;

        internal static Dictionary<string, string> SubtitleDictionary = new Dictionary<string, string>();

        #region ConfigMgr
        public static ConfigWrapper<bool> ShowSubtitles { get; private set; }
        public static ConfigWrapper<string> FontName { get; private set; }
        public static ConfigWrapper<int> FontSize { get; private set; }
        public static ConfigWrapper<FontStyle> FontStyle { get; private set; }
        public static ConfigWrapper<TextAnchor> TextAlign { get; private set; }
        public static ConfigWrapper<int> TextOffset { get; private set; }
        public static ConfigWrapper<int> OutlineThickness { get; private set; }
        public static ConfigWrapper<Color> TextColor { get; private set; }
        public static ConfigWrapper<Color> OutlineColor { get; private set; }
        #endregion

        private void Awake()
        {
#if HS
            if (Application.productName != "HoneySelect") return;
#endif
            Logger = base.Logger;

            ShowSubtitles = Config.GetSetting("Config", "Show Untranslated Text", true, new ConfigDescription("Whether or not to show untranslated text."));
            FontName = Config.GetSetting("Config", "Font Name", "Arial", new ConfigDescription("Name of the font to use for subtitle text."));
            FontSize = Config.GetSetting("Config", "Font Size", -5, new ConfigDescription("Font size of subtitles."));
            FontStyle = Config.GetSetting("Config", "Font Style", UnityEngine.FontStyle.Bold, new ConfigDescription("Font style of subtitles, i.e. bold, italic, etc."));
            TextAlign = Config.GetSetting("Config", "Text Align", TextAnchor.LowerCenter, new ConfigDescription("Text alignment of subtitles."));
            TextOffset = Config.GetSetting("Config", "Text Offset", 10, new ConfigDescription("Distance from edge of the screen."));
            OutlineThickness = Config.GetSetting("Config", "Outline Thickness", 2, new ConfigDescription("Outline thickness for subtitle text."));
            TextColor = Config.GetSetting("Config", "Text Color", ColorUtility.TryParseHtmlString("#FFCCFFFF", out Color color) ? color : Color.magenta, new ConfigDescription("Subtitle text color."));
            OutlineColor = Config.GetSetting("Config", "Outline Color", Color.black, new ConfigDescription("Subtitle text outline color."));

            LoadSubtitles();

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
