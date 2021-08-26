using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_Plugins
{
    /// <summary>
    /// Random stuff
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class TestPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.testplugin";
        public const string PluginName = "Test Plugin";
        public const string PluginNameInternal = "KK_TestPlugin";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        internal void Main()
        {
            Logger = base.Logger;

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene s, LoadSceneMode lsm)
        {
            foreach (var light in FindObjectsOfType<Light>())
            {
                light.shadowCustomResolution = 10000;
                light.shadowBias = 0.0075f;
                if (light.name == "Directional Chara") //Studio shadow strength is different from main game for some reason
                    light.shadowStrength = 1;
            }
        }
    }
}