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
                    SavePatternTex(__instance, _idx, filePath);
                }
                else
                {
                    //Remove any MaterialEditor pattern texture edits when changing patterns
                    foreach (var rend in __instance.itemComponent.GetRenderers())
                        MaterialEditor.MEStudio.GetSceneController().RemoveMaterialTexture(__instance.objectInfo.dicKey, rend.material, $"PatternMask{_idx + 1}", false);
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

                if (SaveBG.Value && !file.IsNullOrEmpty())
                {
                    //Save the BG to the scene data via MaterialEditor
                    string filePath = UserData.Path + BackgroundList.dirName + "/" + file;
                    SaveBGTex(__instance, filePath);
                }
                else
                {
                    //Remove any MaterialEditor MainTex texture edits
                    foreach (var rend in __instance.panelComponent.renderer)
                        MaterialEditor.MEStudio.GetSceneController().RemoveMaterialTexture(__instance.objectInfo.dicKey, rend.material, "MainTex", false);
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(FrameCtrl), nameof(FrameCtrl.Load))]
            internal static void FrameCtrlLoadHook(string _file)
            {
                if (SaveFrame.Value && !_file.IsNullOrEmpty() && !DefaultFrames.Contains(_file.ToLower()))
                {
                    //Save the frame to the scene data
                    string filePath = UserData.Path + "frame/" + _file;

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