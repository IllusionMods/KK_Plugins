using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChaCustom;
using KKAPI;
using KKAPI.Maker;
using System;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class MakerDefaults : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.makerdefaults";
        public const string PluginName = "Maker Defaults";
        public const string PluginNameInternal = Constants.Prefix + "_MakerDefaults";
        public const string Version = "1.0.1";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<ClothingState> DefaultClothingState { get; private set; }
        public static ConfigEntry<EyebrowPattern> DefaultEyebrowPattern { get; private set; }
        public static ConfigEntry<EyePattern> DefaultEyePattern { get; private set; }
        public static ConfigEntry<float> DefaultEyeOpenness { get; private set; }
        public static ConfigEntry<bool> DefaultDisableBlinking { get; private set; }
        public static ConfigEntry<MouthPattern> DefaultMouthPattern { get; private set; }
        public static ConfigEntry<float> DefaultMouthOpenness { get; private set; }
        public static ConfigEntry<GazeDirection> DefaultGazeDirection { get; private set; }
        public static ConfigEntry<float> DefaultGazeDirectionRate { get; private set; }
        public static ConfigEntry<HeadDirection> DefaultHeadDirection { get; private set; }
        public static ConfigEntry<float> DefaultHeadDirectionRate { get; private set; }
        public static ConfigEntry<Background> DefaultBackground { get; private set; }
        public static ConfigEntry<int> DefaultPose { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;
            MakerAPI.MakerFinishedLoading += MakerFinishedLoading;
            DefaultClothingState = Config.Bind("Settings", "Default Clothing State", ClothingState.Automatic, new ConfigDescription("Default clothing state", null, new ConfigurationManagerAttributes { Order = 13 }));
            DefaultEyebrowPattern = Config.Bind("Settings", "Default Eyebrow Pattern", EyebrowPattern.Default, new ConfigDescription("Default eyebrow pattern", null, new ConfigurationManagerAttributes { Order = 12 }));
            DefaultEyePattern = Config.Bind("Settings", "Default Eye Pattern", EyePattern.Default, new ConfigDescription("Default eye pattern", null, new ConfigurationManagerAttributes { Order = 11 }));
            DefaultEyeOpenness = Config.Bind("Settings", "Default Eye Openness", 1f, new ConfigDescription("Default eye openness", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 10 }));
            DefaultDisableBlinking = Config.Bind("Settings", "Default Disable Blinking", false, new ConfigDescription("Default disable blinking state", null, new ConfigurationManagerAttributes { Order = 9 }));
            DefaultMouthPattern = Config.Bind("Settings", "Default Mouth Pattern", MouthPattern.Default, new ConfigDescription("Default mouth pattern", null, new ConfigurationManagerAttributes { Order = 8 }));
            DefaultMouthOpenness = Config.Bind("Settings", "Default Mouth Openness", 0f, new ConfigDescription("Default mouth openness", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 7 }));
            DefaultGazeDirection = Config.Bind("Settings", "Default Gaze Direction", GazeDirection.AtCamera, new ConfigDescription("Default gaze direction", null, new ConfigurationManagerAttributes { Order = 6 }));
            DefaultGazeDirectionRate = Config.Bind("Settings", "Default Gaze Direction Rate", 1f, new ConfigDescription("Default gaze direction rate", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 5 }));
            DefaultHeadDirection = Config.Bind("Settings", "Default Head Direction", HeadDirection.FromAnimation, new ConfigDescription("Default head direction", null, new ConfigurationManagerAttributes { Order = 4 }));
            DefaultHeadDirectionRate = Config.Bind("Settings", "Default Head Direction Rate", 1f, new ConfigDescription("Default head direction", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 3 }));
            DefaultPose = Config.Bind("Settings", "Default Pose", 0, new ConfigDescription("Default pose, number corresponds to the index of the pose in the dropdown list", new AcceptableValueRange<int>(0, 1000), new ConfigurationManagerAttributes { Order = 2 }));
            DefaultBackground = Config.Bind("Settings", "Default Background", Background.Image, new ConfigDescription("Default background type", null, new ConfigurationManagerAttributes { Order = 1 }));
        }

        private static void MakerFinishedLoading(object sender, EventArgs e)
        {
            //Clothing state
            if (DefaultClothingState.Value != ClothingState.Automatic)
            {
                switch (DefaultClothingState.Value)
                {
                    case ClothingState.Clothed:
                        CustomBase.Instance.customCtrl.cmpDrawCtrl.tglClothesState[1].isOn = true;
                        break;
                    case ClothingState.Underwear:
                        CustomBase.Instance.customCtrl.cmpDrawCtrl.tglClothesState[2].isOn = true;
                        break;
#if KK || KKS
                    case ClothingState.Naked:
                        CustomBase.Instance.customCtrl.cmpDrawCtrl.tglClothesState[3].isOn = true;
                        break;
#elif EC
                    case ClothingState.HalfOff:
                        CustomBase.Instance.customCtrl.cmpDrawCtrl.tglClothesState[3].isOn = true;
                        break;
                    case ClothingState.Naked:
                        CustomBase.Instance.customCtrl.cmpDrawCtrl.tglClothesState[4].isOn = true;
                        break;
#endif
                }
            }

            //Set eyebrow pattern. 0=Default, 1=Angry, etc.
            if (DefaultEyebrowPattern.Value != EyebrowPattern.Default)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.ddEyesPtn.value = (int)DefaultEyebrowPattern.Value;

            //Set eye pattern. 0=Default, 1=Closed, etc.
            if (DefaultEyePattern.Value != EyePattern.Default)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.ddEyesPtn.value = (int)DefaultEyePattern.Value;

            //Eye openness
            if (DefaultEyeOpenness.Value != 1f)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.sldEyesOpen.value = DefaultEyeOpenness.Value;

            //Disable blinking
            if (DefaultDisableBlinking.Value)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.tglBlink.isOn = DefaultDisableBlinking.Value;

            //Set mouth pattern. 0=Default, 1=Smile, etc.
            if (DefaultMouthPattern.Value != MouthPattern.Default)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.ddMouthPtn.value = (int)DefaultMouthPattern.Value;

            //Mouth open
            if (DefaultMouthOpenness.Value != 0f)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.sldMouthOpen.value = DefaultMouthOpenness.Value;

            //Gaze Direction
            if (DefaultGazeDirection.Value != GazeDirection.AtCamera)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.ddEyesLook.value = (int)DefaultGazeDirection.Value;

            if (DefaultGazeDirectionRate.Value != 1f)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.sldEyesLookRate.value = DefaultGazeDirectionRate.Value;

            //Head Direction
            if (DefaultHeadDirection.Value != HeadDirection.FromAnimation)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.ddNeckLook.value = (int)DefaultHeadDirection.Value;

            if (DefaultHeadDirectionRate.Value != 1f)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.sldNeckLookRate.value = DefaultHeadDirectionRate.Value;

            //Pose
            if (DefaultPose.Value != 0)
            {
                if (DefaultPose.Value < CustomBase.Instance.customCtrl.cmpDrawCtrl.ddPose.options.Count)
                    CustomBase.Instance.customCtrl.cmpDrawCtrl.ddPose.value = DefaultPose.Value;
            }

            //Background
            if (DefaultBackground.Value != Background.Image)
                CustomBase.Instance.customCtrl.cmpDrawCtrl.tglBackType[1].isOn = true;
        }

