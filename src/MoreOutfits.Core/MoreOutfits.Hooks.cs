using ChaCustom;
using HarmonyLib;
using MessagePack;
using Studio;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace KK_Plugins.MoreOutfits
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
            int coordinateCount = list.Count;

            //Ensure the card doesn't have too few coordinates (typically happens converting a KKS card to KK)
#if KK
            if (coordinateCount < 7)
                coordinateCount = 7;
#elif KKS
            if (coordinateCount < 4)
                coordinateCount = 4;
#endif

            //Reinitialize the array with the new length
            __instance.coordinate = new ChaFileCoordinate[coordinateCount];
            for (int i = 0; i < coordinateCount; i++)
                __instance.coordinate[i] = new ChaFileCoordinate();

            //Load all the coordinates
            for (int i = 0; i < list.Count; i++)
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
            StudioUI.InitializeStudioUI(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), nameof(HSceneProc.Start))]
        private static void HSceneProc_MapSameObjectDisable()
        {
            HSceneUI.HSceneUIInitialized = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HSprite), nameof(HSprite.OnMainMenu))]
        private static void HSprite_OnMainMenu()
        {
            HSceneUI.InitializeHSceneUI();
        }

#if KKS
        private static bool DoingImport;

        [HarmonyPrefix, HarmonyPatch(typeof(ConvertChaFileScene), nameof(ConvertChaFileScene.Start))]
        private static void ConvertChaFileSceneStart() => DoingImport = true;

        [HarmonyPostfix, HarmonyPatch(typeof(ConvertChaFileScene), nameof(ConvertChaFileScene.OnDestroy))]
        private static void ConvertChaFileSceneEnd() => DoingImport = false;

        /// <summary>
        /// Don't allow outfits to be replaced by defaults
        /// </summary>
        /// <returns></returns>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.AssignCoordinate))]
        private static bool ChaFile_AssignCoordinate()
        {
            if (DoingImport)
                return false;
            return true;
        }
#endif
    }


    // Manual runtime patch for VRHScene
    internal static class VRHScenePatcher
    {
        internal static void ApplyPatch(Harmony harmony)
        {
            Type vrhSceneType = Type.GetType("VRHScene, Assembly-CSharp");
            if (vrhSceneType == null)
            {
                Plugin.Logger.LogWarning("VRHScene type not found, skipping patch.");
                return;
            }

            MethodInfo startMethod = vrhSceneType.GetMethod("Start", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (startMethod == null)
            {
                Plugin.Logger.LogWarning("VRHScene.Start method not found, skipping patch.");
                return;
            }

            MethodInfo postfixMethod = typeof(VRHScenePatcher).GetMethod(nameof(VRHScene_MapSameObjectDisable), BindingFlags.Static | BindingFlags.NonPublic);
            if (postfixMethod == null)
            {
                Plugin.Logger.LogError("Failed to find VRHScene_MapSameObjectDisable method. Ensure it is private static.");
                return;
            }

            Plugin.Logger.LogDebug($"VRHScene Type: {vrhSceneType}");
            Plugin.Logger.LogDebug($"VRHScene Start Method: {startMethod}");
            harmony.Patch(startMethod, postfix: new HarmonyMethod(postfixMethod));
            Plugin.Logger.LogDebug("Successfully patched VRHScene.Start.");
        }

        // Ensure this method is private and static
        private static void VRHScene_MapSameObjectDisable(object __instance)
        {
            Plugin.Logger.LogDebug("Hooks > VRHScene MapSameObjectDisable");
            Plugin.Logger.LogDebug("HSceneUIInitialized -> false");
            HSceneUI.HSceneUIInitialized = false;
        }
    }
}
