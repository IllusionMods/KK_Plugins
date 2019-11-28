using BepInEx;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    /// <summary>
    /// Displays the name of each scene in the log when it is loaded
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class TestPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.testplugin";
        public const string PluginName = "Test Plugin";
        public const string PluginNameInternal = "KK_TestPlugin";
        public const string Version = "1.0";

        internal void Main() => SceneManager.sceneLoaded += (s, lsm) => Logger.LogWarning($"Scene loaded: {s.name}");
    }
}