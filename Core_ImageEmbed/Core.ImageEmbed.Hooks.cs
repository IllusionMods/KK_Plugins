using HarmonyLib;
using Studio;

namespace KK_Plugins
{
    public partial class ImageEmbed
    {
        internal static class Hooks
        {
            /// <summary>
            /// Save the pattern image to the scene data
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.SetPatternTex), typeof(int), typeof(int))]
            internal static void SetPatternTexHook(OCIItem __instance, int _idx)
            {
                if (__instance?.itemComponent == null) return;

                string file = __instance.GetPatternPath(_idx);

                if (SavePattern.Value && !file.IsNullOrEmpty())
                {
                    //Save the pattern if it is one that comes from the userdata/pattern folder via MaterialEditor
                    string filePath = UserData.Path + "pattern/" + file;

                    Logger.LogDebug($"Saving pattern to scene data.");
                    foreach (var rend in __instance.itemComponent.GetRenderers())
                        MaterialEditor.GetSceneController().AddMaterialTextureProperty(__instance.objectInfo.dicKey, rend.material.NameFormatted(), $"PatternMask{_idx + 1}", __instance.objectItem, filePath);
                    __instance.SetPatternPath(_idx, "");
                }
                else
                {
                    //Remove any MaterialEditor pattern texture edits when changing patterns
                    foreach (var rend in __instance.itemComponent.GetRenderers())
                        MaterialEditor.GetSceneController().RemoveMaterialTextureProperty(__instance.objectInfo.dicKey, rend.material.NameFormatted(), $"PatternMask{_idx + 1}", MaterialEditor.TexturePropertyType.Texture, false);
                }
            }

            /// <summary>
            /// Save the BG image to the scene data
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(OCIItem), nameof(OCIItem.SetMainTex), typeof(string))]
            internal static void SetMainTexHook(OCIItem __instance)
            {
                if (__instance?.panelComponent == null) return;

                string file = __instance.itemInfo.panel.filePath;

                if (SaveBG.Value && !file.IsNullOrEmpty() && !DefaultBGs.Contains(file.ToLower()))
                {
                    //Save the BG to the scene data via MaterialEditor
                    string filePath = UserData.Path + BackgroundList.dirName + "/" + file;

                    Logger.LogDebug($"Saving background image to scene data.");
                    foreach (var rend in __instance.panelComponent.renderer)
                        MaterialEditor.GetSceneController().AddMaterialTextureProperty(__instance.objectInfo.dicKey, rend.material.NameFormatted(), "MainTex", __instance.objectItem, filePath);
                    __instance.itemInfo.panel.filePath = "";
                }
                else
                {
                    //Remove any MaterialEditor MainTex texture edits
                    foreach (var rend in __instance.panelComponent.renderer)
                        MaterialEditor.GetSceneController().RemoveMaterialTextureProperty(__instance.objectInfo.dicKey, rend.material.NameFormatted(), "MainTex", false);
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(FrameCtrl), nameof(FrameCtrl.Load))]
            internal static void FrameCtrlLoadHook(string _file)
            {
                if (SaveFrame.Value && !_file.IsNullOrEmpty() && !DefaultFrames.Contains(_file.ToLower()))
                {
                    //Save the frame to the scene data
                    string filePath = UserData.Path + "frame/" + _file;

                    Logger.LogDebug($"Saving frame image to scene data.");
                    GetSceneController().SetFrameTex(filePath);
                }
                else
                {
                    GetSceneController().ClearFrameTex();
                }
            }
        }
    }
}