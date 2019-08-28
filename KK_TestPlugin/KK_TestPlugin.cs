using BepInEx;
using BepInEx.Logging;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    /// <summary>
    /// Displays the name of each scene in the log when it is loaded
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_TestPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.testplugin";
        public const string PluginName = "Test Plugin";
        public const string Version = "1.0";

        private void Main() => SceneManager.sceneLoaded += (s, lsm) => Logger.Log(LogLevel.Warning, $"Scene loaded: {s.name}");
    }
}