using AIProject;
using BepInEx;
using HarmonyLib;
using System.Reflection;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    public partial class StudioSceneLoadedSound : BaseUnityPlugin
    {
        private static MethodInfo playSEMethodInfo = null;

        private static void PlayStudioSound(SoundPack.SystemSE sound)
        {
            if (playSEMethodInfo == null)
            {
                var studioUtility = typeof(Studio.Studio)?.Assembly?.GetType("Studio.Utility");
                if (studioUtility != null)
                    playSEMethodInfo = AccessTools.Method(studioUtility, "PlaySE");
            }

            playSEMethodInfo?.Invoke(null, new object[] { sound });
        }

        private static void PlayAlertSound() => PlayStudioSound(SoundPack.SystemSE.LevelUP);
    }
}