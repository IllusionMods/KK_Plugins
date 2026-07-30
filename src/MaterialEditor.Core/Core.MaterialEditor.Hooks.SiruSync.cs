#if KK || KKS
using HarmonyLib;
using System;

namespace KK_Plugins.MaterialEditor
{
    internal partial class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetSiruFlags), typeof(ChaFileDefine.SiruParts), typeof(byte))]
        private static void ChaControl_SetSiruFlags_Postfix(ChaControl __instance, ChaFileDefine.SiruParts __0, byte __1)
        {
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller == null)
                return;

            try
            {
                controller.QueueSiruWrite(__0, __1);
            }
            catch (Exception ex)
            {
                controller.CancelSiruUpdate();
                MaterialEditorPlugin.Logger.LogError("Failed to queue Material Editor siru synchronization: " + ex);
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateSiru), typeof(bool))]
        private static void ChaControl_UpdateSiru_Prefix(ChaControl __instance, out MaterialEditorCharaController.SiruUpdateSnapshot __state)
        {
            __state = null;
            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller == null)
                return;

            try
            {
                __state = controller.BeginSiruUpdate();
            }
            catch (Exception ex)
            {
                controller.CancelSiruUpdate();
                MaterialEditorPlugin.Logger.LogError("Failed to prepare Material Editor siru synchronization: " + ex);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateSiru), typeof(bool))]
        private static void ChaControl_UpdateSiru_Postfix(ChaControl __instance, MaterialEditorCharaController.SiruUpdateSnapshot __state)
        {
            if (__state == null)
                return;

            var controller = MaterialEditorPlugin.GetCharaController(__instance);
            if (controller == null)
                return;

            try
            {
                controller.CompleteSiruUpdate(__state);
            }
            catch (Exception ex)
            {
                controller.CancelSiruUpdate();
                MaterialEditorPlugin.Logger.LogError("Failed to apply Material Editor siru synchronization: " + ex);
            }
        }
    }
}
#endif
