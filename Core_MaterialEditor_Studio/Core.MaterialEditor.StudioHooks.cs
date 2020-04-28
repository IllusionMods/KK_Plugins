using HarmonyLib;
using Studio;

namespace KK_Plugins
{
    public partial class MaterialEditor
    {
        internal partial class Hooks
        {
            /// <summary>
            /// Save the pattern image to the scene data
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.SetPatternTex), typeof(int), typeof(int))]
            internal static void SetPatternTexHook(OCIItem __instance, int _idx)
            {
                if (!SavePattern.Value) return;
                if (__instance?.itemComponent == null) return;
                if (__instance.GetPatternPath(_idx).IsNullOrEmpty()) return;

                Logger.LogDebug($"Saving pattern to scene data.");

                foreach (var rend in __instance.itemComponent.GetRenderers())
                    GetSceneController().AddMaterialTextureProperty(__instance.objectInfo.dicKey, rend.material.NameFormatted(), $"PatternMask{_idx + 1}", __instance.objectItem, UserData.Path + "pattern/" + __instance.GetPatternPath(_idx));
                __instance.SetPatternPath(_idx, "");
            }

            /// <summary>
            /// Save the BG image to the scene data
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.SetMainTex), typeof(string))]
            internal static void SetMainTexHook(OCIItem __instance, string _file)
            {
                if (!SaveBG.Value) return;
                if (__instance?.panelComponent == null) return;
                if (__instance.itemInfo.panel.filePath == "") return;

                Logger.LogDebug($"Saving background image to scene data. {_file}");

                foreach (var rend in __instance.panelComponent.renderer)
                    GetSceneController().AddMaterialTextureProperty(__instance.objectInfo.dicKey, rend.material.NameFormatted(), "MainTex", __instance.objectItem, UserData.Path + BackgroundList.dirName + "/" + _file);

                __instance.itemInfo.panel.filePath = "";
            }
        }
    }
}