using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Xml.Linq;

namespace KK_Plugins
{
    public partial class ListOverride
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.LoadListInfoAll))]
            private static void LoadListInfoAllPostfix(ChaListControl __instance)
            {
                if (!Directory.Exists(ListOverrideFolder)) return;

                int counter = 0;

                var files = Directory.GetFiles(ListOverrideFolder);
                for (var i = 0; i < files.Length; i++)
                {
                    var fileName = files[i];
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
                            if (keyType == ChaListDefine.KeyType.ID)
                                continue;

                            if (__instance.dictListInfo[categoryNo].ContainsKey(id))
                            {
                                __instance.dictListInfo[categoryNo][id].dictInfo[(int)keyType] = value;
                                counter++;
                            }
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
}