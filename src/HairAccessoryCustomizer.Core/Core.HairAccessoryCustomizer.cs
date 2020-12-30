using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    /// <summary>
    /// Individual customization of hair accessories for adding hair gloss, color matching, etc.
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HairAccessoryCustomizer : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.hairaccessorycustomizer";
        public const string PluginName = "Hair Accessory Customizer";
        public const string PluginNameInternal = Constants.Prefix + "_HairAccessoryCustomizer";
        public const string Version = "1.1.5";
        internal static new ManualLogSource Logger;

        internal static bool ReloadingChara = false;
        internal static AccessoryControlWrapper<MakerToggle, bool> ColorMatchToggle;
        internal static AccessoryControlWrapper<MakerToggle, bool> HairGlossToggle;
        internal static AccessoryControlWrapper<MakerColor, Color> OutlineColorPicker;
        internal static AccessoryControlWrapper<MakerColor, Color> AccessoryColorPicker;
        internal static AccessoryControlWrapper<MakerSlider, float> HairLengthSlider;
        private static readonly bool ColorMatchDefault = true;
        private static readonly bool HairGlossDefault = true;
        private static readonly Color OutlineColorDefault = Color.black;
        private static readonly Color AccessoryColorDefault = Color.red;
        private static readonly float HairLengthDefault = 0;

        private void Start()
        {
            Logger = base.Logger;
            CharacterApi.RegisterExtraBehaviour<HairAccessoryController>(GUID);

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
#if KK
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
#endif
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
        /// <summary>
        /// Hides the accessory color controls for the current slot
        /// </summary>
        internal static void HideAccColors()
        {
            if (!MakerAPI.InsideAndLoaded) return;

            var cvsAccessory = AccessoriesApi.GetMakerAccessoryPageObject(AccessoriesApi.SelectedMakerAccSlot).GetComponent<CvsAccessory>();
            Traverse.Create(cvsAccessory).Field("separateColor").GetValue<GameObject>().SetActive(false);
            Traverse.Create(cvsAccessory).Field("btnAcsColor01").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            Traverse.Create(cvsAccessory).Field("btnAcsColor02").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            Traverse.Create(cvsAccessory).Field("btnAcsColor03").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            Traverse.Create(cvsAccessory).Field("btnAcsColor04").GetValue<Button>().transform.parent.gameObject.SetActive(false);
            Traverse.Create(cvsAccessory).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(false);
        }
        /// <summary>
        /// Shows the accessory color controls for the current slot
        /// </summary>
        internal static void ShowAccColors(bool showButton)
        {
            if (!MakerAPI.InsideAndLoaded) return;
            CvsAccessory cvsAccessory = AccessoriesApi.GetMakerAccessoryPageObject(AccessoriesApi.SelectedMakerAccSlot).GetComponent<CvsAccessory>();
            cvsAccessory.ChangeUseColorVisible();
            Traverse.Create(cvsAccessory).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(showButton);
        }
        /// <summary>
        /// Sets up the visibility and values for the current slot
        /// </summary>
        internal static void InitCurrentSlot(HairAccessoryController controller, bool hairAccessory)
        {
            if (!MakerAPI.InsideAndLoaded) return;

            if (hairAccessory)
            {
                ColorMatchToggle.SetSelectedValue(controller.GetColorMatch(), false);
                HairGlossToggle.SetSelectedValue(controller.GetHairGloss(), false);
                OutlineColorPicker.SetSelectedValue(controller.GetOutlineColor(), false);
                AccessoryColorPicker.SetSelectedValue(controller.GetAccessoryColor(), false);
                HairLengthSlider.SetSelectedValue(controller.GetHairLength(), false);

                ColorMatchToggle.Control.Visible.OnNext(true);
                HairGlossToggle.Control.Visible.OnNext(true);
                OutlineColorPicker.Control.Visible.OnNext(!controller.GetColorMatch());
                AccessoryColorPicker.Control.Visible.OnNext(controller.HasAccessoryPart());
                HairLengthSlider.Control.Visible.OnNext(controller.HasLengthTransforms());

                if (controller.GetColorMatch(AccessoriesApi.SelectedMakerAccSlot))
                    HideAccColors();
                else
                    ShowAccColors(true);
            }
            else
            {
                ColorMatchToggle.SetSelectedValue(ColorMatchDefault, false);
                HairGlossToggle.SetSelectedValue(HairGlossDefault, false);
                OutlineColorPicker.SetSelectedValue(OutlineColorDefault, false);
                AccessoryColorPicker.SetSelectedValue(AccessoryColorDefault, false);

                HairGlossToggle.Control.Visible.OnNext(false);
                ColorMatchToggle.Control.Visible.OnNext(false);
                OutlineColorPicker.Control.Visible.OnNext(false);
                AccessoryColorPicker.Control.Visible.OnNext(false);
                HairLengthSlider.Control.Visible.OnNext(false);
                ShowAccColors(controller.ChaControl.GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot) != null);
            }
        }
        internal static void InitCurrentSlot(HairAccessoryController controller) => InitCurrentSlot(controller, controller.IsHairAccessory(AccessoriesApi.SelectedMakerAccSlot));

        internal static HairAccessoryController GetController(ChaControl character) => character == null ? null : character.gameObject.GetComponent<HairAccessoryController>();
    }
}
