using BepInEx;
using Illusion.Game;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    public partial class StudioSceneLoadedSound : BaseUnityPlugin
    {
        private static void PlayAlertSound() => Utils.Sound.Play(SystemSE.result_single);
    }
}