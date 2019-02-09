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
using UnityEngine;
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
        public const string Version = "1.2";
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

        private static string GetOOBase(ChaControl Character)
        {
            //Characters will always use the uncensor assigned to them if it exists
            if (UncensorList.TryGetValue(Character.chaFile.parameter.fullname.Trim(), out string Uncensor) && AssetBundleCheck.IsFile(Uncensor))
                return Uncensor;

            //Characters will use the uncensor assigned for their sex if one has been set
            if (UncensorList.TryGetValue(Character.chaFile.parameter.sex.ToString(), out Uncensor) && AssetBundleCheck.IsFile(Uncensor))
                return Uncensor;

            //Characters will use the wildcard uncensor if one is set
            if (UncensorList.TryGetValue("*", out Uncensor) && AssetBundleCheck.IsFile(Uncensor))
                return Uncensor;

            //If no other uncensor is defined, the default oo_base is used
            return "chara/oo_base.unity3d";
        }

        //Color matching stuff
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), "SetBodyBaseMaterial", null, null)]
        public static void SetBodyBaseMaterial(ChaControl __instance)
        {
            SetDickMaterial(__instance);
            SetBallsMaterial(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload), null, null)]
        public static void Reload(ChaControl __instance)
        {
            SetDickMaterial(__instance);
            SetBallsMaterial(__instance);
        }

        public static void SetDickMaterial(ChaControl __instance)
        {
            string oo_base = GetOOBase(__instance);

            //get main tex
            string text_t = (__instance.sex == 0) ? "cm_dankon_00_t" : "cf_dankon_00_t";
            Texture2D mainTexture = CommonLib.LoadAsset<Texture2D>(oo_base, text_t, false, string.Empty);
            Singleton<Manager.Character>.Instance.AddLoadAssetBundle(oo_base, string.Empty);
            if (mainTexture == null)
                return;

            //get color mask
            string text_mc = (__instance.sex == 0) ? "cm_dankon_00_mc" : "cf_dankon_00_mc";
            Texture2D colorMask = CommonLib.LoadAsset<Texture2D>(oo_base, text_mc, false, string.Empty);
            Singleton<Manager.Character>.Instance.AddLoadAssetBundle(oo_base, string.Empty);
            if (colorMask == null)
                return;

            //find the dick
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(__instance.objBody.transform);
            GameObject objectFromName = findAssist.GetObjectFromName("o_dankon");
            if (objectFromName == null)
                return;

            string drawMatName = (__instance.sex == 0) ? "cm_m_dankon" : "cf_m_dankon";
            string createMatName = (__instance.sex == 0) ? "cm_m_dankon_create" : "cf_m_dankon_create";
            var CustomTex = new CustomTextureControl(objectFromName.transform);
            CustomTex.Initialize(oo_base, drawMatName, string.Empty, oo_base, createMatName, string.Empty, 2048, 2048, RenderTextureFormat.ARGB32);

            CustomTex.SetMainTexture(mainTexture);
            CustomTex.SetColor(ChaShader._Color, __instance.chaFile.custom.body.skinMainColor);

            CustomTex.SetTexture(ChaShader._ColorMask, colorMask);
            CustomTex.SetColor(ChaShader._Color2, __instance.chaFile.custom.body.skinSubColor);

            //set the new texture
            var NewTex = CustomTex.RebuildTextureAndSetMaterial();
            if (NewTex == null)
                return;

            objectFromName.GetComponent<Renderer>().material.SetTexture(ChaShader._MainTex, NewTex);
        }

        public static void SetBallsMaterial(ChaControl __instance)
        {
            string oo_base = GetOOBase(__instance);

            //get main tex
            string text_t = (__instance.sex == 0) ? "cm_dan_f_00_t" : "cf_dan_f_00_t";
            Texture2D mainTexture = CommonLib.LoadAsset<Texture2D>(oo_base, text_t, false, string.Empty);
            Singleton<Manager.Character>.Instance.AddLoadAssetBundle(oo_base, string.Empty);
            if (mainTexture == null)
                return;

            //get color mask
            string text_mc = (__instance.sex == 0) ? "cm_dan_f_00_mc" : "cf_dan_f_00_mc";
            Texture2D colorMask = CommonLib.LoadAsset<Texture2D>(oo_base, text_mc, false, string.Empty);
            Singleton<Manager.Character>.Instance.AddLoadAssetBundle(oo_base, string.Empty);
            if (colorMask == null)
                return;

            //find the balls
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(__instance.objBody.transform);
            GameObject objectFromName = findAssist.GetObjectFromName("o_dan_f");
            if (objectFromName == null)
                return;

            string drawMatName = (__instance.sex == 0) ? "cm_m_dan_f" : "cf_m_dan_f";
            string createMatName = (__instance.sex == 0) ? "cm_m_dan_f_create" : "cf_m_dan_f_create";
            var CustomTex = new CustomTextureControl(objectFromName.transform);
            CustomTex.Initialize(oo_base, drawMatName, string.Empty, oo_base, createMatName, string.Empty, 2048, 2048, RenderTextureFormat.ARGB32);

            CustomTex.SetMainTexture(mainTexture);
            CustomTex.SetColor(ChaShader._Color, __instance.chaFile.custom.body.skinMainColor);

            CustomTex.SetTexture(ChaShader._ColorMask, colorMask);
            CustomTex.SetColor(ChaShader._Color2, __instance.chaFile.custom.body.skinSubColor);

            //set the new texture
            var NewTex = CustomTex.RebuildTextureAndSetMaterial();
            if (NewTex == null)
                return;

            objectFromName.GetComponent<Renderer>().material.SetTexture(ChaShader._MainTex, NewTex);
        }

        //LineMask texture assigned to the material, toggled on and off for the dick/balls along with the body
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.VisibleAddBodyLine))]
        public static void VisibleAddBodyLine(ChaControl __instance)
        {
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(__instance.objBody.transform);
            GameObject o_dankon = findAssist.GetObjectFromName("o_dankon");
            if (o_dankon != null)
                o_dankon.GetComponent<Renderer>().material.SetFloat(ChaShader._linetexon, __instance.chaFile.custom.body.drawAddLine ? 1f : 0f);

            findAssist = new FindAssist();
            findAssist.Initialize(__instance.objBody.transform);
            GameObject o_dan_f = findAssist.GetObjectFromName("o_dan_f");
            if (o_dan_f != null)
                o_dan_f.GetComponent<Renderer>().material.SetFloat(ChaShader._linetexon, __instance.chaFile.custom.body.drawAddLine ? 1f : 0f);
        }

        //Skin gloss slider level, as assigned in the character maker.
        //This corresponds to the red coloring in the DetailMask texture assigned to the dick/balls material
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingSkinGlossPower))]
        public static void ChangeSettingSkinGlossPower(ChaControl __instance)
        {
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(__instance.objBody.transform);
            GameObject o_dankon = findAssist.GetObjectFromName("o_dankon");
            if (o_dankon != null)
                o_dankon.GetComponent<Renderer>().material.SetFloat(ChaShader._SpecularPower, Mathf.Lerp(__instance.chaFile.custom.body.skinGlossPower, 1f, __instance.chaFile.status.skinTuyaRate));

            findAssist = new FindAssist();
            findAssist.Initialize(__instance.objBody.transform);
            GameObject o_dan_f = findAssist.GetObjectFromName("o_dan_f");
            if (o_dan_f != null)
                o_dan_f.GetComponent<Renderer>().material.SetFloat(ChaShader._SpecularPower, Mathf.Lerp(__instance.chaFile.custom.body.skinGlossPower, 1f, __instance.chaFile.status.skinTuyaRate));
        }
        /// <summary>
        /// For traps and futas, set the normals for the chest area
        /// This prevents strange shadowing around flat-chested trap/futa characters
        /// Currently only works on files that end with _trap or _futa. Probably needs a better implementation.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomBodyWithoutCustomTexture))]
        public static void ChangeCustomBodyWithoutCustomTexture(ChaControl __instance)
        {
            string oo_base = GetOOBase(__instance);

            if (__instance.sex == 0 && __instance.hiPoly && (oo_base.ToLower().EndsWith("_trap.unity3d") || oo_base.ToLower().EndsWith("_futa.unity3d")))
            {
                if (__instance.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlBody, out BustNormal bustNormal))
                    bustNormal.Release();

                bustNormal = new BustNormal();
                bustNormal.Init(__instance.objBody, oo_base, "p_cf_body_00_Nml", string.Empty);
                __instance.dictBustNormal[ChaControl.BustNormalKind.NmlBody] = bustNormal;
            }
        }
    }
}
