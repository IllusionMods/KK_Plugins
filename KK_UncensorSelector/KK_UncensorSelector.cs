using BepInEx;
using BepInEx.Logging;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Logger = BepInEx.Logger;

namespace KK_UncensorSelector
{
    /// <summary>
    /// Plugin for assigning uncensors to characters individually
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_UncensorSelector : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.uncensorselector";
        public const string PluginName = "Uncensor Selector";
        public const string Version = "1.1";
        private static string CharacterName = "";
        private static byte CharacterSex = 0;
        private static readonly string UncensorSelectorPath = Path.Combine(Paths.PluginPath, "KK_UncensorSelector");
        private static Dictionary<string, string> UncensorList = new Dictionary<string, string>();

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_UncensorSelector));

            Type LoadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).Where(x => x.Name.StartsWith("<LoadAsync>c__Iterator")).First();
            MethodInfo LoadAsyncIteratorMoveNext = LoadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(LoadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(KK_UncensorSelector).GetMethod(nameof(LoadAsyncTranspiler), BindingFlags.Static | BindingFlags.Public)));

            GenerateUncensorList();
        }
        /// <summary>
        /// Generate the dictionary of CharacterName,UncensorName from KK_UncensorSelector.csv or KK_UncensorSelector folder
        /// </summary>
        private static void GenerateUncensorList()
        {
            void ReadFile(string UncensorSelectorFile)
            {
                using (StreamReader reader = new StreamReader(UncensorSelectorFile))
                {
                    try
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line == "")
                                continue;
                            if (line.StartsWith(@"//"))
                                continue;

                            string[] parts = line.Split(',');
                            if (parts[0].Trim().ToLower() == "m" || parts[0].Trim().ToLower() == "male" || parts[0].Trim() == "0")
                                UncensorList["0"] = parts[1].Trim();
                            else if (parts[0].Trim().ToLower() == "f" || parts[0].Trim().ToLower() == "female" || parts[0].Trim() == "1")
                                UncensorList["1"] = parts[1].Trim();
                            else
                                UncensorList[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                    catch
                    {
                        StringBuilder sb = new StringBuilder("Error reading KK_UncensorSelector file.").Append(Environment.NewLine);
                        sb.Append($"File: {UncensorSelectorFile}");
                        Logger.Log(LogLevel.Error, sb.ToString());
                    }
                }
            }

            var AllowedExtensions = new[] { ".txt", ".csv" };

            if (Directory.Exists(UncensorSelectorPath))
            {
                var Files = Directory
                            .GetFiles(UncensorSelectorPath)
                            .Where(file => AllowedExtensions.Any(file.ToLower().EndsWith))
                            .ToList();

                foreach (string UncensorSelectorFile in Files)
                    ReadFile(UncensorSelectorFile);

            }

            foreach (string Extension in AllowedExtensions)
                if (File.Exists($"{UncensorSelectorPath}{Extension}"))
                    ReadFile($"{UncensorSelectorPath}{Extension}");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAsync))]
        public static void LoadAsyncPrefix(ChaControl __instance)
        {
            CharacterName = __instance.chaFile.parameter.fullname.Trim();
            CharacterSex = __instance.chaFile.parameter.sex;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        public static void CreateBodyTexturePrefix(ChaControl __instance)
        {
            CharacterName = __instance.chaFile.parameter.fullname.Trim();
            CharacterSex = __instance.chaFile.parameter.sex;
        }

        public static IEnumerable<CodeInstruction> LoadAsyncTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            List<int> OOBaseIndex = new List<int>();

            foreach (var x in instructionsList)
            {
                if (x.operand?.ToString() == "chara/oo_base.unity3d")
                {
                    x.opcode = OpCodes.Call;
                    x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetOOBase), AccessTools.all);
                }
            }

            return instructions;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        public static IEnumerable<CodeInstruction> CreateBodyTextureTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            //cf_body_00_t, cf_body_00_mc, cm_body_00_mc, and _low variants
            foreach (var x in instructionsList)
            {
                if (x.operand?.ToString() == "chara/oo_base.unity3d")
                {
                    x.opcode = OpCodes.Call;
                    x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetOOBase), AccessTools.all);
                }
            }

            return instructions;
        }

        private static string SetOOBase()
        {
            //Characters will always use the uncensor assigned to them if it exists
            if (UncensorList.TryGetValue(CharacterName, out string Uncensor) && AssetBundleCheck.IsFile(Uncensor))
                return Uncensor;

            //Characters will use the uncensor assigned for their sex if one has been set
            if (UncensorList.TryGetValue(CharacterSex.ToString(), out Uncensor) && AssetBundleCheck.IsFile(Uncensor))
                return Uncensor;

            //Characters will use the wildcard uncensor if one is set
            if (UncensorList.TryGetValue("*", out Uncensor) && AssetBundleCheck.IsFile(Uncensor))
                return Uncensor;

            //If no other uncensor is defined, the default oo_base is used
            return "chara/oo_base.unity3d";
        }
    }
}
