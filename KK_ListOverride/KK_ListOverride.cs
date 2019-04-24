using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using Logger = BepInEx.Logger;
using Harmony;
using BepInEx.Logging;
using System.Xml.Linq;
using System.IO;

namespace KK_ListOverride
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_ListOverride : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.listoverride";
        public const string PluginName = "List Override";
        public const string PluginNameInternal = nameof(KK_ListOverride);
        public const string Version = "1.0";
        private static readonly string ListOverrideFolder = Path.Combine(Paths.PluginPath, "KK_ListOverride");

        public KK_ListOverride()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_ListOverride));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ChaListControl), "LoadListInfoAll")]
        public static void LoadListInfoAllPostfix(ChaListControl __instance)
        {
            if (!Directory.Exists(ListOverrideFolder)) return;

            int counter = 0;
            Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>> dictListInfo = Traverse.Create(__instance).Field("dictListInfo").GetValue() as Dictionary<ChaListDefine.CategoryNo, Dictionary<int, ListInfoBase>>;

            foreach (var fileName in Directory.GetFiles(ListOverrideFolder))
            {
                try
                {
                    XDocument doc = XDocument.Load(fileName);
                    foreach (var overrideElement in doc.Root.Elements())
                    {
                        ChaListDefine.CategoryNo categoryNo;
                        if (int.TryParse(overrideElement.Attribute("Category").Value, out int category))
                            categoryNo = (ChaListDefine.CategoryNo)category;
                        else
                            categoryNo = (ChaListDefine.CategoryNo)Enum.Parse(typeof(ChaListDefine.CategoryNo), overrideElement.Attribute("Category").Value);

                        ChaListDefine.KeyType keyType;
                        if (int.TryParse(overrideElement.Attribute("KeyType").Value, out int key))
                            keyType = (ChaListDefine.KeyType)key;
                        else
                            keyType = (ChaListDefine.KeyType)Enum.Parse(typeof(ChaListDefine.KeyType), overrideElement.Attribute("KeyType").Value);

                        int id = int.Parse(overrideElement.Attribute("ID").Value);
                        string value = overrideElement.Attribute("Value").Value;

                        //Don't allow people to change IDs, that's sure to break everything.
                        if (keyType == ChaListDefine.KeyType.ID) continue;

                        dictListInfo[categoryNo][id].dictInfo[(int)keyType] = value;
                        counter++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Failed to load {PluginNameInternal} xml file.");
                    Logger.Log(LogLevel.Error, ex);
                }
            }

            Logger.Log(LogLevel.Debug, $"[{PluginNameInternal}] Loaded {counter} overrides");
        }
    }
}
