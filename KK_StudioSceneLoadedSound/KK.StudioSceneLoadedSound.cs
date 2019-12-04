using BepInEx;
using Illusion.Game;

namespace KK_Plugins
{
    /// <summary>
    /// When a Studio scene is loaded or imported, play a sound
    /// </summary>
    public partial class StudioSceneLoadedSound : BaseUnityPlugin {
        private static void PlayAlertSound()
        {
            Utils.Sound.Play(SystemSE.result_single);
        }
    }
}