using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace KK_Plugins.MaterialEditor
{
    internal static partial class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
        private static void ChangeCoordinateTypePrefix(ChaControl __instance)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller != null)
                controller.CoordinateChangeEvent();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CvsClothesCopy), "CopyClothes")]
        private static void CopyClothesPostfix(TMP_Dropdown[] ___ddCoordeType, Toggle[] ___tglKind)
        {
            List<int> copySlots = new List<int>();
            for (int i = 0; i < Enum.GetNames(typeof(ChaFileDefine.ClothesKind)).Length; i++)
                if (___tglKind[i].isOn)
                    copySlots.Add(i);

            var controller = MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl());
            if (controller != null)
                controller.ClothingCopiedEvent(___ddCoordeType[1].value, ___ddCoordeType[0].value, copySlots);
        }
    }
}