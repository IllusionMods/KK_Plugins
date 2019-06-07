using BepInEx;
using Harmony;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace KK_SettingsFix
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_SettingsFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.settingsfix";
        public const string PluginName = "Settings Fix";
        public const string Version = "1.1";

        private static KK_SettingsFix instance;

        private void Awake()
        {
            //Test setup.xml for validity, delete if it has junk data
            if (File.Exists("UserData/setup.xml"))
                TestSetupXML();

            //Create a setup.xml if there isn't one
            if (!File.Exists("UserData/setup.xml"))
                CreateSetupXML();

            instance = this;

            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_SettingsFix));
        }
        /// <summary>
        /// Run the code for reading setup.xml when inside studio. Done in a Manager.Config.Start hook because the xmlRead method needs stuff to be initialized first.
        /// </summary>
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
        /// <summary>
        /// Read a copy of the setup.xml from the plugin's Resources folder and write it to disk
        /// </summary>
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
        /// <summary>
        /// Try reading the xml, catch exceptions, delete if any invalid data
        /// </summary>
        private void TestSetupXML()
        {
            try
            {
                var dataXml = XElement.Load("UserData/setup.xml");

                if (dataXml != null)
                {
                    IEnumerable<XElement> enumerable = dataXml.Elements();
                    foreach (XElement xelement in enumerable)
                    {
                        string text = xelement.Name.ToString();
                        switch (text)
                        {
                            case null:
                                break;
                            case "Width":
                                var width = int.Parse(xelement.Value);
                                break;
                            case "Height":
                                var height = int.Parse(xelement.Value);
                                break;
                            case "FullScreen":
                                var full = bool.Parse(xelement.Value);
                                break;
                            case "Quality":
                                var quality = int.Parse(xelement.Value);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch
            {
                File.Delete("UserData/setup.xml");
            }
        }
    }
}