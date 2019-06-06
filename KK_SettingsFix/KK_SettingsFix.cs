using BepInEx;
using Harmony;
using System.IO;
using System.Reflection;

namespace KK_SettingsFix
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_SettingsFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.settingsfix";
        public const string PluginName = "Settings Fix";
        public const string Version = "1.0";

        private static KK_SettingsFix instance;

        private void Awake()
        {
            //Create a setup.xml if there isn't one
            if (!File.Exists("UserData/setup.xml"))
                CreateSetupXML();

            instance = this;

            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_SettingsFix));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Config), "Start")]
        public static void ManagerConfigStart()
        {
            if (KKAPI.Studio.StudioAPI.InsideStudio)
            {
                var xmlr = typeof(InitScene).GetMethod("xmlRead", BindingFlags.NonPublic | BindingFlags.Instance);
                var initScene = instance.gameObject.AddComponent<InitScene>();
                xmlr.Invoke(initScene, null);
                DestroyImmediate(initScene);
            }
        }

        private void CreateSetupXML()
        {
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Info, "CreateSetupXML");
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KK_SettingsFix.Resources.setup.xml"))
            {
                using (FileStream fileStream = File.Create("UserData/setup.xml", (int)stream.Length))
                {
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                }
            }
        }
    }
}