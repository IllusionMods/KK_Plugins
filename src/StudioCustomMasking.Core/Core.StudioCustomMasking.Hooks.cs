using HarmonyLib;
using Studio;
using System.Collections;
using UnityEngine;

namespace KK_Plugins.StudioCustomMasking
{
    internal static class Hooks
    {
        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "OnTriggerEnter")]
        internal static void OnTriggerEnter(Collider other) => StudioCustomMasking.SceneControllerInstance.ColliderEnterEvent(other);

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "OnTriggerStay")]
        internal static void OnTriggerStay(Collider other) => StudioCustomMasking.SceneControllerInstance.ColliderStayEvent(other);

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.CameraControl), "OnTriggerExit")]
        internal static void OnTriggerExit(Collider other) => StudioCustomMasking.SceneControllerInstance.ColliderExitEvent(other);

        [HarmonyPostfix, HarmonyPatch(typeof(TreeNodeCtrl), "DeleteNodeLoop", typeof(TreeNodeObject))]
        internal static void DeleteNode(TreeNodeObject _node) => StudioCustomMasking.SceneControllerInstance.ItemDeleteEvent(_node);

        /// <summary>
        /// When saving the scene, set a flag which will disable the drawing of collider lines so they aren't visible in the scene screenshot
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(SystemButtonCtrl), nameof(SystemButtonCtrl.OnClickSave))]
        internal static void OnClickSave()
        {
            StudioCustomMasking.HideLines = true;

            StudioCustomMasking.Instance.StartCoroutine(Reset());
            IEnumerator Reset()
            {
                yield return null;
                StudioCustomMasking.HideLines = false;
            }
        }

        internal static void ScreencapHook()
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
