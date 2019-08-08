using ActionGame.Communication;
using BepInEx;
using Harmony;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using BepInEx.Logging;
using Logger = BepInEx.Logger;

namespace Subtitles
{
    /// <summary>
    /// Displays subitles on screen for H scenes and in dialogues
    /// </summary>
    [BepInPlugin(GUID, PluginNameInternal, Version)]
    public partial class Subtitles : BaseUnityPlugin
    {
        public const string PluginNameInternal = "KK_Subtitles";

        internal static Info ActionGameInfoInstance;
        internal static HSceneProc HSceneProcInstance;

        #region ConfigMgr
        [DisplayName("Show Subtitles")]
        [Category("Config")]
        public static ConfigWrapper<bool> showSubtitles { get; private set; }
        internal static bool ShowSubtitles => showSubtitles.Value;
        [DisplayName("Font")]
        [Category("Caption Text")]
        [Advanced(true)]
        public static ConfigWrapper<string> fontName { get; private set; }
        internal static string FontName => fontName.Value;
        [DisplayName("Size")]
        [Category("Caption Text")]
        [Description("Positive values in px, negative values in % of screen size")]
        [AcceptableValueRange(-100, 300, false)]
        [Advanced(true)]
        public static ConfigWrapper<int> fontSize { get; private set; }
        internal static int FontSize => fontSize.Value;
        [DisplayName("Style")]
        [Category("Caption Text")]
        [Description("Most available fonts are dynamic, but non-dynamic fonts only support Normal style.")]
        [Advanced(true)]
        public static ConfigWrapper<FontStyle> fontStyle { get; private set; }
        internal static FontStyle FontStyle => fontStyle.Value;
        [DisplayName("Alignment")]
        [Category("Caption Text")]
        [Advanced(true)]
        public static ConfigWrapper<TextAnchor> textAlign { get; private set; }
        internal static TextAnchor TextAlign => textAlign.Value;
        [DisplayName("Text Offset")]
        [Category("Caption Text")]
        [Description("Padding from bottom of screen")]
        [AcceptableValueRange(0, 100, false)]
        [Advanced(true)]
        public static ConfigWrapper<int> textOffset { get; private set; }
        internal static int TextOffset => textOffset.Value;
        [DisplayName("Outline Thickness")]
        [Category("Caption Text")]
        [AcceptableValueRange(0, 100, false)]
        [Advanced(true)]
        public static ConfigWrapper<int> outlineThickness { get; private set; }
        internal static int OutlineThickness => outlineThickness.Value;
        [DisplayName("Text Color")]
        [Category("Caption Text")]
        public static ConfigWrapper<Color> textColor { get; private set; }
        internal static Color TextColor => textColor.Value;
        [DisplayName("Outline Color")]
        [Category("Caption Text")]
        public static ConfigWrapper<Color> outlineColor { get; private set; }
        internal static Color OutlineColor => outlineColor.Value;
        #endregion

        private void Main()
        {
            HarmonyInstance.Create(GUID).PatchAll(typeof(Hooks));

            showSubtitles = new ConfigWrapper<bool>(nameof(showSubtitles), PluginNameInternal, true);
            fontName = new ConfigWrapper<string>(nameof(fontName), PluginNameInternal, "Arial");
            fontSize = new ConfigWrapper<int>(nameof(fontSize), PluginNameInternal, -5);
            fontStyle = new ConfigWrapper<FontStyle>(nameof(fontStyle), PluginNameInternal, FontStyle.Bold);
            textAlign = new ConfigWrapper<TextAnchor>(nameof(textAlign), PluginNameInternal, TextAnchor.LowerCenter);
            textOffset = new ConfigWrapper<int>(nameof(textOffset), PluginNameInternal, 10);
            outlineThickness = new ConfigWrapper<int>(nameof(outlineThickness), PluginNameInternal, 2);

            LoadSubtitles();
        }

        public void Start() => StartCoroutine(InitAsync());

        private IEnumerator<WaitWhile> InitAsync()
        {
            yield return new WaitWhile(() => Singleton<Manager.Config>.Instance == null);

            string Col2str(Color c) => ColorUtility.ToHtmlStringRGBA(c);
            Color str2Col(string s) => ColorUtility.TryParseHtmlString("#" + s, out Color c) ? c : Color.clear;

            textColor = new ConfigWrapper<Color>("textColor", PluginNameInternal, str2Col, Col2str, Manager.Config.TextData.Font1Color);
            outlineColor = new ConfigWrapper<Color>("outlineColor", PluginNameInternal, str2Col, Col2str, Color.black);

            yield return null;
        }

        private void LoadSubtitles()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{PluginNameInternal}.Resources.CharaMakerSubs.xml"))
            using (XmlReader reader = XmlReader.Create(stream))
            {
                XDocument doc = XDocument.Load(reader);
                foreach (var element in doc.Root.Elements("Sub"))
                    SubtitleDictionary[element.Attribute("Asset").Value] = element.Attribute("Text").Value;
            }
        }

        public static void Log(LogLevel level, object text) => Logger.Log(level, text);
        public static void Log(object text) => Logger.Log(LogLevel.Info, text);
        public static void LogDebug(object text) => Logger.Log(LogLevel.Debug, text);
    }
}