#if KK || KKS
        public enum ClothingState { Automatic, Clothed, Underwear, Naked }
#elif EC
        public enum ClothingState { Automatic, Clothed, Underwear, HalfOff, Naked }
#endif
        public enum EyebrowPattern { Default, Angry, Worried, Bored, DoubtL, DoubtR, ThinkingL, ThinkingR, FuriousL, FuriousR, Serious, Anxious, Surprised, Disappointed, Smug, WinkingL, WinkingR }
        public enum EyePattern { Default, Closed, Smiling, HappyClosed, Happy, WinkingL, WinkingR, Pained, Bashful, Angry, Serious, Bored, Awkward, Hate, Thinking, Sad, Crying, Impatient, Disappointed, Worried, Smug, CircleEyes1, CircleEyes2, SpiralEyes, StarPupils, HeartPupils, FieryEyes, CartoonyWink, VerticalLine, CartoonyClosed, HorizontalLine, CartoonyCrying }
        public enum MouthPattern { Default, Smiling, HappyBroad, HappyModerate, HappySlight, ExcitedBroad, ExcitedModerate, ExcitedSlight, Angry1, Angry2, Serious1, Serious2, Hate, Lonely, Impatient, Dissatisfied, Amazed, Surprised, SurprisedModerate, Smug, Playful, Eating, HoldInMouth, Kiss, TongueOut, SmallA, BigA, SmallI, BigI, SmallU, BigU, SmallE, BigE, SmallO, BigO, SmallN, BigN, Catlike, Triangle, CartoonySmile }
        public enum GazeDirection { AtCamera, Upward, RightAndUp, Right, DownAndRight, Downward, DownAndLeft, Left, UpAndLeft, AvertGaze }
        public enum HeadDirection { FromAnimation, AtCamera, Upward, RightAndUp, Right, DownAndRight, Downward, DownAndLeft, Left, UpAndLeft, AvertGaze }
        public enum Background { Image, Color }
    }
}