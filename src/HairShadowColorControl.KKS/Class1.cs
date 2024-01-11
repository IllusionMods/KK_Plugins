using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using ChaCustom;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UniRx;
using UnityEngine;

namespace HairShadowColorControl
{
    [BepInPlugin(GUID, DisplayName, Version)]
    public class HairShadowColorControlPlugin : BaseUnityPlugin
    {
        private MakerColor[] _customControls;
        public const string GUID = "HairShadowColorControl";
        public const string Version = "1.0";
        internal const string DisplayName = "HairShadowColorControl";

        private const string ShadowColorPropertyName = "_ShadowColor";

        private void Awake()
        {
            MakerAPI.MakerStartedLoading += MakerLoading;
            MakerAPI.ReloadCustomInterface += MakerRefresh;

            //CharacterApi.CharacterReloaded += CharacterReloaded;

            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
        }

        //private void CharacterReloaded(object sender, CharaReloadEventArgs e)
        //{
        //    //todo doesn't wrok because too early, hook hair update instead
        //    //todo find out what the default is? not necessary probably
        //    if (ExtDataTryGetShadowColor(e.ReloadedCharacter.fileHair.parts[0], out var color))
        //        SetShadowColor(e.ReloadedCharacter, color);
        //}

        private void MakerRefresh(object sender, EventArgs e)
        {
            // todo 
            // handle different hair types and selected hair screen
            // stop storing when ui updates
            // respect all hair toggle
            // see if material prop exists, set visibility
            // set control default as the color that was loaded?

            var defaultColor = GetDefaultColor();
            Console.WriteLine(defaultColor);

            for (var hairPart = 0; hairPart < _customControls.Length; hairPart++)
            {
                var chaControl = MakerAPI.GetCharacterControl();
                var parts = chaControl.fileHair.parts;
                var customControl = _customControls[hairPart];
                if (ExtDataTryGetShadowColor(parts[hairPart], out var storedColor))
                    customControl.SetValue(storedColor, false);
                else
                    customControl.SetValue(defaultColor, false); //todo find out what the default is? probably can just
            }
        }

        private void MakerLoading(object sender, RegisterCustomControlsEvent args)
        {
            var makerBase = MakerAPI.GetMakerBase();

            // Lines up with how hair parts are indexed
            var hairKinds = new[] { MakerConstants.Hair.Back, MakerConstants.Hair.Front, MakerConstants.Hair.Side, MakerConstants.Hair.Extension };
            _customControls = new MakerColor[hairKinds.Length];
            for (var hairKind = 0; hairKind < hairKinds.Length; hairKind++)
            {
                var hairType = (CvsHair.HairType)hairKind;
                var makerCategory = hairKinds[hairKind];
                var control = args.AddControl(new MakerColor("Hair shadow color", false, makerCategory, Color.magenta, this)); //todo correct default color?
                control.ValueChanged.Subscribe(color =>
                {
                    if (!MakerAPI.InsideAndLoaded) return;

                    var chaCtrl = MakerAPI.GetCharacterControl();

                    if (makerBase.customSettingSave.hairSameSetting)
                    {
                        for (var i = 0; i < _customControls.Length; i++)
                        {
                            ApplySlider((CvsHair.HairType)i);
                            _customControls[i].SetValue(color, false);
                        }
                    }
                    else
                    {
                        ApplySlider(hairType);
                    }

                    void ApplySlider(CvsHair.HairType targetHairType)
                    {
                        var hairPart = chaCtrl.fileHair.parts[(int)targetHairType];

                        ExtDataSetShadowColor(hairPart, color);

                        SetShadowColor(chaCtrl, color, targetHairType);
                    }
                });
                _customControls[hairKind] = control;
            }
        }

        private static Color GetDefaultColor()
        {
            var chara = MakerAPI.GetCharacterControl();
            var defaultColor = Enumerable.Range(0, 4)
                                         .Select(chara.GetCustomHairComponent)
                                         .Where(x => x != null && x.rendHair != null)
                                         .SelectMany(x => x.rendHair)
                                         .Where(x => x != null && x.material != null && x.material.HasProperty(ShadowColorPropertyName))
                                         .Select(x => x.material.GetColor(ShadowColorPropertyName))
                                         .FirstOrDefault();
            return defaultColor;
        }

        private static void SetShadowColor(ChaControl chaCtrl, Color color, CvsHair.HairType kind)
        {
            var rend = chaCtrl.GetCustomHairComponent((int)kind);
            if (rend != null && rend.rendHair != null)
            {
                foreach (var r in rend.rendHair)
                    r.material.SetColor(ShadowColorPropertyName, color);
            }
        }

        private static void ExtDataSetShadowColor(ChaFileHair.PartsInfo hairPart, Color color)
        {
            hairPart.SetExtendedDataById(GUID, new PluginData { version = 1, data = new Dictionary<string, object> { { ShadowColorPropertyName, '#' + ColorUtility.ToHtmlStringRGBA(color) } } });
        }

        private static bool ExtDataTryGetShadowColor(ChaFileHair.PartsInfo hairPart, out Color storedColor)
        {
            storedColor = Color.magenta;
            return hairPart.TryGetExtendedDataById(GUID, out var data) &&
                   data.data.TryGetValue(ShadowColorPropertyName, out var color) &&
                   ColorUtility.TryParseHtmlString(color as string, out storedColor);
        }

        private static class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
            [HarmonyWrapSafe]
            private static void ChaFileHair_LoadFile_Postfix(ChaControl __instance, int parts)
            {
                if (ExtDataTryGetShadowColor(__instance.fileHair.parts[parts], out var color))
                    SetShadowColor(__instance, color, (CvsHair.HairType)parts);
            }
        }
    }
}
