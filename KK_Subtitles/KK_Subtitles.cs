using ActionGame.Communication;
using BepInEx;
using BepInEx.Logging;
using Harmony;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_Subtitles
{
    /// <summary>
    /// Displays subitles on screen for H scenes and in dialogues
    /// </summary>
    [BepInPlugin(GUID, PluginNameInternal, Version)]
    public class KK_Subtitles : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.subtitles";
        public const string PluginName = "Subtitles";
        public const string PluginNameInternal = "KK_Subtitles";
        public const string Version = "1.2";
        internal static Info ActionGameInfoInstance;
        internal static HSceneProc HSceneProcInstance;
        public static bool DoSubtitle = false;

        #region ConfigMgr
        [DisplayName("Show Untranslated Text")]
        [Category("Caption Text")]
        public static ConfigWrapper<bool> showUntranslated { get; private set; }
        [DisplayName("Font")]
        [Category("Caption Text")]
        [Advanced(true)]
        public static ConfigWrapper<string> fontName { get; private set; }
        [DisplayName("Size")]
        [Category("Caption Text")]
        [Description("Positive values in px, negative values in % of screen size")]
        [AcceptableValueRange(-100, 300, false)]
        [Advanced(true)]
        public static ConfigWrapper<int> fontSize { get; private set; }
        [DisplayName("Style")]
        [Category("Caption Text")]
        [Description("Most available fonts are dynamic, but non-dynamic fonts only support Normal style.")]
        [Advanced(true)]
        public static ConfigWrapper<FontStyle> fontStyle { get; private set; }
        [DisplayName("Alignment")]
        [Category("Caption Text")]
        [Advanced(true)]
        public static ConfigWrapper<TextAnchor> textAlign { get; private set; }
        [DisplayName("Text Offset")]
        [Category("Caption Text")]
        [Description("Padding from bottom of screen")]
        [AcceptableValueRange(0, 100, false)]
        [Advanced(true)]
        public static ConfigWrapper<int> textOffset { get; private set; }
        [DisplayName("Outline Thickness")]
        [Category("Caption Text")]
        [AcceptableValueRange(0, 100, false)]
        [Advanced(true)]
        public static ConfigWrapper<int> outlineThickness { get; private set; }
        [DisplayName("Text Color")]
        [Category("Caption Text")]
        public static ConfigWrapper<Color> textColor { get; private set; }
        [DisplayName("Outline Color")]
        [Category("Caption Text")]
        public static ConfigWrapper<Color> outlineColor { get; private set; }
        #endregion

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_Subtitles));

            showUntranslated = new ConfigWrapper<bool>("showUntranslated", PluginNameInternal, true);
            fontName = new ConfigWrapper<string>("fontName", PluginNameInternal, "Arial");
            fontSize = new ConfigWrapper<int>("fontSize", PluginNameInternal, -5);
            fontStyle = new ConfigWrapper<FontStyle>("fontStyle", PluginNameInternal, FontStyle.Bold);
            textAlign = new ConfigWrapper<TextAnchor>("textAlignment", PluginNameInternal, TextAnchor.LowerCenter);
            textOffset = new ConfigWrapper<int>("textOffset", PluginNameInternal, 10);
            outlineThickness = new ConfigWrapper<int>("outlineThickness", PluginNameInternal, 2);
        }

        public void Start()
        {
            StartCoroutine(InitAsync());
        }

        private IEnumerator<WaitWhile> InitAsync()
        {
            yield return new WaitWhile(() => Singleton<Manager.Config>.Instance == null);

            string Col2str(Color c) => ColorUtility.ToHtmlStringRGBA(c);
            Color str2Col(string s) => ColorUtility.TryParseHtmlString("#" + s, out Color c) ? c : Color.clear;

            textColor = new ConfigWrapper<Color>("textColor", PluginNameInternal, str2Col, Col2str, Manager.Config.TextData.Font1Color);
            outlineColor = new ConfigWrapper<Color>("outlineColor", PluginNameInternal, str2Col, Col2str, Color.black);

            yield return null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadVoice), "Play")]
        public static void PlayVoice(LoadVoice __instance)
        {
            if (__instance.audioSource == null || __instance.audioSource.clip == null || __instance.audioSource.loop)
                return;

            if (HSceneProcInstance != null)
                Caption.DisplayHSubtitle(__instance);
            else if (ActionGameInfoInstance != null && DoSubtitle)
                Caption.DisplayDialogueSubtitle(__instance);

            //Captions for chara maker
            //There doesn't appear to be any text for these spoken lines. May have to transcribe them manually or something

            //assetBundleName:custom/samplevoice_00.unity3d assetName:csv_08_00
            //if (__instance.assetBundleName == "custom/samplevoice_00.unity3d" && __instance.assetName == "csv_08_00")
            //{
            //    Caption.InitGUI();
            //    Caption.DisplaySubtitle(__instance, "はじめまして、よろしくね！");
            //}

            DoSubtitle = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Info), "Init")]
        public static void InfoInit(Info __instance)
        {
            Caption.InitGUI();
            ActionGameInfoInstance = __instance;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TalkScene), "TouchFunc")]
        public static void TouchFunc(TalkScene __instance) => DoSubtitle = true;
        [HarmonyPostfix, HarmonyPatch(typeof(Info), nameof(Info.GetLeaveAloneVoice))]
        public static void GetLeaveAloneVoice(TalkScene __instance) => DoSubtitle = true;

        [HarmonyPostfix, HarmonyPatch(typeof(HVoiceCtrl), "Init")]
        public static void HVoiceCtrlInit()
        {
            Caption.InitGUI();
            HSceneProcInstance = FindObjectOfType<HSceneProc>();
        }
    }
}

