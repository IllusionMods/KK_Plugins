using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CharaCustom;
using KKAPI.Maker;
using System;
using UnityEngine.UI;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class MakerDefaults : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.makerdefaults";
        public const string PluginName = "Maker Defaults";
        public const string PluginNameInternal = Constants.Prefix + "_MakerDefaults";
        public const string Version = "1.0.1";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<GazeDirection> DefaultGazeDirection { get; private set; }
        public static ConfigEntry<HeadDirection> DefaultHeadDirection { get; private set; }
        public static ConfigEntry<int> DefaultPose { get; private set; }
        public static ConfigEntry<int> DefaultEyebrowPattern { get; private set; }
        public static ConfigEntry<int> DefaultEyePattern { get; private set; }
        public static ConfigEntry<int> DefaultMouthPattern { get; private set; }
        public static ConfigEntry<bool> DefaultPause { get; private set; }
        public static ConfigEntry<ClothingState> DefaultClothingState { get; private set; }
        public static ConfigEntry<Background> DefaultBackground { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            MakerAPI.MakerFinishedLoading += MakerFinishedLoading;
            DefaultGazeDirection = Config.Bind("Settings", "Default Gaze Direction", GazeDirection.Camera, new ConfigDescription("Default gaze direction", null, new ConfigurationManagerAttributes { Order = 9 }));
            DefaultHeadDirection = Config.Bind("Settings", "Default Head Direction", HeadDirection.Pose, new ConfigDescription("Default head direction", null, new ConfigurationManagerAttributes { Order = 8 }));
            DefaultPose = Config.Bind("Settings", "Default Pose", 1, new ConfigDescription("Default pose", new AcceptableValueRange<int>(0, 1000), new ConfigurationManagerAttributes { Order = 7 }));
            DefaultEyebrowPattern = Config.Bind("Settings", "Default Eyebrow Pattern", 1, new ConfigDescription("Default eyebrow pattern", new AcceptableValueRange<int>(1, 10), new ConfigurationManagerAttributes { Order = 6 }));
            DefaultEyePattern = Config.Bind("Settings", "Default Eye Pattern", 1, new ConfigDescription("Default eye pattern", new AcceptableValueRange<int>(1, 9), new ConfigurationManagerAttributes { Order = 5 }));
            DefaultMouthPattern = Config.Bind("Settings", "Default Mouth Pattern", 1, new ConfigDescription("Default mouth pattern", new AcceptableValueRange<int>(1, 18), new ConfigurationManagerAttributes { Order = 4 }));
            DefaultPause = Config.Bind("Settings", "Default Pause", false, new ConfigDescription("Default pause state", null, new ConfigurationManagerAttributes { Order = 3 }));
            DefaultClothingState = Config.Bind("Settings", "Default Clothing State", ClothingState.Automatic, new ConfigDescription("Default clothing state", null, new ConfigurationManagerAttributes { Order = 2 }));
            DefaultBackground = Config.Bind("Settings", "Default Background", Background.Color, new ConfigDescription("Default background type", null, new ConfigurationManagerAttributes { Order = 1 }));
        }

        private static void MakerFinishedLoading(object sender, EventArgs e)
        {
            //Gaze Direction
            if (DefaultGazeDirection.Value != GazeDirection.Camera)
                CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwChara/eyelook/items/tgl02").GetComponent<Toggle>().isOn = true;

            //Head Direction
            if (DefaultHeadDirection.Value != HeadDirection.Pose)
                CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwChara/necklook/items/tgl02").GetComponent<Toggle>().isOn = true;

            //Pose
            if (DefaultPose.Value != 1)
            {
                var pose = CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwChara/pose/items/inpNo").GetComponent<InputField>();
                pose.text = DefaultPose.Value.ToString();
                pose.onEndEdit.Invoke(DefaultPose.Value.ToString());
            }

            //Eyebrow pattern
            if (DefaultEyebrowPattern.Value != 1)
            {
                var eyebrow = CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwChara/eyebrow/items/inpNo").GetComponent<InputField>();
                eyebrow.text = DefaultEyebrowPattern.Value.ToString();
                eyebrow.onEndEdit.Invoke(DefaultEyebrowPattern.Value.ToString());
            }

            //Eye pattern
            if (DefaultEyePattern.Value != 1)
            {
                var eye = CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwChara/eye/items/inpNo").GetComponent<InputField>();
                eye.text = DefaultEyePattern.Value.ToString();
                eye.onEndEdit.Invoke(DefaultEyePattern.Value.ToString());
            }

            //Mouth pattern
            if (DefaultMouthPattern.Value != 1)
            {
                var mouth = CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwChara/mouth/items/inpNo").GetComponent<InputField>();
                mouth.text = DefaultMouthPattern.Value.ToString();
                mouth.onEndEdit.Invoke(DefaultMouthPattern.Value.ToString());
            }

            //Pause
            if (DefaultPause.Value)
                CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwChara/play/tglPlay").GetComponent<UI_ToggleOnOffEx>().isOn = false;

            //Clothing State
            switch (DefaultClothingState.Value)
            {
                case ClothingState.Clothed:
                    CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwCoorde/clothes/items/tgl01").GetComponent<Toggle>().isOn = true;
                    break;
                case ClothingState.Underwear:
                    CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwCoorde/clothes/items/tgl02").GetComponent<Toggle>().isOn = true;
                    break;
                case ClothingState.Naked:
                    CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwCoorde/clothes/items/tgl03").GetComponent<Toggle>().isOn = true;
                    break;
            }

            //Background
            if (DefaultBackground.Value != Background.Color)
                CustomBase.Instance.transform.Find("CanvasDraw/DrawWindow/dwBG/type/items/tgl02").GetComponent<Toggle>().isOn = true;
        }

        public enum GazeDirection { Camera, Forward }
        public enum HeadDirection { Pose, Forward }
        public enum ClothingState { Automatic, Clothed, Underwear, Naked }
        public enum Background { Image, Color }
    }
}