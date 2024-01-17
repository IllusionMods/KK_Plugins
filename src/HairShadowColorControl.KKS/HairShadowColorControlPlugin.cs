using System;
using BepInEx;
using ChaCustom;
using HarmonyLib;
using KK_Plugins.MaterialEditor;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UniRx;
using UnityEngine;

namespace HairShadowColorControl
{
    [BepInPlugin(GUID, DisplayName, Version)]
    public class HairShadowColorControlPlugin : BaseUnityPlugin
    {
        public const string GUID = "HairShadowColorControl";
        public const string Version = "1.0";
        internal const string DisplayName = "HairShadowColorControl";

        private const string ShadowColorPropertyName = "ShadowColor";
        private static readonly Color _DefaultColor = new Color(0.83f, 0.86f, 0.94f);

        private static MakerColor[] _customControls;
        private static ChaControl _charaController;
        private static MaterialEditorCharaController _meController;

        private void Awake()
        {
            MakerAPI.MakerStartedLoading += MakerLoading;
            MakerAPI.ReloadCustomInterface += MakerRefresh;

            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
        }

        private void MakerLoading(object sender, RegisterCustomControlsEvent args)
        {
            var makerBase = MakerAPI.GetMakerBase();

            _charaController = MakerAPI.GetCharacterControl();
            _meController = _charaController.GetComponent<MaterialEditorCharaController>();

            // Lines up with how hair parts are indexed
            var hairKinds = new[] { MakerConstants.Hair.Back, MakerConstants.Hair.Front, MakerConstants.Hair.Side, MakerConstants.Hair.Extension };
            _customControls = new MakerColor[hairKinds.Length];
            for (var hairKind = 0; hairKind < hairKinds.Length; hairKind++)
            {
                var makerCategory = hairKinds[hairKind];
                var hairType = (CvsHair.HairType)hairKind;
                var control = args.AddControl(new MakerColor("Hair shadow color", false, makerCategory, _DefaultColor, this) { GroupingID = null });
                control.ValueChanged.Subscribe(color =>
                {
                    if (!MakerAPI.InsideAndLoaded) return;

                    var chaCtrl = MakerAPI.GetCharacterControl();

                    if (makerBase.customSettingSave.hairSameSetting)
                    {
                        for (var i = 0; i < _customControls.Length; i++)
                        {
                            SetShadowColor(chaCtrl, color, (CvsHair.HairType)i);
                            _customControls[i].SetValue(color, false);
                        }
                    }
                    else
                    {
                        SetShadowColor(chaCtrl, color, hairType);
                    }
                });
                _customControls[hairKind] = control;
            }
        }

        private static void SetShadowColor(ChaControl chaCtrl, Color color, CvsHair.HairType kind)
        {
            var me = chaCtrl.GetComponent<MaterialEditorCharaController>();

            var rend = chaCtrl.GetCustomHairComponent((int)kind);
            if (rend != null && rend.rendHair != null)
            {
                foreach (var r in rend.rendHair)
                    me.SetMaterialColorProperty((int)kind, MaterialEditorCharaController.ObjectType.Hair, r.material, ShadowColorPropertyName, color, rend.gameObject);
            }
        }

        private static void MakerRefresh(object sender, EventArgs e)
        {
            for (var hairPart = 0; hairPart < _customControls.Length; hairPart++)
                UpdateSliderValue(hairPart);
        }

        private static void UpdateSliderValue(int hairPart)
        {
            if (!MakerAPI.InsideMaker) return;

            // Figure out current color to show in the color control
            Color? setColor = null, originColor = null, currentColor = null;
            var any = false;
            var rendHair = _charaController.GetCustomHairComponent(hairPart)?.rendHair;
            if (rendHair != null)
            {
                foreach (var renderer in rendHair)
                {
                    if (renderer == null) continue;
                    any = true;

                    setColor = _meController.GetMaterialColorPropertyValue(hairPart, MaterialEditorCharaController.ObjectType.Hair, renderer.material, ShadowColorPropertyName, renderer.gameObject);
                    if (setColor.HasValue) break;

                    if (originColor.HasValue) continue;
                    originColor = _meController.GetMaterialColorPropertyValueOriginal(hairPart, MaterialEditorCharaController.ObjectType.Hair, renderer.material, ShadowColorPropertyName, renderer.gameObject);

                    if (currentColor.HasValue) continue;
                    currentColor = renderer.material.GetColor("_" + ShadowColorPropertyName);
                }
            }
            var color = setColor ?? originColor ?? currentColor ?? _DefaultColor;
            // Alpha is ignored by shaders and can be 0 making it invisible in the color picker
            color.a = 1;

            var customControl = _customControls[hairPart];
            customControl.SetValue(color, false);

            // If there are no renderers then the control should be hidden like base game color controls
            if (customControl.Visible.Value != any)
                customControl.Visible.OnNext(any);
        }

        private static class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsHair), nameof(CvsHair.UpdateSelectHair))]
            private static void UpdateHairUi(CvsHair __instance)
            {
                // Problem is that if user changes hair type then shadow color is lost
                // Update the slider to show the color of the newly loaded hair type
                UpdateSliderValue(__instance.hairType);

                // doesn't work, no obvious way to tell if this is a card load or user ui click (breaks card load)
                //var makerColor = _customControls[__instance.hairType];
                //var value = makerColor.Value;
                //makerColor.SetValue(Color.magenta, false);
                //makerColor.SetValue(value, true);
            }
        }
    }
}
