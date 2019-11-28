using HarmonyLib;

namespace KK_Plugins
{
    public partial class StudioSceneLoadedSound
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickLoad")]
            internal static void OnClickLoadPrefix() => LoadOrImportClicked = true;

            [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneLoadScene), "OnClickImport")]
            internal static void OnClickImportPrefix() => LoadOrImportClicked = true;
        }
    }
}