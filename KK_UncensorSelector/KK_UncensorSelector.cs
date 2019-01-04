using BepInEx;
using BepInEx.Logging;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Logger = BepInEx.Logger;
/// <summary>
/// Plugin for assigning uncensors to characters individually
/// </summary>
namespace KK_UncensorSelector
{
    [BepInPlugin("com.deathweasel.bepinex.uncensorselector", "Uncensor Selector", Version)]
    public class KK_UncensorSelector : BaseUnityPlugin
    {
        public const string Version = "1.0";
        private static string CharacterName = "";
        private static readonly string UncensorSelectorFilePath = Path.Combine(Paths.PluginPath, "KK_UncensorSelector.csv");
        private static Dictionary<string, string> UncensorList = new Dictionary<string, string>();

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.uncensorselector");
            harmony.PatchAll(typeof(KK_UncensorSelector));

            Type LoadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).Where(x => x.Name.StartsWith("<LoadAsync>c__Iterator")).First();
            MethodInfo LoadAsyncIteratorMoveNext = LoadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(LoadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(KK_UncensorSelector).GetMethod(nameof(LoadAsyncTranspiler), BindingFlags.Static | BindingFlags.Public)));

            if (File.Exists(UncensorSelectorFilePath))
                GenerateUncensorList();
        }
        /// <summary>
        /// Generate the dictionary of CharacterName,UncensorName from KK_UncensorSelector.csv
        /// </summary>
        private static void GenerateUncensorList()
        {
            using (StreamReader reader = new StreamReader(UncensorSelectorFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        string[] parts = line.Split(',');
                        UncensorList.Add(parts[0], parts[1]);
                    }
                    catch
                    {
                        Logger.Log(LogLevel.Error, $"Error reading KK_UncensorSelector.csv line, skipping.");
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAsync))]
        public static void LoadAsyncPrefix(ChaControl __instance)
        {
            CharacterName = __instance.chaFile.parameter.fullname.Trim();
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
            if (UncensorList.TryGetValue(CharacterName, out string Uncensor) && AssetBundleCheck.IsFile(Uncensor))
                return Uncensor;
            else if (UncensorList.TryGetValue("*", out string DefaultUncensor) && AssetBundleCheck.IsFile(DefaultUncensor))
                return DefaultUncensor;

            return "chara/oo_base.unity3d";
        }
    }
}
