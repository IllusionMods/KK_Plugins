using HarmonyLib;
using MaterialEditor;
using Studio;

namespace KK_Plugins.MaterialEditorWrapper
{
    internal static class StudioHooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.OnDelete))]
        private static void OCIItem_OnDelete(OCIItem __instance)
        {
            var controller = MEStudio.GetSceneController();
            if (controller != null)
                controller.ItemDeleteEvent(__instance.objectInfo.dicKey);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.visible), MethodType.Setter)]
        private static void OCIItem_Visible(OCIItem __instance, bool value)
        {
            var controller = MEStudio.GetSceneController();
            if (controller != null)
                controller.ItemVisibleEvent(__instance.objectInfo.dicKey, value);
        }

        /// <summary>
        /// Refresh the UI when changing selected item
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(WorkspaceCtrl), nameof(WorkspaceCtrl.OnSelectSingle))]
        private static void WorkspaceCtrl_OnSelectSingle()
        {
            if (!MaterialEditorUI.Visible)
                return;

            var controller = MEStudio.GetSceneController();
            if (controller == null)
                return;

            MEStudio.Instance.UpdateUI();
        }
    }
}
