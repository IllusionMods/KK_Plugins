using HarmonyLib;
using Studio;

namespace KK_Plugins.MaterialEditor
{
    internal static class StudioHooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.OnDelete))]
        private static void OCIItemOnDelete(OCIItem __instance)
        {
            var controller = MEStudio.GetSceneController();
            if (controller != null)
                controller.ItemDeleteEvent(__instance.objectInfo.dicKey);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.visible), MethodType.Setter)]
        private static void OCIItemVisible(OCIItem __instance, bool value)
        {
            var controller = MEStudio.GetSceneController();
            if (controller != null)
                controller.ItemVisibleEvent(__instance.objectInfo.dicKey, value);
        }
    }
}
