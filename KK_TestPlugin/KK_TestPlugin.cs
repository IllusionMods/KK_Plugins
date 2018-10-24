using BepInEx;
using BepInEx.Logging;
using UnityEngine.SceneManagement;

/// <summary>
/// Displays the name of each scene in the log when it is loaded
/// </summary>
namespace KK_TestPlugin
{
    [BepInPlugin("com.deathweasel.bepinex.testplugin", "Test Plugin", "1.0")]
    public class KK_TestPlugin : BaseUnityPlugin
    {
        void Main()
        {
            SceneManager.sceneLoaded += (s, lsm) => Logger.Log(LogLevel.Warning, $"Scene loaded: {s.name}");
        }
    }
}