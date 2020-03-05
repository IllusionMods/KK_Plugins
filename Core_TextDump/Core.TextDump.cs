using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KK_Plugins
{
    public partial class TextDump
    {
        public const string GUID = "com.deathweasel.bepinex.textdump";
        public const string PluginName = "Text Dump";
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
            int a = DumpCommunicationText();
            int b = DumpScenarioText();
            int c = DumpHText();
            Logger.LogInfo($"[TextDump] Total lines:{a + b + c}");
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

                    Dictionary<string, string> Translations = new Dictionary<string, string>();

                    if (AssetName.StartsWith("optiondisplayitems"))
                    {
                        foreach (var param in Asset.list)
                        {
                            if (param.list[0] == "no") continue;
                            for (int i = 1; i < 4; i++)
                            {
                                if (param.list.Count <= i) continue;
                                if (param.list[i].IsNullOrWhiteSpace()) continue;
                                if (param.list[i] == "・・・") continue;
                                AllJPText.Add(param.list[i]);
                                Translations[param.list[i]] = "";
                                try
                                {
                                    Translations[param.list[i]] = param.list[i + 3];
                                }
                                catch { }
                            }
                        }
                    }
                    else
                    {
                        foreach (var param in Asset.list)
                        {
                            if (15 <= param.list.Count && !param.list[15].IsNullOrEmpty() && param.list[15] != "テキスト")
                            {
                                AllJPText.Add(param.list[15]);
                                Translations[param.list[15]] = "";
                                try
                                {
                                    Translations[param.list[15]] = param.list[20];
                                }
                                catch { }
                            }
                        }
                    }

                    if (Translations.Count > 0)
                    {
                        Logger.LogInfo($"Writing Translations:{AssetName}");

                        string FolderPath = Path.Combine(Paths.GameRootPath, "TextDump");
                        FolderPath = Path.Combine(FolderPath, AssetBundleName.Replace(".unity3d", ""));
                        FolderPath = Path.Combine(FolderPath, AssetName.Replace(".asset", ""));
                        FolderPath = FolderPath.Replace('/', '\\');
                        if (!Directory.Exists(FolderPath))
                            Directory.CreateDirectory(FolderPath);

                        string FilePath = Path.Combine(FolderPath, "translation.txt");
                        if (File.Exists(FilePath))
                            File.Delete(FilePath);

                        List<string> Lines = new List<string>();
                        foreach (var tl in Translations)
                        {
                            string JP = tl.Key.Trim();
                            string ENG = tl.Value.Trim();
                            if (JP.Contains("\n"))
                                JP = $"\"{JP.Replace("\n", @"\n").Trim()}\"";
                            if (ENG.Contains("\n"))
                                ENG = $"\"{ENG.Replace("\n", @"\n").Trim()}\"";
                            ENG = ENG.Replace(";", ",");

                            if (ENG.IsNullOrEmpty())
                                Lines.Add($"{JP}=");
                            else
                                Lines.Add($"{JP}={ENG}");
                        }

                        File.WriteAllLines(FilePath, Lines.ToArray());
                    }
                    else
                        Logger.LogInfo($"No Translations:{AssetName}");
                }
            }
            Logger.LogInfo($"[TextDump] Total Communication unique lines:{AllJPText.Count}");
            return AllJPText.Count;
        }

        private string BuildReplacementKey(string assetBundleName, string key)
        {
            return string.Join("|", new string[] { Path.GetDirectoryName(assetBundleName), key });
        }

        private Dictionary<string, string> BuildReplacementDictionary()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            List<string> assetBundleNames = CommonLib.GetAssetBundleNameListFromPath("adv/scenario", true);
            assetBundleNames.Sort();

            foreach (var AssetBundleName in assetBundleNames)
            {
                List<string> AssetNameList = new List<string>(AssetBundleCheck.GetAllAssetName(AssetBundleName));
                AssetNameList.Sort();
                foreach (var AssetName in AssetNameList)
                {
                    var Asset = ManualLoadAsset<ADV.ScenarioData>(AssetBundleName, AssetName, "abdata");
                    textResourceHelper.BuildReplacements(Asset.list).ToList().ForEach(x => result[BuildReplacementKey(AssetBundleName, x.Key)] = x.Value);
                }
            }
            return result;
        }

        private int DumpScenarioText()
        {
            HashSet<string> AllJPText = new HashSet<string>();

            Dictionary<string, string> choiceDictionary = BuildReplacementDictionary();

            foreach (var AssetBundleName in CommonLib.GetAssetBundleNameListFromPath("adv/scenario", true))
            {
                foreach (var AssetName in AssetBundleCheck.GetAllAssetName(AssetBundleName)) //.Where(x => x.StartsWith("personality_voice_"))
                {
                    var Asset = ManualLoadAsset<ADV.ScenarioData>(AssetBundleName, AssetName, "abdata");

                    Dictionary<string, string> Translations = new Dictionary<string, string>();

                    foreach (var param in Asset.list)
                    {
                        if (!textResourceHelper.IsSupportedCommand(param.Command))
                        {
                            continue;
                        }

                        if (param.Command == ADV.Command.Text)
                        {
                            if (param.Args.Length >= 2 && !param.Args[1].IsNullOrEmpty())
                            {
                                AllJPText.Add(param.Args[1]);
                                Translations[param.Args[1]] = "";
                                if (param.Args.Length >= 3 && !param.Args[2].IsNullOrEmpty())
                                    Translations[param.Args[1]] = param.Args[2];
                            }
                        }
                        else if (param.Command == ADV.Command.Calc)
                        {
                            if (param.Args.Length >= 3 && textResourceHelper.CalcKeys.Contains(param.Args[0]))
                            {
                                var key = textResourceHelper.GetSpecializedKey(param, 2, out string value);
                                AllJPText.Add(key);
                                Translations[key] = value;
                            }
                        }
                        else if (param.Command == ADV.Command.Format)
                        {
                            if (param.Args.Length >= 2 && textResourceHelper.FormatKeys.Contains(param.Args[0]))
                            {
                                AllJPText.Add(param.Args[1]);
                                Translations[param.Args[1]] = "";
                            }
                        }
                        else if (param.Command == ADV.Command.Choice)
                        {
                            for (int i = 0; i < param.Args.Length; i++)
                            {
                                var key = textResourceHelper.GetSpecializedKey(param, i, out string fallbackValue);
                                if (!key.IsNullOrEmpty())
                                {
                                    if (!choiceDictionary.TryGetValue(BuildReplacementKey(AssetBundleName, fallbackValue), out string value))
                                    {
                                        value = fallbackValue;
                                    }
                                    AllJPText.Add(key);
                                    Translations[key] = value;
                                }
                            }
                        }
#if false
                        else if (param.Command == ADV.Command.Switch)
                        {
                            for (int i = 0; i < param.Args.Length; i++)
                            {
                                var key = textResourceHelper.GetSpecializedKey(param, i, out string value);
                                AllJPText.Add(key);
                                Translations[key] = value;
                            }
                        }
#endif
#if false
                        else if (param.Command == ADV.Command.InfoText)
                        {
                            for (int i = 2; i < param.Args.Length; i += 2)
                            {
                                AllJPText.Add(param.Args[i]);
                                Translations[param.Args[i]] = "";
                            }
                        }
#endif
#if false
                        else if (param.Command == ADV.Command.Jump)
                        {
                            if (param.Args.Length >= 1 && !AllAscii.IsMatch(param.Args[0]))
                            {
                                AllJPText.Add(param.Args[0]);
                                Translations[param.Args[0]] = "Jump";
                            }
                        }
#endif
                        else
                        {
                            Logger.LogDebug($"[TextDump] Unsupported command: {param.Command}: {string.Join(",", param.Args.Select((a) => a?.ToString() ?? string.Empty).ToArray())}");
                        }
                    }

                    if (Translations.Count > 0)
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

                        List<string> Lines = new List<string>();
                        foreach (var tl in Translations)
                        {
                            string JP = tl.Key.Trim();
                            string ENG = tl.Value.Trim();
                            if (JP.Contains("\n"))
                                JP = $"\"{JP.Replace("\n", @"\n").Trim()}\"";
                            if (ENG.Contains("\n"))
                                ENG = $"\"{ENG.Replace("\n", @"\n").Trim()}\"";
                            ENG = ENG.Replace(";", ",");

                            if (ENG.IsNullOrEmpty())
                                Lines.Add($"{JP}=");
                            else
                                Lines.Add($"{JP}={ENG}");
                        }

                        File.WriteAllLines(FilePath, Lines.ToArray());
                    }
                }
            }
            Logger.LogInfo($"[TextDump] Total Scenario unique lines:{AllJPText.Count}");
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
                                AllJPText.Add($"{Cells[4]}=");
                                JPText.Add($"{Cells[4]}=");
                            }
                            if (27 < Cells.Length && !Cells[27].IsNullOrEmpty())
                            {
                                AllJPText.Add($"{Cells[27]}=");
                                JPText.Add($"{Cells[27]}=");
                            }
                            if (50 < Cells.Length && !Cells[50].IsNullOrEmpty())
                            {
                                AllJPText.Add($"{Cells[50]}=");
                                JPText.Add($"{Cells[50]}=");
                            }
                            if (73 < Cells.Length && !Cells[73].IsNullOrEmpty())
                            {
                                AllJPText.Add($"{Cells[73]}=");
                                JPText.Add($"{Cells[73]}=");
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
            Logger.LogInfo($"[TextDump] Total H-Scene unique lines:{AllJPText.Count}");
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
