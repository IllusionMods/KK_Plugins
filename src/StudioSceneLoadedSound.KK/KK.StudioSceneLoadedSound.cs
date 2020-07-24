using Illusion.Game;

namespace KK_Plugins
{
    public partial class StudioSceneLoadedSound
    {
        private static void PlayAlertSound() => Utils.Sound.Play(SystemSE.result_single);
    }
}