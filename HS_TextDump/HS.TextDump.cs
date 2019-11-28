using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace KK_Plugins
{
    /// <summary>
    /// Dumps untranslated text to .txt files
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class TextDump : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.textdump";
        public const string PluginName = "Text Dump";
        public const string PluginNameInternal = "HS_TextDump";
        public const string Version = "1.1";

        public static ConfigEntry<bool> Enabled { get; private set; }

        internal void Main()
        {
            Enabled = Config.Bind("Settings", "Enabled", false, "Whether the plugin is enabled");
            if (!Enabled.Value) return;

            if (Directory.Exists(Path.Combine(Paths.GameRootPath, "TextDump")))
                Directory.Delete(Path.Combine(Paths.GameRootPath, "TextDump"), true);

            DumpText();
        }

        private void DumpText()
        {
            int a = DumpSubtitleText();
            Logger.LogInfo($"Total lines:{a}");
        }

        private int DumpSubtitleText()
        {
            HashSet<string> AllJPText = new HashSet<string>();
            Dictionary<string, Dictionary<string, string>> AllText = new Dictionary<string, Dictionary<string, string>>();

            foreach (var AssetBundleName in CommonLib.GetAssetBundleNameListFromPath("studioneo/info/"))
            {
                foreach (var AssetName in AssetBundleCheck.GetAllAssetName(AssetBundleName).Where(x => x.StartsWith("voice_")))
                {
                    var AssetSplit = AssetName.Replace(".asset", "").Split('_');
                    var Asset = CommonLib.LoadAsset<ExcelData>(AssetBundleName, AssetName);

                    string prefix = AssetSplit[3] == "00" ? "c" + AssetSplit[1] : "BattleArena";

                    if (!AllText.TryGetValue(prefix, out var _))
                        AllText[prefix] = new Dictionary<string, string>();

                    foreach (var param in Asset.list)
                    {
                        if (5 <= param.list.Count && !param.list[5].IsNullOrEmpty() && param.list[5] != "ファイル名")
                        {
                            AllJPText.Add(param.list[3]);
                            AllText[prefix][param.list[5]] = param.list[3];
                        }
                    }
                }
            }

            foreach (var TranslationsKVP in AllText)
            {
                var Translations = TranslationsKVP.Value;
                if (Translations.Count > 0)
                {
                    string FolderPath = Path.Combine(Paths.GameRootPath, "TextDump");
                    string FilePath = Path.Combine(FolderPath, $"{TranslationsKVP.Key}.xml");

                    Directory.CreateDirectory(FolderPath);
                    if (File.Exists(FilePath))
                        File.Delete(FilePath);

                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;

                    using (XmlWriter writer = XmlWriter.Create(FilePath, settings))
                    {
                        writer.WriteStartElement("HS_Subtitles");

                        foreach (var tl in Translations)
                        {
                            writer.WriteStartElement("Sub");
                            writer.WriteAttributeString("Asset", tl.Key.Trim());
                            writer.WriteAttributeString("Text", tl.Value.Trim());
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.Flush();
                    }
                }
            }

            Logger.Log(LogLevel.Info, $"[TextDump] Total Subtitle unique lines:{AllJPText.Count}");
            return AllJPText.Count;
        }
    }
}
