using HarmonyLib;

namespace KK_Plugins
{
    public partial class Subtitles
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(LoadAudioBase), "Play")]
            internal static void PlayVoice(LoadAudioBase __instance)
            {
                if (__instance.audioSource == null || __instance.audioSource.clip == null || __instance.audioSource.loop)
                    return;

                if (VRHSceneInstance != null)
                    Caption.DisplayHSubtitle(__instance);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(HVoiceCtrl), "Init")]
            internal static void HVoiceCtrlInit() => VRHSceneInstance = FindObjectOfType<VRHScene>();
        }
    }
}