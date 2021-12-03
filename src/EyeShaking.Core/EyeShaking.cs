using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using System;
using System.Collections;
using System.Xml;
using UniRx;

namespace KK_Plugins
{
    /// <summary>
    /// Adds shaking to a character's eye highlights when she is a virgin in an H scene
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class EyeShaking : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.eyeshaking";
        public const string PluginName = "Eye Shaking";
        public const string PluginNameInternal = Constants.Prefix + "_EyeShaking";
        public const string Version = "1.3";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> Enabled { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
            CharacterApi.RegisterExtraBehaviour<EyeShakingController>(GUID);

            Enabled = Config.Bind("Config", "Enabled", true, "When enabled, virgins in H scenes will appear to have shaking eye highlights");

            //Patch the VR version of these methods via reflection
            Type VRHSceneType = Type.GetType("VRHScene, Assembly-CSharp");
            if (VRHSceneType != null)
            {
                harmony.Patch(VRHSceneType.GetMethod("MapSameObjectDisable", AccessTools.all), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.MapSameObjectDisableVR), AccessTools.all)));
                harmony.Patch(VRHSceneType.GetMethod("EndProc", AccessTools.all), new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.EndProcVR), AccessTools.all)));
            }

            if (StudioAPI.InsideStudio)
            {
                RegisterStudioControls();
                StartCoroutine(PopulateTimelineCoroutine());
            }
        }

        private IEnumerator PopulateTimelineCoroutine()
        {
            for (int i = 0; i < 10; ++i)
                yield return null;
            if (TimelineCompatibility.Init())
                PopulateTimeline();
        }

        private void PopulateTimeline()
        {
            TimelineCompatibility.AddInterpolableModelDynamic(
                owner: PluginName,
                id: "shakingEnabled",
                name: "Shaking Eye Highlights",
                interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((EyeShakingController)parameter).EyeShaking = (bool)leftValue,
                interpolateAfter: null,
                isCompatibleWithTarget: (oci) => oci is OCIChar,
                getValue: (oci, parameter) => ((EyeShakingController)parameter).EyeShaking,
                readValueFromXml: (parameter, node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString((bool)value)),
                getParameter: oci => GetController(((OCIChar)oci).GetChaControl()),
                readParameterFromXml: (oci, node) => GetController(((OCIChar)oci).GetChaControl()),
                getFinalName: (currentName, oci, parameter) => "Shaking Eye Highlights");
        }

        private static EyeShakingController GetController(ChaControl character) => character == null ? null : character.gameObject.GetComponent<EyeShakingController>();

        private static void RegisterStudioControls()
        {
            var invisibleSwitch = new CurrentStateCategorySwitch("Shaking Eye Highlights", controller => controller.charInfo.GetComponent<EyeShakingController>().EyeShaking);
            invisibleSwitch.Value.Subscribe(Observer.Create((bool value) =>
            {
                bool first = true;
                foreach (var controller in StudioAPI.GetSelectedControllers<EyeShakingController>())
                {
                    //Prevent changing other characters when the value did not actually change
                    if (first && controller.EyeShaking == value)
                        break;

                    first = false;
                    controller.EyeShaking = value;
                }
            }));

            StudioAPI.GetOrCreateCurrentStateCategory("").AddControl(invisibleSwitch);
        }
    }
}