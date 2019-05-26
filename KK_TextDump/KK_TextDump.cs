using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logger;
using Object = UnityEngine.Object;

namespace KK_TextDump
{
    /// <summary>
    /// Dumps untranslated text to .txt files
    /// </summary>
    [BepInProcess("CharaStudio")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_TextDump : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.textdump";
        public const string PluginName = "KK_TextDump";
        public const string Version = "1.0";

        private void Main() => DumpText();

        private void DumpText()
        {
            if (Directory.Exists(Path.Combine(Paths.GameRootPath, "TextDump")))
            {
                Logger.Log(LogLevel.Info, "[TextDump] Not dumping text, folder already exists.");
                return;
            }

            int a = DumpCommunicationText();
            int b = DumpScenarioText();
            int c = DumpHText();
            Logger.Log(LogLevel.Info, $"[TextDump] Total lines:{a + b + c}");
        }

        private int DumpCommunicationText()
        {
            HashSet<string> AllJPText = new HashSet<string>();

            foreach (var AssetBundleName in CommonLib.GetAssetBundleNameListFromPath("communication"))
            {
                if (AssetBundleName.Contains("hit_"))
                    continue;

                foreach (var AssetName in AssetBundleCheck.GetAllAssetName(AssetBundleName))
                {
                    if (AssetName.Contains("speed_"))
                        continue;

                    var Asset = ManualLoadAsset<ExcelData>(AssetBundleName, AssetName, "abdata");

                    HashSet<string> JPText = new HashSet<string>();

                    foreach (var param in Asset.list)
                    {
                        if (15 <= param.list.Count && !param.list[15].IsNullOrEmpty() && param.list[15] != "テキスト")
                        {
                            AllJPText.Add($"//{param.list[15]}=");
                            JPText.Add($"//{param.list[15]}=");
                        }
                    }

                    if (JPText.Count > 0)
                    {
                        string FolderPath = Path.Combine(Paths.GameRootPath, "TextDump");
                        FolderPath = Path.Combine(FolderPath, AssetBundleName.Replace(".unity3d", ""));
                        FolderPath = Path.Combine(FolderPath, AssetName.Replace(".asset", ""));
                        FolderPath = FolderPath.Replace('/', '\\');
                        if (!Directory.Exists(FolderPath))
                            Directory.CreateDirectory(FolderPath);

                        string FilePath = Path.Combine(FolderPath, "translation.txt");
                        if (File.Exists(FilePath))
                            File.Delete(FilePath);

                        File.WriteAllLines(FilePath, JPText.ToArray());
                    }
                }
            }
            Logger.Log(LogLevel.Info, $"[TextDump] Total Communication unique lines:{AllJPText.Count}");
            return AllJPText.Count;
        }

        private int DumpScenarioText()
        {
            HashSet<string> AllJPText = new HashSet<string>();

            foreach (var AssetBundleName in CommonLib.GetAssetBundleNameListFromPath("adv/scenario", true))
            {
                foreach (var AssetName in AssetBundleCheck.GetAllAssetName(AssetBundleName)) //.Where(x => x.StartsWith("personality_voice_"))
                {

                    var Asset = ManualLoadAsset<ADV.ScenarioData>(AssetBundleName, AssetName, "abdata");

                    HashSet<string> JPText = new HashSet<string>();
                    foreach (var param in Asset.list)
                    {
                        if (param.Command == ADV.Command.Text)
                        {
                            if (1 <= param.Args.Length && !param.Args[1].IsNullOrEmpty())
                            {
                                AllJPText.Add($"//{param.Args[1]}=");
                                JPText.Add($"//{param.Args[1]}=");
                            }
                        }
                    }

                    if (JPText.Count > 0)
                    {
                        string FolderPath = Path.Combine(Paths.GameRootPath, "TextDump");
                        FolderPath = Path.Combine(FolderPath, AssetBundleName.Replace(".unity3d", ""));
                        FolderPath = Path.Combine(FolderPath, AssetName.Replace(".asset", ""));
                        FolderPath = FolderPath.Replace('/', '\\');
                        if (!Directory.Exists(FolderPath))
                            Directory.CreateDirectory(FolderPath);

                        string FilePath = Path.Combine(FolderPath, "translation.txt");
                        if (File.Exists(FilePath))
                            File.Delete(FilePath);

                        File.WriteAllLines(FilePath, JPText.ToArray());
                    }
                }
            }
            Logger.Log(LogLevel.Info, $"[TextDump] Total Scenario unique lines:{AllJPText.Count}");
            return AllJPText.Count;
        }

        private int DumpHText()
        {
            HashSet<string> AllJPText = new HashSet<string>();

            foreach (var AssetBundleName in CommonLib.GetAssetBundleNameListFromPath("h/list/"))
            {
                foreach (var AssetName in AssetBundleCheck.GetAllAssetName(AssetBundleName).Where(x => x.StartsWith("personality_voice_")))
                {
                    if (AssetName.EndsWith(".txt"))
                    {

                        var Asset = ManualLoadAsset<TextAsset>(AssetBundleName, AssetName, "abdata");

                        HashSet<string> JPText = new HashSet<string>();
                        string[] Rows = Asset.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        for (int i = 0; i < Rows.Count(); i++)
                        {
                            string[] Cells = Rows[i].Split('\t');
                            if (4 < Cells.Length && !Cells[4].IsNullOrEmpty())
                            {
                                AllJPText.Add($"//{Cells[4]}=");
                                JPText.Add($"//{Cells[4]}=");
                            }
                            if (27 < Cells.Length && !Cells[27].IsNullOrEmpty())
                            {
                                AllJPText.Add($"//{Cells[27]}=");
                                JPText.Add($"//{Cells[27]}=");
                            }
                            if (50 < Cells.Length && !Cells[50].IsNullOrEmpty())
                            {
                                AllJPText.Add($"//{Cells[50]}=");
                                JPText.Add($"//{Cells[50]}=");
                            }
                            if (73 < Cells.Length && !Cells[73].IsNullOrEmpty())
                            {
                                AllJPText.Add($"//{Cells[73]}=");
                                JPText.Add($"//{Cells[73]}=");
                            }
                        }

                        if (JPText.Count > 0)
                        {
                            string FolderPath = Path.Combine(Paths.GameRootPath, "TextDump");
                            FolderPath = Path.Combine(FolderPath, AssetBundleName.Replace(".unity3d", ""));
                            FolderPath = Path.Combine(FolderPath, AssetName.Replace(".txt", ""));
                            FolderPath = FolderPath.Replace('/', '\\');
                            if (!Directory.Exists(FolderPath))
                                Directory.CreateDirectory(FolderPath);

                            string FilePath = Path.Combine(FolderPath, "translation.txt");
                            if (File.Exists(FilePath))
                                File.Delete(FilePath);

                            File.WriteAllLines(FilePath, JPText.ToArray());
                        }
                    }
                }
            }
            Logger.Log(LogLevel.Info, $"[TextDump] Total H-Scene unique lines:{AllJPText.Count}");
            return AllJPText.Count;
        }

        public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : Object
        {
            AssetBundleManager.LoadAssetBundleInternal(bundle, false, manifest);
            var assetBundle = AssetBundleManager.GetLoadedAssetBundle(bundle, out _, manifest);

            T output = assetBundle.m_AssetBundle.LoadAsset<T>(asset);

            return output;
        }
    }
}
