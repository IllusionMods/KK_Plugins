using BepInEx;
using BepInEx.Logging;
using Harmony;
using KKAPI;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UniRx;
using Logger = BepInEx.Logger;

namespace UncensorSelector
{
    /// <summary>
    /// Plugin for assigning uncensors to characters individually
    /// </summary>
    [BepInDependency(Sideloader.Sideloader.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID)]
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    internal partial class UncensorSelector : BaseUnityPlugin
    {
        #region Config
        [DisplayName("Genderbender allowed")]
        [Category("Config")]
        [Description("Whether or not genderbender characters are allowed. " +
            "When disabled, girls will always have a female body with no penis, boys will always have a male body and a penis. " +
            "Genderbender characters will still load in Studio for scene compatibility.")]
        public static ConfigWrapper<bool> GenderBender { get; private set; }
        [DisplayName("Default male body")]
        [Category("Config")]
        [Description("Body to use if character does not have one set. The censored body will not be selected randomly if there are any alternatives.")]
        [AcceptableValueList(nameof(GetConfigBodyList))]
        public static ConfigWrapper<string> DefaultMaleBody { get; private set; }
        [DisplayName("Default male penis")]
        [Category("Config")]
        [Description("Penis to use if character does not have one set. The mosaic penis will not be selected randomly if there are any alternatives.")]
        [AcceptableValueList(nameof(GetConfigPenisList))]
        public static ConfigWrapper<string> DefaultMalePenis { get; private set; }
        [DisplayName("Default male balls")]
        [Category("Config")]
        [Description("Balls to use if character does not have one set. The mosaic balls will not be selected randomly if there are any alternatives.")]
        [AcceptableValueList(nameof(GetConfigBallsList))]
        public static ConfigWrapper<string> DefaultMaleBalls { get; private set; }
        [DisplayName("Default female body")]
        [Category("Config")]
        [Description("Body to use if character does not have one set. The censored body will not be selected randomly if there are any alternatives.")]
        [AcceptableValueList(nameof(GetConfigBodyList))]
        public static ConfigWrapper<string> DefaultFemaleBody { get; private set; }
        [DisplayName("Default female penis")]
        [Category("Config")]
        [Description("Penis to use if character does not have one set. The mosaic penis will not be selected randomly if there are any alternatives.")]
        [AcceptableValueList(nameof(GetConfigPenisList))]
        public static ConfigWrapper<string> DefaultFemalePenis { get; private set; }
        [DisplayName("Default female balls")]
        [Category("Config")]
        [Description("Balls to use if character does not have one set. The mosaic balls will not be selected randomly if there are any alternatives.")]
        [AcceptableValueList(nameof(GetConfigBallsList))]
        public static ConfigWrapper<string> DefaultFemaleBalls { get; private set; }
        [DisplayName("Default female balls display")]
        [Category("Config")]
        [Description("Whether balls will be displayed on females if not otherwise configured.")]
        public static ConfigWrapper<bool> DefaultFemaleDisplayBalls { get; private set; }
        #endregion

        private void Start()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(Hooks));

            Type loadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).First(x => x.Name.StartsWith("<LoadAsync>c__Iterator"));
            MethodInfo loadAsyncIteratorMoveNext = loadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(loadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.LoadAsyncTranspiler), AccessTools.all)));

            GenderBender = new ConfigWrapper<bool>(nameof(GenderBender), PluginNameInternal, true);
            DefaultMaleBody = new ConfigWrapper<string>(nameof(DefaultMaleBody), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, "Random");
            DefaultMalePenis = new ConfigWrapper<string>(nameof(DefaultMalePenis), PluginNameInternal, PenisGuidToDisplayName, DisplayNameToPenisGuid, "Random");
            DefaultMaleBalls = new ConfigWrapper<string>(nameof(DefaultMaleBalls), PluginNameInternal, BallsGuidToDisplayName, DisplayNameToBallsGuid, "Random");
            DefaultFemaleBody = new ConfigWrapper<string>(nameof(DefaultFemaleBody), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, "Random");
            DefaultFemalePenis = new ConfigWrapper<string>(nameof(DefaultFemalePenis), PluginNameInternal, PenisGuidToDisplayName, DisplayNameToPenisGuid, "Random");
            DefaultFemaleBalls = new ConfigWrapper<string>(nameof(DefaultFemaleBalls), PluginNameInternal, BallsGuidToDisplayName, DisplayNameToBallsGuid, "Random");
            DefaultFemaleDisplayBalls = new ConfigWrapper<bool>(nameof(DefaultFemaleDisplayBalls), PluginNameInternal, false);
        }

        public static void Log(LogLevel level, object text) => Logger.Log(level, text);
        public static void Log(object text) => Logger.Log(LogLevel.Info, text);
    }
}