using BepInEx;
using BepInEx.Logging;
using Harmony;
using Sideloader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_UncensorSelector
{
    /// <summary>
    /// Plugin for assigning uncensors to characters individually
    /// </summary>
    [BepInDependency("com.bepis.bepinex.sideloader")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_UncensorSelector : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.uncensorselector";
        public const string PluginName = "Uncensor Selector";
        public const string Version = "1.3";
        private static string CharacterName = "";
        private static byte CharacterSex = 0;
        private static readonly string UncensorSelectorPath = Path.Combine(Paths.PluginPath, "KK_UncensorSelector");
        private static Dictionary<string, string> CharacterUncensorList = new Dictionary<string, string>();
        private static Dictionary<string, UncensorData> UncensorList = new Dictionary<string, UncensorData>();
        public static readonly string modDirectory = Path.Combine(Paths.GameRootPath, "mods");

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_UncensorSelector));

            Type LoadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).Where(x => x.Name.StartsWith("<LoadAsync>c__Iterator")).First();
            MethodInfo LoadAsyncIteratorMoveNext = LoadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(LoadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(KK_UncensorSelector).GetMethod(nameof(LoadAsyncTranspiler), BindingFlags.Static | BindingFlags.Public)));

            GenerateUncensorList();
            GenerateCharacterUncensorList();
        }
        /// <summary>
        /// Read all the manifest.xml files and generate a dictionary of uncensors
        /// </summary>
        private void GenerateUncensorList()
        {
            var manifests = AccessTools.Field(typeof(Sideloader.Sideloader), "LoadedManifests").GetValue(GetComponent<Sideloader.Sideloader>()) as List<Manifest>;
            foreach (var manifest in manifests)
            {
                XDocument ManifestDocument = AccessTools.Field(typeof(Manifest), "manifestDocument").GetValue(manifest) as XDocument;
                XElement UncensorSelectorElement = ManifestDocument.Root?.Element("KK_UncensorSelector");
                if (UncensorSelectorElement != null && UncensorSelectorElement.HasElements)
                {
                    foreach (XElement UncensorElement in UncensorSelectorElement.Elements("uncensor"))
                    {
                        UncensorData uncensor = new UncensorData(UncensorElement);
                        if (uncensor.GUID == null)
                        {
                            Logger.Log(LogLevel.Warning, "Uncensor failed to load due to missing GUID.");
                            continue;
                        }
                        if (uncensor.DisplayName == null)
                        {
                            Logger.Log(LogLevel.Warning, "Uncensor failed to load due to missing display name.");
                            continue;
                        }
                        if (uncensor.OOBase == Defaults.OOBase)
                        {
                            Logger.Log(LogLevel.Warning, "Uncensor was not loaded because oo_base is the default.");
                            continue;
                        }
                        UncensorList.Add(uncensor.GUID, uncensor);
                    }
                }
            }
        }
        /// <summary>
        /// Generate the dictionary of CharacterName,UncensorName from KK_UncensorSelector.csv or KK_UncensorSelector folder
        /// </summary>
        private static void GenerateCharacterUncensorList()
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
                                CharacterUncensorList["0"] = parts[1].Trim();
                            else if (parts[0].Trim().ToLower() == "f" || parts[0].Trim().ToLower() == "female" || parts[0].Trim() == "1")
                                CharacterUncensorList["1"] = parts[1].Trim();
                            else
                                CharacterUncensorList[parts[0].Trim()] = parts[1].Trim();
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
        /// <summary>
        /// Get the UncensorData for the specified character
        /// </summary>
        private static UncensorData GetUncensorData(ChaControl Character) => GetUncensorData(Character.chaFile.parameter.fullname, Character.chaFile.parameter.sex);
        private static UncensorData GetUncensorData(string Name, byte Sex, bool MMBase = false) =>
            CharacterUncensorList.TryGetValue(Name.Trim(), out string UncensorID) && UncensorList.TryGetValue(UncensorID, out UncensorData Uncensor) && AssetBundleCheck.IsFile(MMBase ? Uncensor.MMBase : Uncensor.OOBase) ? Uncensor
          : CharacterUncensorList.TryGetValue(Sex.ToString(), out UncensorID) && UncensorList.TryGetValue(UncensorID, out Uncensor) && AssetBundleCheck.IsFile(MMBase ? Uncensor.MMBase : Uncensor.OOBase) ? Uncensor
          : CharacterUncensorList.TryGetValue("*", out UncensorID) && UncensorList.TryGetValue(UncensorID, out Uncensor) && AssetBundleCheck.IsFile(MMBase ? Uncensor.MMBase : Uncensor.OOBase) ? Uncensor
          : null;
        private static string SetOOBase() => GetUncensorData(CharacterName, CharacterSex)?.OOBase ?? Defaults.OOBase;
        private static string SetNormals() => GetUncensorData(CharacterName, CharacterSex)?.Normals ?? Defaults.Normals;
        private static string SetBodyMainTex() => GetUncensorData(CharacterName, CharacterSex)?.BodyMainTex ?? Defaults.BodyMainTex;
        private static string SetMaleBodyLow() => SetBodyAsset(0, false);
        private static string SetMaleBodyHigh() => SetBodyAsset(0, true);
        private static string SetFemaleBodyLow() => SetBodyAsset(1, false);
        private static string SetFemaleBodyHigh() => SetBodyAsset(1, true);
        private static string SetBodyAsset(byte sex, bool HiPoly) =>
            HiPoly ? (GetUncensorData(CharacterName, CharacterSex)?.AssetHighPoly ?? (sex == 0 ? Defaults.AssetMale : Defaults.AssetFemale))
                   : (GetUncensorData(CharacterName, CharacterSex)?.AssetLowPoly ?? (sex == 0 ? Defaults.AssetMaleLow : Defaults.AssetFemaleLow));
        private static string SetMMBase() => GetUncensorData(CharacterName, CharacterSex, true)?.MMBase ?? Defaults.MMBase;
        private static string SetBodyColorMaskMale() => SetColorMask(0);
        private static string SetBodyColorMaskFemale() => SetColorMask(1);
        private static string SetColorMask(byte sex) => GetUncensorData(CharacterName, CharacterSex)?.BodyColorMask ?? (sex == 0 ? Defaults.BodyColorMaskMale : Defaults.BodyColorMaskFemale);
        /// <summary>
        /// Do color matching for every object configured in the manifest.xml
        /// </summary>
        public static void ColorMatchMaterials(ChaControl __instance)
        {
            UncensorData Uncensor = GetUncensorData(__instance);
            if (Uncensor == null)
                return;

            foreach (var ColorMatchPart in Uncensor.ColorMatchList)
            {
                //get main tex
                Texture2D MainTexture = CommonLib.LoadAsset<Texture2D>(Uncensor.OOBase, ColorMatchPart.MainTex, false, string.Empty);
                if (MainTexture == null)
                    continue;

                //get color mask
                Texture2D ColorMask = CommonLib.LoadAsset<Texture2D>(Uncensor.OOBase, ColorMatchPart.ColorMask, false, string.Empty);
                if (ColorMask == null)
                    continue;

                //find the game object
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(__instance.objBody.transform);
                GameObject gameObject = findAssist.GetObjectFromName(ColorMatchPart.Object);
                if (gameObject == null)
                    continue;

                var CustomTex = new CustomTextureControl(gameObject.transform);
                CustomTex.Initialize(Uncensor.OOBase, ColorMatchPart.Material, string.Empty, Uncensor.OOBase, ColorMatchPart.MaterialCreate, string.Empty, 2048, 2048, RenderTextureFormat.ARGB32);

                CustomTex.SetMainTexture(MainTexture);
                CustomTex.SetColor(ChaShader._Color, __instance.chaFile.custom.body.skinMainColor);

                CustomTex.SetTexture(ChaShader._ColorMask, ColorMask);
                CustomTex.SetColor(ChaShader._Color2, __instance.chaFile.custom.body.skinSubColor);

                //set the new texture
                var NewTex = CustomTex.RebuildTextureAndSetMaterial();
                if (NewTex == null)
                    continue;

                gameObject.GetComponent<Renderer>().material.SetTexture(ChaShader._MainTex, NewTex);
            }
        }
        /// <summary>
        /// Set the character name/sex
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAsync))]
        public static void LoadAsyncPrefix(ChaControl __instance)
        {
            CharacterName = __instance.chaFile.parameter.fullname.Trim();
            CharacterSex = __instance.chaFile.parameter.sex;
        }
        /// <summary>
        /// Set the character name/sex
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        public static void CreateBodyTexturePrefix(ChaControl __instance)
        {
            CharacterName = __instance.chaFile.parameter.fullname.Trim();
            CharacterSex = __instance.chaFile.parameter.sex;
        }
        /// <summary>
        /// Modifies the code for string replacement of oo_base, etc.
        /// </summary>
        public static IEnumerable<CodeInstruction> LoadAsyncTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                switch (x.operand?.ToString())
                {
                    case "chara/oo_base.unity3d":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetOOBase), AccessTools.all);
                        break;
                    case "p_cm_body_00":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetMaleBodyHigh), AccessTools.all);
                        break;
                    case "p_cm_body_00_low":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetMaleBodyLow), AccessTools.all);
                        break;
                    case "p_cf_body_00":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetFemaleBodyHigh), AccessTools.all);
                        break;
                    case "p_cf_body_00_low":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetFemaleBodyLow), AccessTools.all);
                        break;
                    case "p_cf_body_00_Nml":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetNormals), AccessTools.all);
                        break;
                }
            }

            return instructions;
        }
        /// <summary>
        /// Modifies the code for string replacement of oo_base, etc.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        public static IEnumerable<CodeInstruction> CreateBodyTextureTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                switch (x.operand?.ToString())
                {
                    case "chara/oo_base.unity3d":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetOOBase), AccessTools.all);
                        break;
                    case "cf_body_00_t":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetBodyMainTex), AccessTools.all);
                        break;
                    case "cm_body_00_mc":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetBodyColorMaskMale), AccessTools.all);
                        break;
                    case "cf_body_00_mc":
                        x.opcode = OpCodes.Call;
                        x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetBodyColorMaskFemale), AccessTools.all);
                        break;
                }
            }

            return instructions;
        }
        /// <summary>
        /// Modifies the code for string replacement of mm_base, etc.
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaControl), "InitBaseCustomTextureBody")]
        public static IEnumerable<CodeInstruction> InitBaseCustomTextureBodyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            Logger.Log(LogLevel.Info, "InitBaseCustomTextureBodyTranspiler");
            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (var x in instructionsList)
            {
                if (x.operand?.ToString() == "chara/mm_base.unity3d")
                {
                    x.opcode = OpCodes.Call;
                    x.operand = typeof(KK_UncensorSelector).GetMethod(nameof(SetMMBase), AccessTools.all);
                }
            }

            return instructions;
        }
        //Color matching hooks
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetBodyBaseMaterial), null, null)]
        public static void SetBodyBaseMaterial(ChaControl __instance) => ColorMatchMaterials(__instance);
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload), null, null)]
        public static void Reload(ChaControl __instance) => ColorMatchMaterials(__instance);
        /// <summary>
        /// LineMask texture assigned to the material, toggled on and off for any color matching parts along with the body
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.VisibleAddBodyLine))]
        public static void VisibleAddBodyLine(ChaControl __instance)
        {
            UncensorData Uncensor = GetUncensorData(__instance);
            if (Uncensor == null)
                return;

            foreach (var ColorMatchPart in Uncensor.ColorMatchList)
            {
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(__instance.objBody.transform);
                GameObject gameObject = findAssist.GetObjectFromName(ColorMatchPart.Object);
                if (gameObject != null)
                    gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._linetexon, __instance.chaFile.custom.body.drawAddLine ? 1f : 0f);
            }
        }
        /// <summary>
        /// Skin gloss slider level, as assigned in the character maker.
        /// This corresponds to the red coloring in the DetailMask texture.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingSkinGlossPower))]
        public static void ChangeSettingSkinGlossPower(ChaControl __instance)
        {
            UncensorData Uncensor = GetUncensorData(__instance);
            if (Uncensor == null)
                return;

            foreach (var ColorMatchPart in Uncensor.ColorMatchList)
            {
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(__instance.objBody.transform);
                GameObject gameObject = findAssist.GetObjectFromName(ColorMatchPart.Object);
                if (gameObject != null)
                    gameObject.GetComponent<Renderer>().material.SetFloat(ChaShader._SpecularPower, Mathf.Lerp(__instance.chaFile.custom.body.skinGlossPower, 1f, __instance.chaFile.status.skinTuyaRate));
            }
        }
        /// <summary>
        /// For traps and futas, set the normals for the chest area. This prevents strange shadowing around flat-chested trap/futa characters
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomBodyWithoutCustomTexture))]
        public static void ChangeCustomBodyWithoutCustomTexture(ChaControl __instance)
        {
            UncensorData Uncensor = GetUncensorData(__instance);
            if (Uncensor == null)
                return;

            if (__instance.sex == 0 && __instance.hiPoly && Uncensor.FutaFix)
            {
                if (__instance.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlBody, out BustNormal bustNormal))
                    bustNormal.Release();

                bustNormal = new BustNormal();
                bustNormal.Init(__instance.objBody, Uncensor.OOBase, Uncensor.Normals == "" ? Defaults.Normals : Uncensor.Normals, string.Empty);
                __instance.dictBustNormal[ChaControl.BustNormalKind.NmlBody] = bustNormal;
            }
        }

        public class UncensorData
        {
            public string GUID;
            public string DisplayName;
            public string OOBase;
            public string MMBase;
            public string Normals;
            public bool Male = false;
            public bool Female = false;
            public bool FutaFix = false;
            public string BodyMainTex;
            public string BodyColorMask;
            public string AssetHighPoly;
            public string AssetLowPoly;
            public List<ColorMatchPart> ColorMatchList = new List<ColorMatchPart>();

            public UncensorData(XElement UncensorData)
            {
                GUID = UncensorData.Element("guid")?.Value;
                DisplayName = UncensorData.Element("displayName")?.Value;
                MMBase = UncensorData.Element("mm_base")?.Value;

                if (UncensorData.Element("sex")?.Value.ToLower() == "male" || UncensorData.Element("sex")?.Value.ToLower() == "0")
                    Male = true;
                else if (UncensorData.Element("sex")?.Value.ToLower() == "female" || UncensorData.Element("sex")?.Value.ToLower() == "1")
                    Female = true;
                else
                {
                    Male = true;
                    Female = true;
                }

                if (UncensorData.Element("futaFix")?.Value.ToLower() == "true")
                    FutaFix = true;

                XElement oo_base = UncensorData.Element("oo_base");
                if (oo_base != null)
                {
                    OOBase = oo_base.Element("file")?.Value;
                    AssetHighPoly = oo_base.Element("assetHighPoly")?.Value;
                    AssetLowPoly = oo_base.Element("assetLowPoly")?.Value;
                    BodyMainTex = oo_base.Element("mainTex")?.Value;
                    BodyColorMask = oo_base.Element("colorMask")?.Value;
                    Normals = oo_base.Element("normals")?.Value;

                    foreach (XElement colorMatch in oo_base.Elements("colorMatch"))
                    {
                        ColorMatchPart Part = new ColorMatchPart(colorMatch.Element("object")?.Value,
                                                                 colorMatch.Element("material")?.Value,
                                                                 colorMatch.Element("materialCreate")?.Value,
                                                                 colorMatch.Element("mainTex")?.Value,
                                                                 colorMatch.Element("colorMask")?.Value);
                        if (Part.Verify())
                            ColorMatchList.Add(Part);
                    }
                }

                //These things can be null if the XML doesn't exist or empty strings if it does exist but is left blank
                //Set everything to null/defaults for easier checks
                MMBase = MMBase.IsNullOrWhiteSpace() ? Defaults.MMBase : MMBase;
                OOBase = OOBase.IsNullOrWhiteSpace() ? Defaults.OOBase : OOBase;
                GUID = GUID.IsNullOrWhiteSpace() ? null : GUID;
                DisplayName = DisplayName.IsNullOrWhiteSpace() ? null : DisplayName;
                Normals = Normals.IsNullOrWhiteSpace() ? Defaults.Normals : Normals;
                BodyMainTex = BodyMainTex.IsNullOrWhiteSpace() ? Defaults.BodyMainTex : BodyMainTex;
                BodyColorMask = BodyColorMask.IsNullOrWhiteSpace() ? null : BodyColorMask;
                AssetHighPoly = AssetHighPoly.IsNullOrWhiteSpace() ? null : AssetHighPoly;
                AssetLowPoly = AssetLowPoly.IsNullOrWhiteSpace() ? null : AssetLowPoly;
            }

            public class ColorMatchPart
            {
                public string Object;
                public string Material;
                public string MaterialCreate;
                public string MainTex;
                public string ColorMask;

                public ColorMatchPart(string obj, string mat, string matCreate, string mainTex, string colorMask)
                {
                    Object = obj.IsNullOrWhiteSpace() ? null : obj;
                    Material = mat.IsNullOrWhiteSpace() ? null : mat;
                    MaterialCreate = matCreate.IsNullOrWhiteSpace() ? null : matCreate;
                    MainTex = mainTex.IsNullOrWhiteSpace() ? null : mainTex;
                    ColorMask = colorMask.IsNullOrWhiteSpace() ? null : colorMask;
                }

                public bool Verify() => Object == null || Material == null || MaterialCreate == null || MainTex == null || ColorMask == null ? false : true;
            }
        }

        public static class Defaults
        {
            public static readonly string OOBase = "chara/oo_base.unity3d";
            public static readonly string MMBase = "chara/mm_base.unity3d";
            public static readonly string AssetMale = "p_cm_body_00";
            public static readonly string AssetMaleLow = "p_cm_body_00_low";
            public static readonly string AssetFemale = "p_cf_body_00";
            public static readonly string AssetFemaleLow = "p_cf_body_00_low";
            public static readonly string BodyMainTex = "cf_body_00_t";
            public static readonly string BodyColorMaskMale = "cm_body_00_mc";
            public static readonly string BodyColorMaskFemale = "cf_body_00_mc";
            public static readonly string Normals = "p_cf_body_00_Nml";
        }
    }
}
