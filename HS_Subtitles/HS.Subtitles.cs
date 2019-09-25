using BepInEx;
using BepInEx.Logging;
using System;
using System.IO;
using System.Xml.Linq;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BaseUnityPlugin
    {
        public const string PluginNameInternal = "HS_Subtitles";

        private readonly string XMLPath = Paths.BepInExRootPath + $@"\Translation\{PluginNameInternal}";

        internal void Main() => LoadSubtitles();

        private void LoadSubtitles()
        {
            if (Directory.Exists(XMLPath))
            {
                foreach (var fileName in Directory.GetFiles(XMLPath))
                {
                    try
                    {
                        XDocument doc = XDocument.Load(fileName);
                        foreach (var element in doc.Element(PluginNameInternal).Elements("Sub"))
                            SubtitleDictionary[element.Attribute("Asset").Value] = element.Attribute("Text").Value;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, $"Failed to load {PluginNameInternal} xml file.");
                        Logger.Log(LogLevel.Error, ex);
                    }
                }
            }
        }
    }
}
