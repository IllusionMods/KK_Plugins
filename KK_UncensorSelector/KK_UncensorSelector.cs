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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UniRx;
using UnityEngine;
using static Sideloader.Sideloader;
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
        public const string Version = "2.5.1";
        private const string UncensorKeyRandom = "Random";
        internal static ChaControl CurrentCharacter;
        internal static ChaFileControl CurrentChaFile;
        public static readonly Dictionary<string, UncensorData> UncensorDictionary = new Dictionary<string, UncensorData>();
        public static readonly List<string> UncensorList = new List<string>();
        public static readonly List<string> UncensorListDisplay = new List<string>();
        public static readonly List<string> UncensorListFull = new List<string>();
        private static MakerDropdown UncensorDropdown;
        private static MakerToggle BallsToggle;
        internal static bool DoUncensorDropdownEvents = false;
        internal static bool DoingForcedReload = false;
        private static readonly HashSet<string> BodyParts = new HashSet<string>() { "o_dankon", "o_dan_f", "o_gomu", "o_mnpa", "o_mnpb", "o_shadowcaster" };

        #region Config
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
        [AcceptableValueList(nameof(GenerateUncensorList))]
        public static ConfigWrapper<string> DefaultMaleUncensor { get; private set; }
        [DisplayName("Default female uncensor")]
        [Category("Config")]
        [Description("GUID of the uncensor to use if character does not have one set.")]
        [AcceptableValueList(nameof(GenerateUncensorList))]
        public static ConfigWrapper<string> DefaultFemaleUncensor { get; private set; }
        #endregion

        private void Start()
        {
            if (KoikatuAPI.CheckIncompatiblePlugin(this, "koikatsu.cartoonuncensor", LogLevel.Error))
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "CartoonUncensor.dll is incompatible with KK_UncensorSelector! Please remove it and restart the game.");
                return;
            }
            if (KoikatuAPI.CheckIncompatiblePlugin(this, "koikatsu.alexaebubblegum", LogLevel.Error))
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "AlexaeBubbleGum.dll is incompatible with KK_UncensorSelector! Please remove it and restart the game.");
                return;
            }

            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(Hooks));

            MethodInfo chaControlInit = typeof(ChaControl).GetMethod("Initialize");
            harmony.Patch(chaControlInit, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.InitializePrefix), BindingFlags.Static | BindingFlags.Public)), null);

            Type loadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).First(x => x.Name.StartsWith("<LoadAsync>c__Iterator"));
            MethodInfo loadAsyncIteratorMoveNext = loadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(loadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.LoadAsyncTranspiler), BindingFlags.Static | BindingFlags.Public)));

            GenerateUncensorList();

            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerExiting += (s, e) => DoUncensorDropdownEvents = false;
            CharacterApi.RegisterExtraBehaviour<UncensorSelectorController>(GUID);

            MaleDisplay = new ConfigWrapper<string>("MaleDisplay", PluginNameInternal, "Male");
            FemaleDisplay = new ConfigWrapper<string>("FemaleDisplay", PluginNameInternal, "Female");
            TrapDisplay = new ConfigWrapper<string>("TrapDisplay", PluginNameInternal, "Both");
            FutaDisplay = new ConfigWrapper<string>("FutaDisplay", PluginNameInternal, "Both");
            EnableTraps = new ConfigWrapper<bool>("EnableTraps", PluginNameInternal, true);
            EnableFutas = new ConfigWrapper<bool>("EnableFutas", PluginNameInternal, true);
            DefaultMaleUncensor = new ConfigWrapper<string>("DefaultMaleUncensor", PluginNameInternal, UncensorKeyRandom);
            DefaultFemaleUncensor = new ConfigWrapper<string>("DefaultFemaleUncensor", PluginNameInternal, UncensorKeyRandom);
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            UncensorList.Clear();
            UncensorListDisplay.Clear();

            UncensorList.Add("Default");
            UncensorListDisplay.Add("Default");

            foreach (UncensorData uncensor in UncensorDictionary.Select(x => x.Value))
            {
                if (UncensorAllowedInMaker(uncensor.Gender, (byte)MakerAPI.GetMakerSex()))
                {
                    UncensorList.Add(uncensor.UncensorGUID);
                    UncensorListDisplay.Add($"[{uncensor.Gender.ToString()}]{uncensor.DisplayName}");
                }
            }

            UncensorDropdown = e.AddControl(new MakerDropdown("Uncensor", UncensorListDisplay.ToArray(), MakerConstants.Body.All, 0, this));
            UncensorDropdown.ValueChanged.Subscribe(Observer.Create<int>(UncensorDropdownChanged));
            void UncensorDropdownChanged(int uncensorID)
            {
                if (DoUncensorDropdownEvents == false)
                {
                    DoUncensorDropdownEvents = true;
                    return;
                }

                GetController(MakerAPI.GetMakerBase().chaCtrl).UncensorGUID = UncensorDropdown.Value == 0 ? null : SelectedUncensor.UncensorGUID;
                DoingForcedReload = true;
                MakerAPI.GetMakerBase().chaCtrl.Reload(true, true, true, false);
                DoingForcedReload = false;
                SetBallsVisibility(MakerAPI.GetMakerBase().chaCtrl, BallsToggle.Value);
            }

            BallsToggle = e.AddControl(new MakerToggle(MakerConstants.Body.All, "Display balls", this));
            BallsToggle.ValueChanged.Subscribe(Observer.Create<bool>(BallsToggleChanged));
            BallsToggle.Value = MakerAPI.GetMakerSex() == 0;
            void BallsToggleChanged(bool value)
            {
                SetBallsVisibility(MakerAPI.GetMakerBase().chaCtrl, BallsToggle.Value);
                GetController(MakerAPI.GetMakerBase().chaCtrl).DisplayBalls = BallsToggle.Value;
            }
            e.AddControl(new MakerText("Warning: Your selected default uncensor will not be displayed in maker, but it will be used elsewhere.", MakerConstants.Body.All, this) { TextColor = Color.yellow });
            e.AddControl(new MakerText("Warning: Some uncensors might not be displayed fully in maker, but they will work correctly elsewhere.", MakerConstants.Body.All, this) { TextColor = Color.yellow });
        }

        public static void SetBallsVisibility(ChaControl character, bool visible)
        {
            SkinnedMeshRenderer balls = character?.gameObject?.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x?.name == "o_dan_f");
            if (balls != null)
                balls.gameObject.GetComponent<Renderer>().enabled = visible;
        }
        #region Uncensor Update
        internal static void ReloadCharacterUncensor(ChaControl chaControl, bool updateMesh = true)
        {
            UncensorData uncensor = GetUncensorData(chaControl);
            bool temp = chaControl.fileStatus.visibleSonAlways;

            if (updateMesh)
                UpdateUncensor(chaControl, uncensor);
            UpdateSkin(chaControl, uncensor);
            SetChestNormals(chaControl, uncensor);
            ColorMatchMaterials(chaControl, uncensor);

            chaControl.customMatBody.SetTexture(ChaShader._AlphaMask, Traverse.Create(chaControl).Property("texBodyAlphaMask").GetValue() as Texture);
            Traverse.Create(chaControl).Property("updateAlphaMask").SetValue(true);

            if (StudioAPI.InsideStudio)
                chaControl.fileStatus.visibleSonAlways = temp;
            else if (uncensor == null)
                chaControl.fileStatus.visibleSonAlways = chaControl.sex == 0;
            else
                chaControl.fileStatus.visibleSonAlways = uncensor.ShowPenis;
        }
        /// <summary>
        /// Load the body asset, copy its mesh, and delete it
        /// </summary>
        private static void UpdateUncensor(ChaControl chaControl, UncensorData uncensor)
        {
            string OOBase = uncensor?.OOBase ?? Defaults.OOBase;
            string Asset = uncensor?.Asset ?? (chaControl.sex == 0 ? Defaults.AssetMale : Defaults.AssetFemale);

            GameObject uncensorCopy = CommonLib.LoadAsset<GameObject>(OOBase, Asset, true);
            foreach (var mesh in chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (mesh.name == "o_body_a")
                    UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh);
                else if (BodyParts.Contains(mesh.name))
                    UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh, true);
                else if (uncensor != null)
                    foreach (var part in uncensor.ColorMatchList)
                        if (mesh.name == part.Object)
                            UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(x => x.name == part.Object), mesh, true);
            }
            Destroy(uncensorCopy);
        }

        /// <summary>
        /// Rebuild the character's skin textures
        /// </summary>
        private static void UpdateSkin(ChaControl chaControl, UncensorData uncensor)
        {
            int num = chaControl.hiPoly ? 2048 : 512;
            string mm_base = uncensor?.MMBase ?? Defaults.MMBase;
            string mat = SetBodyMaterial(chaControl.sex, uncensor);
            string matCreate = uncensor?.BodyMaterialCreate ?? Defaults.BodyMaterialCreate;

            chaControl.customTexCtrlBody.Initialize(mm_base, mat, string.Empty, mm_base, matCreate, string.Empty, num, num);
            chaControl.AddUpdateCMBodyTexFlags(true, true, true, true, true);
            chaControl.AddUpdateCMBodyColorFlags(true, true, true, true, true, true);
            chaControl.AddUpdateCMBodyLayoutFlags(true, true);
            chaControl.SetBodyBaseMaterial();
            chaControl.CreateBodyTexture();
            chaControl.ChangeCustomBodyWithoutCustomTexture();
        }
        /// <summary>
        /// Copy the mesh from one SkinnedMeshRenderer to another. If there is a significant mismatch in the number of bones
        /// this will fail horribly and create abominations. Verify the uncensor body has the proper number of bones in such a case.
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
            foreach (Transform t in src.bones)
            {
                try
                {
                    newBones.Add(Array.Find(originalBones, c => c.name == t.name));
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
        public static void ColorMatchMaterials(ChaControl chaControl, UncensorData uncensor)
        {
            if (uncensor == null)
                return;

            foreach (var colorMatchPart in uncensor.ColorMatchList)
            {
                //get main tex
                Texture2D mainTexture = CommonLib.LoadAsset<Texture2D>(uncensor.OOBase, colorMatchPart.MainTex, false, string.Empty);
                if (mainTexture == null)
                    continue;

                //get color mask
                Texture2D colorMask = CommonLib.LoadAsset<Texture2D>(uncensor.OOBase, colorMatchPart.ColorMask, false, string.Empty);
                if (colorMask == null)
                    continue;

                //find the game object
                FindAssist findAssist = new FindAssist();
                findAssist.Initialize(chaControl.objBody.transform);
                GameObject gameObject = findAssist.GetObjectFromName(colorMatchPart.Object);
                if (gameObject == null)
                    continue;

                var customTex = new CustomTextureControl(gameObject.transform);
                customTex.Initialize(uncensor.OOBase, colorMatchPart.Material, string.Empty, uncensor.OOBase, colorMatchPart.MaterialCreate, string.Empty, 2048, 2048);

                customTex.SetMainTexture(mainTexture);
                customTex.SetColor(ChaShader._Color, chaControl.chaFile.custom.body.skinMainColor);

                customTex.SetTexture(ChaShader._ColorMask, colorMask);
                customTex.SetColor(ChaShader._Color2, chaControl.chaFile.custom.body.skinSubColor);

                //set the new texture
                var newTex = customTex.RebuildTextureAndSetMaterial();
                if (newTex == null)
                    continue;

                Material mat = gameObject.GetComponent<Renderer>().material;
                var mt = mat.GetTexture(ChaShader._MainTex);
                mat.SetTexture(ChaShader._MainTex, newTex);
                //Destroy the old texture to prevent memory leak
                Destroy(mt);
            }
        }
        /// <summary>
        /// Set the normals for the character's chest. This fixes the shadowing for small-chested characters.
        /// By default it is not applied to males so we do it manually for all characters in case the male is using a female body.
        /// </summary>
        public static void SetChestNormals(ChaControl chaControl, UncensorData uncensor)
        {
            if (chaControl.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlBody, out BustNormal bustNormal))
                bustNormal.Release();

            bustNormal = new BustNormal();
            bustNormal.Init(chaControl.objBody, uncensor?.OOBase ?? Defaults.OOBase, uncensor?.Normals ?? Defaults.Normals, string.Empty);
            chaControl.dictBustNormal[ChaControl.BustNormalKind.NmlBody] = bustNormal;
        }
        #endregion
        /// <summary>
        /// Read all the manifest.xml files and generate a dictionary of uncensors
        /// </summary>
        private object[] GenerateUncensorList()
        {
            if (LoadedManifests == null)
                return null;

            if (UncensorListFull.Count > 0)
                return UncensorListFull.ToArray();

            UncensorListFull.Add("None (censored)");
            UncensorListFull.Add(UncensorKeyRandom);

            foreach (var manifest in LoadedManifests)
            {
                XDocument manifestDocument = manifest.manifestDocument;
                XElement uncensorSelectorElement = manifestDocument?.Root?.Element(PluginNameInternal);
                if (uncensorSelectorElement != null && uncensorSelectorElement.HasElements)
                {
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("uncensor"))
                    {
                        UncensorData uncensor = new UncensorData(uncensorElement);
                        if (uncensor.UncensorGUID == null)
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
                        UncensorDictionary.Add(uncensor.UncensorGUID, uncensor);
                        UncensorListFull.Add(uncensor.UncensorGUID);
                    }
                }
            }

            return UncensorListFull.ToArray();
        }
        /// <summary>
        /// Get the UncensorData for the specified character
        /// </summary>
        public static UncensorData GetUncensorData(ChaControl character)
        {
            try
            {
                UncensorData uncensor = null;

                if (MakerAPI.InsideAndLoaded && DoingForcedReload)
                    return SelectedUncensor;
                else if (character?.chaFile == null && CurrentChaFile != null)
                {
                    //ChaFile hasn't been initialized yet, get the one set by the ChaControl.Initialize hook
                    PluginData extendedData = ExtendedSave.GetExtendedDataById(CurrentChaFile, GUID);
                    if (extendedData != null)
                    {
                        if (extendedData.data.TryGetValue("UncensorGUID", out var uncensorGUID))
                        {
                            if (uncensorGUID != null && UncensorDictionary.TryGetValue(uncensorGUID.ToString(), out var uncensorData))
                                uncensor = uncensorData;
                        }
                    }
                }
                else if (MakerAPI.InsideAndLoaded)
                {
                    if (GetController(character)?.UncensorGUID != null)
                    {
                        if (UncensorDictionary.TryGetValue(GetController(character).UncensorGUID, out var uncensorData))
                        {
                            uncensor = uncensorData;
                        }
                    }
                }
                else if (character?.chaFile != null)
                {
                    PluginData extendedData = ExtendedSave.GetExtendedDataById(character.chaFile, GUID);
                    if (extendedData != null)
                    {
                        if (extendedData.data.TryGetValue("UncensorGUID", out var uncensorGUID))
                        {
                            if (uncensorGUID != null && UncensorDictionary.TryGetValue(uncensorGUID.ToString(), out var uncensorData))
                                uncensor = uncensorData;
                        }
                    }
                }

                if (character?.chaFile != null)
                {
                    //If the uncensor is a trap or futa uncensor and those are disabled get the alternate uncensor if one has been configured
                    if (uncensor != null)
                    {
                        if (uncensor.Gender == Gender.Trap && !EnableTraps.Value)
                        {
                            if (character.sex == 0 && uncensor.MaleAlternate != null)
                                if (UncensorDictionary.TryGetValue(uncensor.MaleAlternate, out UncensorData alternateUncensor))
                                    uncensor = alternateUncensor;
                            if (character.sex == 1 && uncensor.FemaleAlternate != null)
                                if (UncensorDictionary.TryGetValue(uncensor.FemaleAlternate, out UncensorData alternateUncensor))
                                    uncensor = alternateUncensor;
                        }
                        else if (uncensor.Gender == Gender.Futa && !EnableFutas.Value)
                        {
                            if (character.sex == 0 && uncensor.MaleAlternate != null)
                                if (UncensorDictionary.TryGetValue(uncensor.MaleAlternate, out UncensorData alternateUncensor))
                                    uncensor = alternateUncensor;
                            if (character.sex == 1 && uncensor.FemaleAlternate != null)
                                if (UncensorDictionary.TryGetValue(uncensor.FemaleAlternate, out UncensorData alternateUncensor))
                                    uncensor = alternateUncensor;
                        }
                    }

                    //If no uncensor has been found get the default uncensor
                    if (!MakerAPI.InsideMaker && uncensor == null)
                    {
                        var male = character.sex == 0;
                        var uncensorKey = male ? DefaultMaleUncensor.Value : DefaultFemaleUncensor.Value;

                        if (uncensorKey == UncensorKeyRandom)
                        {
                            // Calculate a value that is unique for a character and unlikely to change
                            // Use System.Random to spread the results out to full int span while keeping them deterministic (so most girls don't use the same uncensor)
                            var charaHash = new System.Random(character.fileParam.birthDay + character.fileParam.personality + character.fileParam.bloodType).Next();

                            // Only randomize vanilla uncensors, no surprise futas
                            var gender = male ? Gender.Male : Gender.Female;

                            // Find a close match that is unlikely to change even if number of uncensors change
                            var query = from unc in UncensorDictionary
                                        where unc.Value.Gender == gender
                                        let uncHash = new System.Random(unc.Key.GetHashCode()).Next()
                                        orderby Mathf.Abs(uncHash - charaHash)
                                        select unc.Value;

                            var closestUncensor = query.FirstOrDefault();
                            if (closestUncensor != null)
                                uncensor = closestUncensor;
                        }
                        else
                        {
                            if (UncensorDictionary.TryGetValue(uncensorKey, out UncensorData defaultUncensor))
                                uncensor = defaultUncensor;
                        }
                    }
                }

                return uncensor;
            }
            catch { }
            return null;
        }
        /// <summary>
        /// Check if the uncensor is permitted in the character maker
        /// </summary>
        public static bool UncensorAllowedInMaker(Gender uncensorGender, byte characterSex)
        {
            if (uncensorGender == Gender.Male && (MaleDisplay.Value == "Both" || (characterSex == 0 && MaleDisplay.Value == "Male")))
                return true;

            if (uncensorGender == Gender.Female && (FemaleDisplay.Value == "Both" || (characterSex == 1 && FemaleDisplay.Value == "Female")))
                return true;

            if (uncensorGender == Gender.Trap)
            {
                bool showTraps = EnableTraps.Value;
                if (showTraps)
                {
                    if (TrapDisplay.Value == "Both")
                        showTraps = true;
                    else if (characterSex == 0 && TrapDisplay.Value == "Male")
                        showTraps = true;
                    else if (characterSex == 1 && TrapDisplay.Value == "Female")
                        showTraps = true;
                    else
                        showTraps = false;
                }
                return showTraps;
            }

            if (uncensorGender == Gender.Futa)
            {
                bool showFutas = EnableFutas.Value;
                if (showFutas)
                {
                    if (TrapDisplay.Value == "Both")
                        showFutas = true;
                    else if (characterSex == 0 && TrapDisplay.Value == "Male")
                        showFutas = true;
                    else if (characterSex == 1 && TrapDisplay.Value == "Female")
                        showFutas = true;
                    else
                        showFutas = false;
                }
                return showFutas;
            }

            return false;
        }
        private static UncensorSelectorController GetController(ChaControl character) => character?.gameObject?.GetComponent<UncensorSelectorController>();
        public static UncensorData SelectedUncensor => MakerAPI.InsideAndLoaded ? UncensorDropdown.Value == 0 ? null : UncensorDictionary[UncensorList[UncensorDropdown.Value]] : null;

        internal static string SetOOBase() => GetUncensorData(CurrentCharacter)?.OOBase ?? Defaults.OOBase;
        internal static string SetNormals() => GetUncensorData(CurrentCharacter)?.Normals ?? Defaults.Normals;
        internal static string SetBodyMainTex() => GetUncensorData(CurrentCharacter)?.BodyMainTex ?? Defaults.BodyMainTex;
        internal static string SetMaleBodyLow() => SetBodyAsset(0, false);
        internal static string SetMaleBodyHigh() => SetBodyAsset(0, true);
        internal static string SetFemaleBodyLow() => SetBodyAsset(1, false);
        internal static string SetFemaleBodyHigh() => SetBodyAsset(1, true);
        internal static string SetBodyAsset(byte sex, bool hiPoly)
        {
            string asset = GetUncensorData(CurrentCharacter)?.Asset ?? (sex == 0 ? Defaults.AssetMale : Defaults.AssetFemale);
            if (!hiPoly)
                asset += "_low";
            return asset;
        }
        internal static string SetMMBase() => GetUncensorData(CurrentCharacter)?.MMBase ?? Defaults.MMBase;
        internal static string SetBodyMaterialMale() => SetBodyMaterial(0);
        internal static string SetBodyMaterialFemale() => SetBodyMaterial(1);
        internal static string SetBodyMaterial(byte sex) => SetBodyMaterial(sex, GetUncensorData(CurrentCharacter));
        internal static string SetBodyMaterial(byte sex, UncensorData uncensor) =>
            uncensor?.BodyMaterial == null ? uncensor?.BodyType == null
            ? sex == 0 ? Defaults.BodyMaterialMale : Defaults.BodyMaterialFemale
            : uncensor.BodyType == BodyType.Male ? Defaults.BodyMaterialMale : Defaults.BodyMaterialFemale
            : uncensor.BodyMaterial;
        internal static string SetBodyMaterialCreate() => GetUncensorData(CurrentCharacter)?.BodyMaterialCreate ?? Defaults.BodyMaterialCreate;
        internal static string SetBodyColorMaskMale() => SetColorMask(0);
        internal static string SetBodyColorMaskFemale() => SetColorMask(1);
        internal static string SetColorMask(byte sex) => GetUncensorData(CurrentCharacter)?.BodyColorMask ?? (sex == 0 ? Defaults.BodyColorMaskMale : Defaults.BodyColorMaskFemale);

        public enum Gender { Male, Female, Trap, Futa }
        public enum BodyType { Male, Female }

        public class UncensorData
        {
            public string UncensorGUID;
            public string DisplayName;
            public string OOBase;
            public string MMBase;
            public string Normals;
            public Gender Gender = Gender.Male;
            public BodyType BodyType = BodyType.Male;
            public bool ShowPenis = false;
            public string BodyMainTex;
            public string BodyColorMask;
            public string BodyMaterial;
            public string BodyMaterialCreate;
            public string Asset;
            public string MaleAlternate;
            public string FemaleAlternate;
            public List<ColorMatchPart> ColorMatchList = new List<ColorMatchPart>();

            public UncensorData(XContainer uncensorXMLData)
            {
                if (uncensorXMLData == null)
                    return;

                UncensorGUID = uncensorXMLData.Element("guid")?.Value;
                DisplayName = uncensorXMLData.Element("displayName")?.Value;

                switch (uncensorXMLData.Element("gender")?.Value.ToLower())
                {
                    case "male":
                        Gender = Gender.Male;
                        break;
                    case "female":
                        Gender = Gender.Female;
                        break;
                    case "trap":
                        Gender = Gender.Trap;
                        break;
                    case "futa":
                        Gender = Gender.Futa;
                        break;
                }

                if (uncensorXMLData.Element("bodyType")?.Value.ToLower() == "female")
                    BodyType = BodyType.Female;
                if (uncensorXMLData.Element("showPenis")?.Value.ToLower() == "true" || uncensorXMLData.Element("showPenis")?.Value.ToLower() == "1")
                    ShowPenis = true;

                XElement oo_base = uncensorXMLData.Element("oo_base");
                if (oo_base != null)
                {
                    OOBase = oo_base.Element("file")?.Value;
                    //assetHighPoly only exists for backwards compatibility, can go away after any breaking change
                    Asset = oo_base.Element("assetHighPoly")?.Value;
                    if (Asset.IsNullOrWhiteSpace())
                        Asset = oo_base.Element("asset")?.Value;
                    BodyMainTex = oo_base.Element("mainTex")?.Value;
                    BodyColorMask = oo_base.Element("colorMask")?.Value;
                    Normals = oo_base.Element("normals")?.Value;
                    MaleAlternate = oo_base.Element("maleAlternate")?.Value;
                    FemaleAlternate = oo_base.Element("femaleAlternate")?.Value;

                    foreach (XElement colorMatch in oo_base.Elements("colorMatch"))
                    {
                        ColorMatchPart part = new ColorMatchPart(colorMatch.Element("object")?.Value,
                                                                 colorMatch.Element("material")?.Value,
                                                                 colorMatch.Element("materialCreate")?.Value,
                                                                 colorMatch.Element("mainTex")?.Value,
                                                                 colorMatch.Element("colorMask")?.Value);
                        if (part.Verify())
                            ColorMatchList.Add(part);
                    }
                }

                XElement mm_base = uncensorXMLData.Element("oo_base");
                if (mm_base != null)
                {
                    MMBase = mm_base.Element("mm_base")?.Value;
                    BodyMaterial = mm_base.Element("material")?.Value;
                    BodyMaterialCreate = mm_base.Element("materialCreate")?.Value;
                }

                //These things can be null if the XML doesn't exist or empty strings if it does exist but is left blank
                //Set everything to null/defaults for easier checks
                MMBase = MMBase.IsNullOrWhiteSpace() ? Defaults.MMBase : MMBase;
                OOBase = OOBase.IsNullOrWhiteSpace() ? Defaults.OOBase : OOBase;
                UncensorGUID = UncensorGUID.IsNullOrWhiteSpace() ? null : UncensorGUID;
                DisplayName = DisplayName.IsNullOrWhiteSpace() ? null : DisplayName;
                Normals = Normals.IsNullOrWhiteSpace() ? Defaults.Normals : Normals;
                BodyMainTex = BodyMainTex.IsNullOrWhiteSpace() ? Defaults.BodyMainTex : BodyMainTex;
                BodyColorMask = BodyColorMask.IsNullOrWhiteSpace() ? null : BodyColorMask;
                Asset = Asset.IsNullOrWhiteSpace() ? null : Asset;
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

                public bool Verify() => Object != null && Material != null && MaterialCreate != null && MainTex != null && ColorMask != null;
            }
        }

        public static class Defaults
        {
            public static readonly string OOBase = "chara/oo_base.unity3d";
            public static readonly string MMBase = "chara/mm_base.unity3d";
            public static readonly string AssetMale = "p_cm_body_00";
            public static readonly string AssetFemale = "p_cf_body_00";
            public static readonly string BodyMainTex = "cf_body_00_t";
            public static readonly string BodyColorMaskMale = "cm_body_00_mc";
            public static readonly string BodyColorMaskFemale = "cf_body_00_mc";
            public static readonly string Normals = "p_cf_body_00_Nml";
            public static readonly string BodyMaterialMale = "cm_m_body";
            public static readonly string BodyMaterialFemale = "cf_m_body";
            public static readonly string BodyMaterialCreate = "cf_m_body_create";
        }

        public class UncensorSelectorController : CharaCustomFunctionController
        {
            internal bool DisplayBalls { get; set; }
            internal string UncensorGUID { get; set; }
            internal bool DidReload { get; set; }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                if (currentGameMode == GameMode.Maker)
                {
                    DisplayBalls = BallsToggle.Value;
                    UncensorGUID = SelectedUncensor?.UncensorGUID;
                }

                var data = new PluginData();
                data.data.Add("DisplayBalls", DisplayBalls);
                data.data.Add("UncensorGUID", UncensorGUID);
                data.version = 1;
                SetExtendedData(data);
            }

            protected override void OnReload(GameMode currentGameMode)
            {
                DisplayBalls = ChaControl.sex == 0;
                UncensorGUID = null;

                var data = GetExtendedData();
                if (data != null)
                {
                    if (data.data.TryGetValue("DisplayBalls", out var loadedDisplayBalls))
                    {
                        DisplayBalls = (bool)loadedDisplayBalls;
                    }
                    if (data.data.TryGetValue("UncensorGUID", out var loadedUncensorGUID) && loadedUncensorGUID != null)
                    {
                        UncensorGUID = loadedUncensorGUID.ToString();
                        if (UncensorGUID.IsNullOrWhiteSpace())
                            UncensorGUID = null;
                    }
                }

                if (MakerAPI.InsideAndLoaded)
                {
                    if (MakerAPI.GetCharacterLoadFlags().Body)
                    {
                        if (MakerAPI.GetMakerBase().chaCtrl == ChaControl)
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
                    }
                    else
                    {
                        //Set the uncensor stuff to whatever is set in the maker
                        UncensorGUID = UncensorDropdown.Value == 0 ? null : UncensorList[UncensorDropdown.Value];
                        DisplayBalls = BallsToggle.Value;
                    }
                    if (UncensorList.IndexOf(UncensorGUID) == -1 || GetUncensorData(ChaControl) == null)
                        ChaControl.fileStatus.visibleSonAlways = ChaControl.sex == 0;
                    else
                        ChaControl.fileStatus.visibleSonAlways = GetUncensorData(ChaControl).ShowPenis;

                    ColorMatchMaterials(ChaControl, GetUncensorData(ChaControl));
                }
                else
                {
                    SetBallsVisibility(ChaControl, DisplayBalls);
                }

                //If the chafile is null the character may have loaded with the wrong mm_base information. Reload just the skin textures to fix that.
                if (CurrentChaFile == null && !MakerAPI.InsideMaker && !DidReload)
                {
                    DidReload = true;
                    CurrentChaFile = ChaControl.chaFile;
                    ReloadCharacterUncensor(ChaControl, false);
                }
            }
        }
    }
}