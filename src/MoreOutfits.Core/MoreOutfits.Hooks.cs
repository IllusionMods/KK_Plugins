using ChaCustom;
using HarmonyLib;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;

namespace KK_Plugins
{
    public class Hooks
    {
        /// <summary>
        /// Ensure extra coordinates are loaded
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.SetCoordinateBytes))]
        private static bool SetCoordinateBytes(ChaFile __instance, byte[] data, Version ver)
        {
            List<byte[]> list = MessagePackSerializer.Deserialize<List<byte[]>>(data);

            //Reinitialize the array with the new length
            __instance.coordinate = new ChaFileCoordinate[list.Count];
            for (int i = 0; i < list.Count; i++)
                __instance.coordinate[i] = new ChaFileCoordinate();

            //Load all the coordinates
            for (int i = 0; i < __instance.coordinate.Length; i++)
                __instance.coordinate[i].LoadBytes(list[i], ver);

            return false;
        }

        /// <summary>
        /// Prevent index out of range exceptions
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPrefix, HarmonyPatch(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.ChangeDstDD))]
        private static void CvsAccessoryCopy_ChangeDstDD(CvsAccessoryCopy __instance)
        {
            if (__instance.ddCoordeType[0].value >= __instance.chaCtrl.chaFile.coordinate.Length)
                __instance.ddCoordeType[0].value = 0;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(CvsAccessoryCopy), nameof(CvsAccessoryCopy.ChangeSrcDD))]
        private static void CvsAccessoryCopy_ChangeSrcDD(CvsAccessoryCopy __instance)
        {
            if (__instance.ddCoordeType[1].value >= __instance.chaCtrl.chaFile.coordinate.Length)
                __instance.ddCoordeType[1].value = 0;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(CvsClothesCopy), nameof(CvsClothesCopy.ChangeDstDD))]
        private static void CvsClothesCopy_ChangeDstDD(CvsAccessoryCopy __instance)
        {
            if (__instance.ddCoordeType[0].value >= __instance.chaCtrl.chaFile.coordinate.Length)
                __instance.ddCoordeType[0].value = 0;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(CvsClothesCopy), nameof(CvsClothesCopy.ChangeSrcDD))]
        private static void CvsClothesCopy_ChangeSrcDD(CvsAccessoryCopy __instance)
        {
            if (__instance.ddCoordeType[1].value >= __instance.chaCtrl.chaFile.coordinate.Length)
                __instance.ddCoordeType[1].value = 0;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl), nameof(MPCharCtrl.OnClickRoot))]
        private static void MPCharCtrl_OnClickRoot(MPCharCtrl __instance)
        {
            MoreOutfits.InitializeStudioUI(__instance);
        }
    }
}
