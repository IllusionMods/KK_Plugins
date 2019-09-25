using ActionGame.Communication;
using BepInEx;
using BepInEx.Logging;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace KK_Plugins
{
    /// <summary>
    /// Displays subitles on screen for H scenes and in dialogues
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class Subtitles : BaseUnityPlugin
    {
        internal static Info ActionGameInfoInstance;
        internal static HSceneProc HSceneProcInstance;

        internal void Main() => LoadSubtitles();

        private void LoadSubtitles()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.CharaMakerSubs.xml"))
            using (XmlReader reader = XmlReader.Create(stream))
            {
                XDocument doc = XDocument.Load(reader);
                foreach (var element in doc.Root.Elements("Sub"))
                    SubtitleDictionary[element.Attribute("Asset").Value] = element.Attribute("Text").Value;
            }
        }
    }
}
