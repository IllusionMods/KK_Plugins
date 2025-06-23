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
        internal static MakerSlider EyeOpenMinSlider;
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
            CharacterApi.RegisterExtraBehaviour<EyeControlCharaController>(GUID);
            Hooks.ApplyHooks();
        }
        
        /// <summary>
        /// Register the custom controls
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
#if KK || EC || KKS
            var category = MakerConstants.Face.Eye;
#else
            var category = MakerConstants.Face.Eyes;
#endif
            EyeOpenMaxSlider = e.AddControl(new MakerSlider(category, "Eye Open Max", 0, 1, 1, this));
            EyeOpenMaxSlider.BindToFunctionController<EyeControlCharaController, float>(c => c.EyeOpenMax, (c, v) => c.EyeOpenMax = v);
            EyeOpenMinSlider = e.AddControl(new MakerSlider(category, "Eye Open Min", 0, 1, 0, this));
            EyeOpenMinSlider.BindToFunctionController<EyeControlCharaController, float>(c => c.EyeOpenMin, (c, v) => c.EyeOpenMin = v);
            DisableBlinkingToggle = e.AddControl(new MakerToggle(category, "Disable Character Blinking", this));
            DisableBlinkingToggle.BindToFunctionController<EyeControlCharaController, bool>(c => c.DisableBlinking, (c, v) => c.DisableBlinking = v);
        }

        /// <summary>
        /// Get the EyeControlCharaController associated with the ChaControl
        /// </summary>
        /// <param name="chaControl"></param>
        /// <returns></returns>
        public static EyeControlCharaController GetCharaController(ChaControl chaControl) => chaControl == null ? null : chaControl.gameObject.GetComponent<EyeControlCharaController>();
    }
}
