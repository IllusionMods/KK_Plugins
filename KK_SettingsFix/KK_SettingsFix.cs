using BepInEx;
using System.IO;
using System.Reflection;

namespace KK_SettingsFix
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess("CharaStudio")]
    public class KK_SettingsFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.settingsfix";
        public const string PluginName = "Settings Fix";
        public const string Version = "1.0";

        private void Start()
        {
            if (!File.Exists("UserData/setup.xml"))
                return;

            var xmlr = typeof(InitScene).GetMethod("xmlRead", BindingFlags.NonPublic | BindingFlags.Instance);
            var initScene = gameObject.AddComponent<InitScene>();
            xmlr.Invoke(initScene, null);
            DestroyImmediate(initScene);
        }
    }
}