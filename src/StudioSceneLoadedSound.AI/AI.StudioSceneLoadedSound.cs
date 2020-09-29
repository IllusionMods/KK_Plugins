using AIProject;
using HarmonyLib;
using System.Reflection;

namespace KK_Plugins
{
    public partial class StudioSceneLoadedSound
    {
        private static MethodInfo playSEMethodInfo;

        private static void PlayStudioSound(SoundPack.SystemSE sound)
        {
            if (playSEMethodInfo == null)
            {
                var studioUtility = typeof(Studio.Studio).Assembly.GetType("Studio.Utility");
                if (studioUtility != null)
                    playSEMethodInfo = AccessTools.Method(studioUtility, "PlaySE");
            }

            playSEMethodInfo?.Invoke(null, new object[] { sound });
        }

        private static void PlayAlertSound() => PlayStudioSound(SoundPack.SystemSE.LevelUP);
    }
}