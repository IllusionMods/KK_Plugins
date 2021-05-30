using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    public class EyeControlCharaController : CharaCustomFunctionController
    {
        private float _EyeOpenMax;
        /// <summary>
        /// Get or set the eye open max value which will be saved and loaded with the card.
        /// Also applies the change to the character, except in Studio where this value is only applied to the character once upon adding it to the scene.
        /// </summary>
        public float EyeOpenMax
        {
            get => _EyeOpenMax;
            set
            {
                _EyeOpenMax = value;
#if KK || EC || KKS
                if (MakerAPI.InsideAndLoaded)
                    ChaControl.ChangeEyesOpenMax(ChaCustom.CustomBase.Instance.customCtrl.cmpDrawCtrl.sldEyesOpen.value);
                else
#endif
                //Do not apply the change to the character in Studio. It is only applied once when the character is added to a scene.
                if (EyeControl.InsideStudio)
                { }
                else
                    ChaControl.ChangeEyesOpenMax(value);

                if (MakerAPI.InsideAndLoaded)
                    EyeControl.EyeOpenMaxSlider.SetValue(value);
            }
        }

        private bool _DisableBlinking;
        /// <summary>
        /// Get or set the disable blinking value which will be saved and loaded with the card.
        /// Also applies the change to the character, except in Studio where this value is only applied to the character once upon adding it to the scene.
        /// </summary>
        public bool DisableBlinking
        {
            get => _DisableBlinking;
            set
            {
                _DisableBlinking = value;
#if KK || EC || KKS
                if (MakerAPI.InsideAndLoaded)
                    ChaControl.ChangeEyesBlinkFlag(!ChaCustom.CustomBase.Instance.customCtrl.cmpDrawCtrl.tglBlink.isOn);
                else
#endif
                //Do not apply the change to the character in Studio. It is only applied once when the character is added to a scene.
                if (EyeControl.InsideStudio)
                { }
                else
                    ChaControl.ChangeEyesBlinkFlag(!value);

                if (MakerAPI.InsideAndLoaded)
                    EyeControl.DisableBlinkingToggle.SetValue(value);
            }
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            if (EyeOpenMax == 1f && DisableBlinking == false)
            {
                SetExtendedData(null);
            }
            else
            {
                var data = new PluginData();
                data.data.Add("EyeOpenMax", EyeOpenMax);
                data.data.Add("DisableBlinking", DisableBlinking);
                SetExtendedData(data);
            }
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            LoadExtendedSaveData();
            base.OnReload(currentGameMode, maintainState);
        }

        /// <summary>
        /// In studio, saved data is only applied upon adding a character to a scene.
        /// </summary>
        internal void OnCharacterAddedToScene()
        {
            LoadExtendedSaveData();
            ChaControl.ChangeEyesOpenMax(EyeOpenMax);
            if (DisableBlinking)
                ChaControl.ChangeEyesBlinkFlag(!DisableBlinking);
        }

        private void LoadExtendedSaveData()
        {
            EyeOpenMax = 1f;
            DisableBlinking = false;

            var data = GetExtendedData();
            if (data != null)
            {
                if (data.data.TryGetValue("EyeOpenMax", out var loadedEyeOpenMax))
                    EyeOpenMax = (float)loadedEyeOpenMax;
                if (data.data.TryGetValue("DisableBlinking", out var loadedDisableBlinking))
                    DisableBlinking = (bool)loadedDisableBlinking;
            }
        }
    }
}
