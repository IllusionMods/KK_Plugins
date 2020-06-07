using ChaCustom;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine.UI;
using KKAPI.Maker;
using System.Collections.Generic;

namespace KK_Plugins.MaterialEditor
{
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
        internal static void ChangeCoordinateTypePrefix(ChaControl __instance) => MaterialEditorPlugin.GetCharaController(__instance)?.CoordinateChangeEvent();

        [HarmonyPostfix, HarmonyPatch(typeof(CvsClothesCopy), "CopyClothes")]
        internal static void CopyClothesPostfix(TMP_Dropdown[] ___ddCoordeType, Toggle[] ___tglKind)
        {
            List<int> copySlots = new List<int>();
            for (int i = 0; i < Enum.GetNames(typeof(ChaFileDefine.ClothesKind)).Length; i++)
                if (___tglKind[i].isOn)
                    copySlots.Add(i);

            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl())?.ClothingCopiedEvent(___ddCoordeType[1].value, ___ddCoordeType[0].value, copySlots);
        }
    }
}