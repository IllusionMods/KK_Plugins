using BepInEx;
using Harmony;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HairAccessoryCustomizer
{
    /// <summary>
    /// Individual customization of hair accessories for adding hair gloss, color matching, etc.
    /// </summary>
    public partial class HairAccessoryCustomizer : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.hairaccessorycustomizer";
        public const string PluginName = "Hair Accessory Customizer";
        public const string Version = "1.1.1";

        internal static bool ReloadingChara = false;
        internal static AccessoryControlWrapper<MakerToggle, bool> ColorMatchToggle;
        internal static AccessoryControlWrapper<MakerToggle, bool> HairGlossToggle;
        internal static AccessoryControlWrapper<MakerColor, Color> OutlineColorPicker;
        internal static AccessoryControlWrapper<MakerColor, Color> AccessoryColorPicker;
        internal static AccessoryControlWrapper<MakerSlider, float> HairLengthSlider;
        private static readonly bool ColorMatchDefault = true;
        private static readonly bool HairGlossDefault = true;
        private static Color OutlineColorDefault = Color.black;
        private static Color AccessoryColorDefault = Color.red;
        private static readonly float HairLengthDefault = 0;

        private void Start()
        {
            CharacterApi.RegisterExtraBehaviour<HairAccessoryController>(GUID);

            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
#if KK
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;
#endif
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
        }
        /// <summary>
        /// Hides the accessory color controls for the current slot
        /// </summary>
        internal static void HideAccColors()
        {
            if (!MakerAPI.InsideAndLoaded) return;

            var cvsAccessory = AccessoriesApi.GetCvsAccessory(AccessoriesApi.SelectedMakerAccSlot);
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

            AccessoriesApi.GetCvsAccessory(AccessoriesApi.SelectedMakerAccSlot).ChangeUseColorVisible();
            Traverse.Create(AccessoriesApi.GetCvsAccessory(AccessoriesApi.SelectedMakerAccSlot)).Field("btnInitColor").GetValue<Button>().transform.parent.gameObject.SetActive(showButton);
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
                ShowAccColors(AccessoriesApi.GetAccessory(controller.ChaControl, AccessoriesApi.SelectedMakerAccSlot) != null);
            }
        }
        internal static void InitCurrentSlot(HairAccessoryController controller) => InitCurrentSlot(controller, controller.IsHairAccessory(AccessoriesApi.SelectedMakerAccSlot));

        internal static HairAccessoryController GetController(ChaControl character) => character?.gameObject?.GetComponent<HairAccessoryController>();
    }
}
