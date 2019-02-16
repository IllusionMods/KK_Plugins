using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using Manager;
using Sideloader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using UniRx;
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
        public const string PluginNameInternal = "KK_UncensorSelector";
        public const string Version = "2.0";
        private static ChaControl CurrentCharacter;
        private static ChaFileControl CurrentChaFile;
        private static readonly string UncensorSelectorPath = Path.Combine(Paths.PluginPath, PluginNameInternal);
        public static readonly Dictionary<string, UncensorData> UncensorDictionary = new Dictionary<string, UncensorData>();
        public static readonly List<string> UncensorList = new List<string>();
        public static readonly List<string> UncensorListDisplay = new List<string>();
        public static readonly string modDirectory = Path.Combine(Paths.GameRootPath, "mods");
        private static MakerDropdown UncensorDropdown;
        private static MakerToggle BallsToggle;
        private static bool DoUncensorDropdownEvents = false;
        private static bool DoingForcedReload = false;

        [DisplayName("Male uncensor display")]
        [Category("Config")]
        [Description("Which character maker to display uncensors")]
        [AcceptableValueList(new object[] { "Male", "Both" })]
        public static ConfigWrapper<string> MaleDisplay { get; private set; }
        [DisplayName("Female uncensor display")]
        [Category("Config")]
        [Description("Which character maker to display uncensors")]
        [AcceptableValueList(new object[] { "Female", "Both" })]
        public static ConfigWrapper<string> FemaleDisplay { get; private set; }
        [DisplayName("Trap uncensor display")]
        [Category("Config")]
        [Description("Which character maker to display uncensors")]
        [AcceptableValueList(new object[] { "Male", "Female", "Both" })]
        public static ConfigWrapper<string> TrapDisplay { get; private set; }
        [DisplayName("Futa uncensor display")]
        [Category("Config")]
        [Description("Which character maker to display uncensors")]
        [AcceptableValueList(new object[] { "Male", "Female", "Both" })]
        public static ConfigWrapper<string> FutaDisplay { get; private set; }
        [DisplayName("Enable Trap content")]
        [Category("Config")]
        [Description("Enable or disable all trap uncensors. Characters assigned to a trap uncensor will use the alternate uncensor as configured by the uncensor.")]
        public static ConfigWrapper<bool> EnableTraps { get; private set; }
        [DisplayName("Enable Futa content")]
        [Category("Config")]
        [Description("Enable or disable all futa uncensors. Characters assigned to a futa uncensor will use the alternate uncensor as configured by the uncensor.")]
        public static ConfigWrapper<bool> EnableFutas { get; private set; }
        [DisplayName("Default male uncensor")]
        [Category("Config")]
        [Description("GUID of the uncensor to use if character does not have one set.")]
        public static ConfigWrapper<string> DefaultMaleUncensor { get; private set; }
        [DisplayName("Default female uncensor")]
        [Category("Config")]
        [Description("GUID of the uncensor to use if character does not have one set.")]
        public static ConfigWrapper<string> DefaultFemaleUncensor { get; private set; }

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_UncensorSelector));

            MethodInfo ChaControlInit = typeof(ChaControl).GetMethod("Initialize");
            harmony.Patch(ChaControlInit, new HarmonyMethod(typeof(KK_UncensorSelector).GetMethod(nameof(InitializePrefix), BindingFlags.Static | BindingFlags.Public)), null);

            Type LoadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).Where(x => x.Name.StartsWith("<LoadAsync>c__Iterator")).First();
            MethodInfo LoadAsyncIteratorMoveNext = LoadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(LoadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(KK_UncensorSelector).GetMethod(nameof(LoadAsyncTranspiler), BindingFlags.Static | BindingFlags.Public)));

            GenerateUncensorList();

            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerExiting += (object sender, EventArgs e) => DoUncensorDropdownEvents = false;
            CharacterApi.RegisterExtraBehaviour<UncensorSelectorController>(GUID);

            MaleDisplay = new ConfigWrapper<string>("MaleDisplay", PluginNameInternal, "Male");
            FemaleDisplay = new ConfigWrapper<string>("FemaleDisplay", PluginNameInternal, "Female");
            TrapDisplay = new ConfigWrapper<string>("TrapDisplay", PluginNameInternal, "Both");
            FutaDisplay = new ConfigWrapper<string>("FutaDisplay", PluginNameInternal, "Both");
            EnableTraps = new ConfigWrapper<bool>("EnableTraps", PluginNameInternal, true);
            EnableFutas = new ConfigWrapper<bool>("EnableFutas", PluginNameInternal, true);
            DefaultMaleUncensor = new ConfigWrapper<string>("DefaultMaleUncensor", PluginNameInternal, "None");
            DefaultFemaleUncensor = new ConfigWrapper<string>("DefaultFemaleUncensor", PluginNameInternal, "None");
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            UncensorList.Clear();
            UncensorListDisplay.Clear();

            UncensorList.Add("None");
            UncensorListDisplay.Add("None");

            foreach (UncensorData Uncensor in UncensorDictionary.Select(x => x.Value))
            {
                if (UncensorAllowedInMaker(Uncensor.Gender, (byte)MakerAPI.GetMakerSex()))
                {
                    UncensorList.Add(Uncensor.GUID);
                    UncensorListDisplay.Add($"[{Uncensor.Gender.ToString()}]{Uncensor.DisplayName}");
                }
            }

            UncensorDropdown = e.AddControl(new MakerDropdown("Uncensor", UncensorListDisplay.ToArray(), MakerConstants.Body.All, 0, this));
            UncensorDropdown.ValueChanged.Subscribe(Observer.Create<int>(UncensorDropdownChanged));
            void UncensorDropdownChanged(int UncensorID)
            {
                if (DoUncensorDropdownEvents == false)
                {
                    DoUncensorDropdownEvents = true;
                    return;
                }

                DoingForcedReload = true;
                MakerAPI.GetMakerBase().chaCtrl.Reload(true, true, true, false);
                DoingForcedReload = false;
                SetBallsVisibility(MakerAPI.GetMakerBase().chaCtrl, BallsToggle.Value);
            }

            BallsToggle = e.AddControl(new MakerToggle(MakerConstants.Body.All, "Display balls", this));
            BallsToggle.ValueChanged.Subscribe(Observer.Create<bool>(BallsToggleChanged));
            BallsToggle.Value = MakerAPI.GetMakerSex() == 0 ? true : false;
            void BallsToggleChanged(bool value) => SetBallsVisibility(MakerAPI.GetMakerBase().chaCtrl, BallsToggle.Value);
        }

        public static void SetBallsVisibility(ChaControl Character, bool Visible)
        {
            var balls = Character?.gameObject?.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(x => x?.name == "o_dan_f").FirstOrDefault();
            if (balls != null)
                balls.gameObject.GetComponent<Renderer>().enabled = Visible;
        }
        #region Uncensor Update
        private static void ReloadCharacterUncensor(ChaControl chaControl)
        {
            UncensorData Uncensor = GetUncensorData(chaControl);
            bool temp = chaControl.fileStatus.visibleSonAlways;

            UpdateUncensor(chaControl, Uncensor);
            UpdateSkin(chaControl, Uncensor);
            SetChestNormals(chaControl, Uncensor);
            ColorMatchMaterials(chaControl, Uncensor);

            if (StudioAPI.InsideStudio)
                chaControl.fileStatus.visibleSonAlways = temp;
            else if (Uncensor == null)
                if (chaControl.sex == 1)
                    chaControl.fileStatus.visibleSonAlways = false;
                else
                    chaControl.fileStatus.visibleSonAlways = true;
            else
                chaControl.fileStatus.visibleSonAlways = Uncensor.ShowPenis;
        }
        /// <summary>
        /// Create a copy of the character with the new uncensor, copy its mesh, and delete it
        /// </summary>
        private static void UpdateUncensor(ChaControl chaControl, UncensorData Uncensor)
        {
            ChaControl chaControlTemp = Singleton<Character>.Instance.CreateChara(chaControl.sex, null, 9846215, chaControl.chaFile, chaControl.hiPoly);
            chaControlTemp.Load(false);
            foreach (var mesh in chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (mesh.name == "o_body_a")
                    UpdateMeshRenderer(chaControlTemp.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(x => x.name == mesh.name)?.FirstOrDefault(), mesh, true);
                else if (mesh.name == "o_dankon" || mesh.name == "o_dan_f" || mesh.name == "o_mnpa" || mesh.name == "o_mnpb")
                    UpdateMeshRenderer(chaControlTemp.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(x => x.name == mesh.name)?.FirstOrDefault(), mesh, true);
                else if (Uncensor != null)
                    foreach (var part in Uncensor.ColorMatchList)
                        if (mesh.name == part.Object)
                            UpdateMeshRenderer(chaControlTemp.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().Where(x => x.name == part.Object)?.FirstOrDefault(), mesh, true);
            }

            Singleton<Character>.Instance.DeleteChara(chaControlTemp);
            CurrentCharacter = chaControl;
        }
        /// <summary>
        /// Update the character's skin textures
        /// </summary>
        private static void UpdateSkin(ChaControl chaControl, UncensorData Uncensor)
        {
            int num = chaControl.hiPoly ? 2048 : 512;
            string mat = chaControl.sex == 0 ? "cm_m_body" : "cf_m_body";
            string mmbase = Uncensor?.MMBase ?? Defaults.MMBase;
            chaControl.customTexCtrlBody.Initialize(mmbase, mat, string.Empty, mmbase, "cf_m_body_create", string.Empty, num, num, RenderTextureFormat.ARGB32);

            chaControl.AddUpdateCMBodyTexFlags(true, true, true, true, true);
            chaControl.AddUpdateCMBodyColorFlags(true, true, true, true, true, true);
            chaControl.AddUpdateCMBodyLayoutFlags(true, true);
            chaControl.SetBodyBaseMaterial();
            chaControl.CreateBodyTexture();
            chaControl.ChangeCustomBodyWithoutCustomTexture();
        }
        /// <summary>
        /// Copy the mesh from one SkinnedMeshRenderer to another
        /// </summary>
        private static void UpdateMeshRenderer(SkinnedMeshRenderer src, SkinnedMeshRenderer dst, bool copyMaterials = false)
        {
            if (src == null || dst == null)
                return;

            //Copy the mesh
            dst.sharedMesh = src.sharedMesh;

            Transform[] originalBones = dst.bones;

            //Sort the bones
            List<Transform> newBones = new List<Transform>();
            for (int boneOrder = 0; boneOrder < src.bones.Length; boneOrder++)
            {
                try
                {
                    newBones.Add(Array.Find(originalBones, c => c.name == src.bones[boneOrder].name));
                }
                catch { }
            }
            dst.bones = newBones.ToArray();

            if (copyMaterials)
                dst.materials = src.materials;
        }
        /// <summary>
        /// Do color matching for every object configured in the manifest.xml
        /// </summary>
        public static void ColorMatchMaterials(ChaControl __instance, UncensorData Uncensor)
        {
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
        public static void SetChestNormals(ChaControl chaControl, UncensorData Uncensor)
        {
            if (chaControl.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlBody, out BustNormal bustNormal))
                bustNormal.Release();

            bustNormal = new BustNormal();
            bustNormal.Init(chaControl.objBody, Uncensor?.OOBase ?? Defaults.OOBase, Uncensor?.Normals ?? Defaults.Normals, string.Empty);
            chaControl.dictBustNormal[ChaControl.BustNormalKind.NmlBody] = bustNormal;
        }
        #endregion
        /// <summary>
        /// Read all the manifest.xml files and generate a dictionary of uncensors
        /// </summary>
        private void GenerateUncensorList()
        {
            var manifests = AccessTools.Field(typeof(Sideloader.Sideloader), "LoadedManifests").GetValue(GetComponent<Sideloader.Sideloader>()) as List<Manifest>;
            foreach (var manifest in manifests)
            {
                XDocument ManifestDocument = AccessTools.Field(typeof(Manifest), "manifestDocument").GetValue(manifest) as XDocument;
                XElement UncensorSelectorElement = ManifestDocument.Root?.Element(PluginNameInternal);
                if (UncensorSelectorElement != null && UncensorSelectorElement.HasElements)
                {
                    foreach (XElement UncensorElement in UncensorSelectorElement.Elements("uncensor"))
                    {
                        UncensorData Uncensor = new UncensorData(UncensorElement);
                        if (Uncensor.GUID == null)
                        {
                            Logger.Log(LogLevel.Warning, "Uncensor failed to load due to missing GUID.");
                            continue;
                        }
                        if (Uncensor.DisplayName == null)
                        {
                            Logger.Log(LogLevel.Warning, "Uncensor failed to load due to missing display name.");
                            continue;
                        }
                        if (Uncensor.OOBase == Defaults.OOBase)
                        {
                            Logger.Log(LogLevel.Warning, "Uncensor was not loaded because oo_base is the default.");
                            continue;
                        }
                        UncensorDictionary.Add(Uncensor.GUID, Uncensor);
                    }
                }
            }
        }
        /// <summary>
        /// Get the UncensorData for the specified character
        /// </summary>
        private static UncensorData GetUncensorData(ChaControl Character)
        {
            try
            {
                UncensorData Uncensor = null;

                if (MakerAPI.InsideAndLoaded && DoingForcedReload)
                    return SelectedUncensor;
                else if (Character?.chaFile == null && CurrentChaFile != null)
                {
                    //ChaFile hasn't been initialized yet, get the one set by the ChaControl.Initialize hook
                    PluginData ExtendedData = ExtendedSave.GetExtendedDataById(CurrentChaFile, GUID);
                    if (ExtendedData != null)
                    {
                        if (ExtendedData.data.TryGetValue("UncensorGUID", out var UncensorGUID))
                        {
                            if (UncensorGUID == null)
                                Uncensor = null;

                            if (UncensorDictionary.TryGetValue(UncensorGUID.ToString(), out var uncensorData))
                                Uncensor = uncensorData;
                        }
                    }
                }
                else if (MakerAPI.InsideAndLoaded)
                {
                    if (GetController(Character)?.UncensorGUID != null)
                    {
                        if (UncensorDictionary.TryGetValue(GetController(Character).UncensorGUID, out var uncensorData))
                        {
                            Uncensor = uncensorData;
                        }
                    }
                }
                else
                {
                    PluginData ExtendedData = ExtendedSave.GetExtendedDataById(Character.chaFile, GUID);
                    if (ExtendedData != null)
                    {
                        if (ExtendedData.data.TryGetValue("UncensorGUID", out var UncensorGUID))
                        {
                            if (UncensorGUID == null)
                                Uncensor = null;

                            if (UncensorDictionary.TryGetValue(UncensorGUID.ToString(), out var uncensorData))
                            {
                                Uncensor = uncensorData;
                            }
                        }
                    }
                }

                if (Character?.chaFile != null)
                {
                    //If the uncensor is a trap or futa uncensor and those are disabled get the alternate uncensor if one has been configured
                    if (Uncensor != null)
                    {
                        if (Uncensor.Gender == Gender.Trap && !EnableTraps.Value)
                        {
                            if (Character.sex == 0 && Uncensor.MaleAlternate != null)
                                if (UncensorDictionary.TryGetValue(Uncensor.MaleAlternate, out UncensorData AlternateUncensor))
                                    Uncensor = AlternateUncensor;
                            if (Character.sex == 1 && Uncensor.FemaleAlternate != null)
                                if (UncensorDictionary.TryGetValue(Uncensor.FemaleAlternate, out UncensorData AlternateUncensor))
                                    Uncensor = AlternateUncensor;
                        }
                        else if (Uncensor.Gender == Gender.Futa && !EnableFutas.Value)
                        {
                            if (Character.sex == 0 && Uncensor.MaleAlternate != null)
                                if (UncensorDictionary.TryGetValue(Uncensor.MaleAlternate, out UncensorData AlternateUncensor))
                                    Uncensor = AlternateUncensor;
                            if (Character.sex == 1 && Uncensor.FemaleAlternate != null)
                                if (UncensorDictionary.TryGetValue(Uncensor.FemaleAlternate, out UncensorData AlternateUncensor))
                                    Uncensor = AlternateUncensor;
                        }
                    }

                    //If no uncensor has been found get the default uncensor
                    if (!MakerAPI.InsideMaker)
                        if (Uncensor == null && UncensorDictionary.TryGetValue(Character.sex == 0 ? DefaultMaleUncensor.Value : DefaultFemaleUncensor.Value, out UncensorData DefaultUncensor))
                            Uncensor = DefaultUncensor;
                }

                return Uncensor;
            }
            catch { }
            return null;
        }
        /// <summary>
        /// Check if the uncensor is permitted in the character maker
        /// </summary>
        public static bool UncensorAllowedInMaker(Gender UncensorGender, byte CharacterSex)
        {
            if (UncensorGender == Gender.Male && (MaleDisplay.Value == "Both" || (CharacterSex == 0 && MaleDisplay.Value == "Male")))
                return true;
            else if (UncensorGender == Gender.Female && (FemaleDisplay.Value == "Both" || (CharacterSex == 1 && FemaleDisplay.Value == "Female")))
                return true;
            else if (UncensorGender == Gender.Trap)
            {
                bool ShowTraps = EnableTraps.Value;
                if (ShowTraps)
                {
                    if (TrapDisplay.Value == "Both")
                        ShowTraps = true;
                    else if (CharacterSex == 0 && TrapDisplay.Value == "Male")
                        ShowTraps = true;
                    else if (CharacterSex == 1 && TrapDisplay.Value == "Female")
                        ShowTraps = true;
                    else
                        ShowTraps = false;
                }
                return ShowTraps;
            }
            else if (UncensorGender == Gender.Futa)
            {
                bool ShowFutas = EnableFutas.Value;
                if (ShowFutas)
                {
                    if (TrapDisplay.Value == "Both")
                        ShowFutas = true;
                    else if (CharacterSex == 0 && TrapDisplay.Value == "Male")
                        ShowFutas = true;
                    else if (CharacterSex == 1 && TrapDisplay.Value == "Female")
                        ShowFutas = true;
                    else
                        ShowFutas = false;
                }
                return ShowFutas;
            }
            else
                return false;
        }
        private static UncensorSelectorController GetController(ChaControl Character) => Character?.gameObject.GetComponent<UncensorSelectorController>();
        private static string SetOOBase() => GetUncensorData(CurrentCharacter)?.OOBase ?? Defaults.OOBase;
        private static string SetNormals() => GetUncensorData(CurrentCharacter)?.Normals ?? Defaults.Normals;
        private static string SetBodyMainTex() => GetUncensorData(CurrentCharacter)?.BodyMainTex ?? Defaults.BodyMainTex;
        private static string SetMaleBodyLow() => SetBodyAsset(0, false);
        private static string SetMaleBodyHigh() => SetBodyAsset(0, true);
        private static string SetFemaleBodyLow() => SetBodyAsset(1, false);
        private static string SetFemaleBodyHigh() => SetBodyAsset(1, true);
        private static string SetBodyAsset(byte sex, bool HiPoly) =>
            HiPoly ? (GetUncensorData(CurrentCharacter)?.AssetHighPoly ?? (sex == 0 ? Defaults.AssetMale : Defaults.AssetFemale))
                   : (GetUncensorData(CurrentCharacter)?.AssetLowPoly ?? (sex == 0 ? Defaults.AssetMaleLow : Defaults.AssetFemaleLow));
        private static string SetMMBase() => GetUncensorData(CurrentCharacter)?.MMBase ?? Defaults.MMBase;
        private static string SetBodyColorMaskMale() => SetColorMask(0);
        private static string SetBodyColorMaskFemale() => SetColorMask(1);
        private static string SetColorMask(byte sex) => GetUncensorData(CurrentCharacter)?.BodyColorMask ?? (sex == 0 ? Defaults.BodyColorMaskMale : Defaults.BodyColorMaskFemale);
        public static UncensorData SelectedUncensor => MakerAPI.InsideAndLoaded ? UncensorDropdown.Value == 0 ? null : UncensorDictionary[UncensorList[UncensorDropdown.Value]] : null;

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
        public static void CreateBodyTexturePrefix(ChaControl __instance) => CurrentCharacter = __instance;
        public static void InitializePrefix(ChaControl __instance, ChaFileControl _chaFile)
        {
            CurrentCharacter = __instance;
            CurrentChaFile = _chaFile;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadAsync))]
        public static void LoadAsyncPrefix(ChaControl __instance) => CurrentCharacter = __instance;
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "InitBaseCustomTextureBody")]
        public static void InitBaseCustomTextureBodyPrefix(ChaControl __instance) => CurrentCharacter = __instance;
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
        /// <summary>
        /// Do color matching
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetBodyBaseMaterial), null, null)]
        public static void SetBodyBaseMaterial(ChaControl __instance) => ColorMatchMaterials(__instance, GetUncensorData(__instance));
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload), null, null)]
        public static void Reload(ChaControl __instance, bool noChangeBody)
        {
            if (noChangeBody)
                return;

            if (MakerAPI.InsideAndLoaded && !DoingForcedReload)
                return;

            ReloadCharacterUncensor(__instance);
        }
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

            SetChestNormals(__instance, Uncensor);

            if (!StudioAPI.InsideStudio && !MakerAPI.InsideMaker && Uncensor != null)
                __instance.fileStatus.visibleSonAlways = Uncensor.ShowPenis;
        }

        public enum Gender { Male, Female, Trap, Futa }
        public enum BodyType { Male, Female }

        public class UncensorData
        {
            public string GUID;
            public string DisplayName;
            public string OOBase;
            public string MMBase;
            public string Normals;
            public Gender Gender = Gender.Male;
            public BodyType BodyType = BodyType.Male;
            public bool ShowPenis = false;
            public string BodyMainTex;
            public string BodyColorMask;
            public string AssetHighPoly;
            public string AssetLowPoly;
            public string MaleAlternate;
            public string FemaleAlternate;
            public List<ColorMatchPart> ColorMatchList = new List<ColorMatchPart>();

            public UncensorData(XElement UncensorData)
            {
                GUID = UncensorData.Element("guid")?.Value;
                DisplayName = UncensorData.Element("displayName")?.Value;
                MMBase = UncensorData.Element("mm_base")?.Value;

                if (UncensorData.Element("gender")?.Value?.ToLower() == "male")
                    Gender = Gender.Male;
                else if (UncensorData.Element("gender")?.Value?.ToLower() == "female")
                    Gender = Gender.Female;
                else if (UncensorData.Element("gender")?.Value?.ToLower() == "trap")
                    Gender = Gender.Trap;
                else if (UncensorData.Element("gender")?.Value?.ToLower() == "futa")
                    Gender = Gender.Futa;

                if (UncensorData.Element("bodyType")?.Value?.ToLower() == "female")
                    BodyType = BodyType.Female;
                if (UncensorData.Element("showPenis")?.Value?.ToLower() == "true" || UncensorData.Element("showPenis")?.Value?.ToLower() == "1")
                    ShowPenis = true;

                XElement oo_base = UncensorData.Element("oo_base");
                if (oo_base != null)
                {
                    OOBase = oo_base.Element("file")?.Value;
                    AssetHighPoly = oo_base.Element("assetHighPoly")?.Value;
                    AssetLowPoly = oo_base.Element("assetLowPoly")?.Value;
                    BodyMainTex = oo_base.Element("mainTex")?.Value;
                    BodyColorMask = oo_base.Element("colorMask")?.Value;
                    Normals = oo_base.Element("normals")?.Value;
                    MaleAlternate = oo_base.Element("maleAlternate")?.Value;
                    FemaleAlternate = oo_base.Element("femaleAlternate")?.Value;

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

        public class UncensorSelectorController : CharaCustomFunctionController
        {
            internal bool DisplayBalls { get; set; }
            internal string UncensorGUID { get; set; }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                if (currentGameMode == GameMode.Maker)
                {
                    DisplayBalls = BallsToggle.Value;
                    UncensorGUID = SelectedUncensor?.GUID;
                }

                var data = new PluginData();
                data.data.Add("DisplayBalls", DisplayBalls);
                data.data.Add("UncensorGUID", UncensorGUID);
                data.version = 1;
                SetExtendedData(data);
            }

            protected override void OnReload(GameMode currentGameMode)
            {
                DisplayBalls = ChaControl.sex == 0 ? true : false;
                UncensorGUID = null;

                var data = GetExtendedData();
                if (data != null)
                {
                    if (data.data.TryGetValue("DisplayBalls", out var LoadedDisplayBalls))
                    {
                        DisplayBalls = (bool)LoadedDisplayBalls;
                    }
                    if (data.data.TryGetValue("UncensorGUID", out var LoadedUncensorGUID) && LoadedUncensorGUID != null)
                    {
                        UncensorGUID = LoadedUncensorGUID.ToString();
                        if (UncensorGUID.IsNullOrWhiteSpace())
                            UncensorGUID = null;
                    }
                }

                if (MakerAPI.InsideAndLoaded)
                {

                    if (MakerAPI.GetCharacterLoadFlags().Body)
                    {
                        //Change the UI settings which will update the character's uncensor

                        if (UncensorList.IndexOf(UncensorGUID) == -1)
                        {
                            //The loaded uncensor isn't on the list, possibly due to being forbidden
                            UncensorDropdown.Value = 0;
                            UncensorGUID = null;
                            if (ChaControl.sex == 1)
                                DisplayBalls = false;
                        }
                        else
                        {
                            UncensorDropdown.Value = UncensorList.IndexOf(UncensorGUID) == -1 ? 0 : UncensorList.IndexOf(UncensorGUID);
                            BallsToggle.Value = DisplayBalls;
                        }
                    }
                    else
                    {
                        //Set the uncensor stuff to whatever is set in the maker
                        UncensorGUID = UncensorDropdown.Value == 0 ? null : UncensorList[UncensorDropdown.Value];
                        DisplayBalls = BallsToggle.Value;
                    }
                    if (UncensorList.IndexOf(UncensorGUID) == -1 || GetUncensorData(ChaControl) == null)
                        ChaControl.fileStatus.visibleSonAlways = ChaControl.sex == 0 ? true : false;
                    else
                        ChaControl.fileStatus.visibleSonAlways = GetUncensorData(ChaControl).ShowPenis;
                }
                else
                {
                    SetBallsVisibility(ChaControl, DisplayBalls);
                }

                //if (CurrentChaFile == null && !MakerAPI.InsideMaker)
                //{
                //    CurrentChaFile = ChaControl.chaFile;
                //    ChaControl.Reload(true, true, true, false);
                //}
            }
        }
    }
}