using HarmonyLib;
using Studio;
using System.Collections;
using UnityEngine;

namespace KK_Plugins.StudioCustomMasking
{
    internal static class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.OnTriggerEnter))]
        private static void OnTriggerEnter(Collider other) => StudioCustomMasking.SceneControllerInstance.ColliderEnterEvent(other);

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.OnTriggerStay))]
        private static void OnTriggerStay(Collider other) => StudioCustomMasking.SceneControllerInstance.ColliderStayEvent(other);

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.OnTriggerExit))]
        private static void OnTriggerExit(Collider other) => StudioCustomMasking.SceneControllerInstance.ColliderExitEvent(other);

        [HarmonyPostfix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.DeleteNodeLoop), typeof(TreeNodeObject))]
        private static void DeleteNode(TreeNodeObject _node) => StudioCustomMasking.SceneControllerInstance.ItemDeleteEvent(_node);

        /// <summary>
        /// When saving the scene, set a flag which will disable the drawing of collider lines so they aren't visible in the scene screenshot
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(SystemButtonCtrl), nameof(SystemButtonCtrl.OnClickSave))]
        private static void OnClickSave()
        {
            StudioCustomMasking.HideLines = true;

            StudioCustomMasking.Instance.StartCoroutine(Reset());
            IEnumerator Reset()
            {
                yield return null;
                StudioCustomMasking.HideLines = false;
            }
        }
    }
}
