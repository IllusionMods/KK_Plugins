using BepInEx;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UniRx;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class EyeControl : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.eyecontrol";
        public const string PluginName = "Eye Control";
        public const string PluginNameInternal = Constants.Prefix + "_EyeControl";
        public const string Version = "1.0.1";
        internal static new ManualLogSource Logger;

        internal static MakerSlider EyeOpenMaxSlider;
        internal static MakerToggle DisableBlinkingToggle;

#if EC
        internal static bool InsideStudio = false;
#else
        internal static bool InsideStudio = KKAPI.Studio.StudioAPI.InsideStudio;
#endif

        internal void Main()
        {
            Logger = base.Logger;

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
            CharacterApi.RegisterExtraBehaviour<EyeControlCharaController>(GUID);
            Hooks.ApplyHooks();
        }

        /// <summary>
        /// Set the values based on the loaded character once the character maker finishes loading
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MakerAPI_MakerFinishedLoading(object sender, System.EventArgs e)
        {
            var controller = GetCharaController(MakerAPI.GetCharacterControl());
            EyeOpenMaxSlider.SetValue(controller.EyeOpenMax);
            DisableBlinkingToggle.SetValue(controller.DisableBlinking);
        }

        /// <summary>
        /// Register the custom controls
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
#if KK || EC
            var category = MakerConstants.Face.Eye;
#else
            var category = MakerConstants.Face.Eyes;
#endif
            EyeOpenMaxSlider = e.AddControl(new MakerSlider(category, "Eye Open Max", 0, 1, 1, this));
            EyeOpenMaxSlider.ValueChanged.Subscribe(value => GetCharaController(MakerAPI.GetCharacterControl()).EyeOpenMax = value);
            DisableBlinkingToggle = e.AddControl(new MakerToggle(category, "Disable Character Blinking", this));
            DisableBlinkingToggle.ValueChanged.Subscribe(value => GetCharaController(MakerAPI.GetCharacterControl()).DisableBlinking = value);
        }

        /// <summary>
        /// Get the EyeControlCharaController associated with the ChaControl
        /// </summary>
        /// <param name="chaControl"></param>
        /// <returns></returns>
        public static EyeControlCharaController GetCharaController(ChaControl chaControl) => chaControl == null ? null : chaControl.gameObject.GetComponent<EyeControlCharaController>();
    }
}