using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    partial class KK_UncensorSelector : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.uncensorselector";
        public const string PluginName = "Uncensor Selector";
        public const string PluginNameInternal = "KK_UncensorSelector";
        public const string Version = "2.7.0.1";
        private const string UncensorKeyRandom = "Random";
        private const string UncensorKeyNone = "None";
        private static HashSet<string> AllAdditionalParts = new HashSet<string>();
        public static readonly Dictionary<string, BodyData> BodyDictionary = new Dictionary<string, BodyData>();
        public static readonly Dictionary<string, PenisData> PenisDictionary = new Dictionary<string, PenisData>();
        public static readonly Dictionary<string, BallsData> BallsDictionary = new Dictionary<string, BallsData>();
        public static readonly List<string> BodyList = new List<string>();
        public static readonly List<string> BodyListDisplay = new List<string>();
        public static readonly List<string> PenisList = new List<string>();
        public static readonly List<string> PenisListDisplay = new List<string>();
        public static readonly List<string> BallsList = new List<string>();
        public static readonly List<string> BallsListDisplay = new List<string>();
        public static readonly Dictionary<string, string> BodyConfigListFull = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> PenisConfigListFull = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> BallsConfigListFull = new Dictionary<string, string>();
        private static MakerDropdown BodyDropdown;
        private static MakerDropdown PenisDropdown;
        private static MakerDropdown BallsDropdown;
        private static readonly HashSet<string> BodyParts = new HashSet<string>() { "o_dankon", "o_dan_f", "o_gomu", "o_mnpa", "o_mnpb", "o_shadowcaster" };
        private static readonly HashSet<string> PenisParts = new HashSet<string>() { "o_dankon", "o_gomu" };
        private static readonly HashSet<string> BallsParts = new HashSet<string>() { "o_dan_f" };

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
        [DisplayName("Enable Trap content")]
        [Category("Config")]
        [Description("Enable or disable all trap uncensors. Characters assigned to a trap uncensor will use the alternate uncensor as configured by the uncensor.")]
        public static ConfigWrapper<bool> EnableTraps { get; private set; }
        [DisplayName("Enable Futa content")]
        [Category("Config")]
        [Description("Enable or disable all futa uncensors. Characters assigned to a futa uncensor will use the alternate uncensor as configured by the uncensor.")]
        public static ConfigWrapper<bool> EnableFutas { get; private set; }
        [DisplayName("Default male body")]
        [Category("Config")]
        [Description("Body to use if character does not have one set.")]
        [AcceptableValueList(nameof(GetConfigBodyList))]
        public static ConfigWrapper<string> DefaultMaleBody { get; private set; }
        [DisplayName("Default male penis")]
        [Category("Config")]
        [Description("Penis to use if character does not have one set.")]
        [AcceptableValueList(nameof(GetConfigPenisList))]
        public static ConfigWrapper<string> DefaultMalePenis { get; private set; }
        [DisplayName("Default male balls")]
        [Category("Config")]
        [Description("Balls to use if character does not have one set.")]
        [AcceptableValueList(nameof(GetConfigBallsList))]
        public static ConfigWrapper<string> DefaultMaleBalls { get; private set; }
        [DisplayName("Default female body")]
        [Category("Config")]
        [Description("Body to use if character does not have one set.")]
        [AcceptableValueList(nameof(GetConfigBodyList))]
        public static ConfigWrapper<string> DefaultFemaleBody { get; private set; }
        [DisplayName("Default female penis")]
        [Category("Config")]
        [Description("Penis to use if character does not have one set.")]
        [AcceptableValueList(nameof(GetConfigPenisList))]
        public static ConfigWrapper<string> DefaultFemalePenis { get; private set; }
        [DisplayName("Default female balls")]
        [Category("Config")]
        [Description("Balls to use if character does not have one set.")]
        [AcceptableValueList(nameof(GetConfigBallsList))]
        public static ConfigWrapper<string> DefaultFemaleBalls { get; private set; }
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

            PopulateUncensorLists();

            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            CharacterApi.RegisterExtraBehaviour<UncensorSelectorController>(GUID);

            MaleDisplay = new ConfigWrapper<string>(nameof(MaleDisplay), PluginNameInternal, "Male");
            FemaleDisplay = new ConfigWrapper<string>(nameof(FemaleDisplay), PluginNameInternal, "Female");
            EnableTraps = new ConfigWrapper<bool>(nameof(EnableTraps), PluginNameInternal, true);
            EnableFutas = new ConfigWrapper<bool>(nameof(EnableFutas), PluginNameInternal, true);

            DefaultMaleBody = new ConfigWrapper<string>(nameof(DefaultMaleBody), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, UncensorKeyRandom);
            DefaultMalePenis = new ConfigWrapper<string>(nameof(DefaultMalePenis), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, UncensorKeyRandom);
            DefaultMaleBalls = new ConfigWrapper<string>(nameof(DefaultMaleBalls), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, UncensorKeyRandom);
            DefaultFemaleBody = new ConfigWrapper<string>(nameof(DefaultFemaleBody), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, UncensorKeyRandom);
            DefaultFemalePenis = new ConfigWrapper<string>(nameof(DefaultMalePenis), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, UncensorKeyNone);
            DefaultFemaleBalls = new ConfigWrapper<string>(nameof(DefaultMaleBalls), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, UncensorKeyNone);
        }

        private static string DisplayNameToBodyGuid(string displayName)
        {
            BodyConfigListFull.TryGetValue(displayName, out var guid);
            return guid;
        }
        private static string DisplayNameToPenisGuid(string displayName)
        {
            PenisConfigListFull.TryGetValue(displayName, out var guid);
            return guid;
        }
        private static string DisplayNameToBallsGuid(string displayName)
        {
            BallsConfigListFull.TryGetValue(displayName, out var guid);
            return guid;
        }

        private static string BodyGuidToDisplayName(string guid) => BodyConfigListFull.FirstOrDefault(x => x.Value == guid).Key;
        private static string PenisGuidToDisplayName(string guid) => PenisConfigListFull.FirstOrDefault(x => x.Value == guid).Key;
        private static string BallsGuidToDisplayName(string guid) => BallsConfigListFull.FirstOrDefault(x => x.Value == guid).Key;

        private static string GetDefaultUncensorGuid(byte sex)
        {
            var uncensorName = sex == 0 ? DefaultMaleBody.Value : DefaultFemaleBody.Value;
            return DisplayNameToBodyGuid(uncensorName);
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            bool DoUncensorDropdownEvents = false;
            bool DoPenisDropdownEvents = false;
            bool DoBallsDropdownEvents = false;

            BodyList.Clear();
            BodyListDisplay.Clear();

            BodyList.Add("Default");
            BodyListDisplay.Add("Default");

            var characterSex = (byte)MakerAPI.GetMakerSex();

            foreach (BodyData uncensor in BodyDictionary.Select(x => x.Value)
                .OrderByDescending(x => x.Sex == characterSex)
                .ThenBy(x => x.Sex)
                .ThenBy(x => x.DisplayName))
            {
                if (UncensorAllowedInMaker(uncensor.Sex, characterSex))
                {
                    BodyList.Add(uncensor.BodyGUID);
                    BodyListDisplay.Add(BodyGuidToDisplayName(uncensor.BodyGUID));
                }
            }

            BodyDropdown = e.AddControl(new MakerDropdown("Uncensor", BodyListDisplay.ToArray(), MakerConstants.Body.All, 0, this));
            BodyDropdown.ValueChanged.Subscribe(Observer.Create<int>(UncensorDropdownChanged));
            void UncensorDropdownChanged(int uncensorID)
            {
                if (DoUncensorDropdownEvents == false)
                {
                    DoUncensorDropdownEvents = true;
                    return;
                }

                GetController(MakerAPI.GetMakerBase().chaCtrl).UncensorGUID = BodyDropdown.Value == 0 ? null : SelectedUncensor.BodyGUID;
                ReloadCharacterUncensor(MakerAPI.GetMakerBase().chaCtrl, SelectedUncensor, SelectedPenis, SelectedPenisVisible, SelectedBalls, SelectedBallsVisible);
            }

            PenisList.Clear();
            PenisListDisplay.Clear();

            PenisList.Add("Default");
            PenisListDisplay.Add("Default");
            PenisList.Add("None");
            PenisListDisplay.Add("None");

            foreach (PenisData penis in PenisDictionary.Select(x => x.Value).OrderByDescending(x => x.DisplayName))
            {
                PenisList.Add(penis.PenisGUID);
                PenisListDisplay.Add(penis.DisplayName);
            }

            PenisDropdown = e.AddControl(new MakerDropdown("Penis", PenisListDisplay.ToArray(), MakerConstants.Body.All, characterSex == 0 ? 0 : 1, this));
            PenisDropdown.ValueChanged.Subscribe(Observer.Create<int>(PenisDropdownChanged));
            void PenisDropdownChanged(int uncensorID)
            {
                if (DoPenisDropdownEvents == false)
                {
                    DoPenisDropdownEvents = true;
                    return;
                }

                var chaControl = MakerAPI.GetMakerBase().chaCtrl;
                var controller = GetController(chaControl);

                controller.PenisGUID = SelectedPenis?.PenisGUID;
                controller.DisplayPenis = SelectedPenisVisible;
                ReloadCharacterUncensor(chaControl, SelectedUncensor, SelectedPenis, SelectedPenisVisible, SelectedBalls, SelectedBallsVisible);
            }

            BallsList.Clear();
            BallsListDisplay.Clear();

            BallsList.Add("Default");
            BallsListDisplay.Add("Default");
            BallsList.Add("None");
            BallsListDisplay.Add("None");

            foreach (BallsData balls in BallsDictionary.Select(x => x.Value).OrderByDescending(x => x.DisplayName))
            {
                BallsList.Add(balls.BallsGUID);
                BallsListDisplay.Add(balls.DisplayName);
            }

            BallsDropdown = e.AddControl(new MakerDropdown("Balls", BallsListDisplay.ToArray(), MakerConstants.Body.All, characterSex == 0 ? 0 : 1, this));
            BallsDropdown.ValueChanged.Subscribe(Observer.Create<int>(BallsDropdownChanged));
            void BallsDropdownChanged(int uncensorID)
            {
                if (DoBallsDropdownEvents == false)
                {
                    DoBallsDropdownEvents = true;
                    return;
                }

                var chaControl = MakerAPI.GetMakerBase().chaCtrl;
                var controller = GetController(chaControl);

                controller.BallsGUID = SelectedBalls?.BallsGUID;
                controller.DisplayBalls = SelectedBallsVisible;
                ReloadCharacterUncensor(chaControl, SelectedUncensor, SelectedPenis, SelectedPenisVisible, SelectedBalls, SelectedBallsVisible);
            }

            e.AddControl(new MakerText("You can set a default uncensor in plugin settings. Warning: It will not be displayed in the maker.", MakerConstants.Body.All, this) { TextColor = Color.yellow });
        }
        #region Uncensor Update
        internal static void ReloadCharacterUncensor(ChaControl chaControl, BodyData uncensor, PenisData penisData, bool penisVisible, BallsData ballsData, bool ballsVisible)
        {
            UpdateUncensor(chaControl, uncensor);
            ReloadCharacterPenis(chaControl, penisData, penisVisible);
            ReloadCharacterBalls(chaControl, ballsData, ballsVisible);

            UpdateSkin(chaControl, uncensor);
            SetChestNormals(chaControl, uncensor);
            ColorMatch.ColorMatchMaterials(chaControl, uncensor, penisData, ballsData);

            chaControl.customMatBody.SetTexture(ChaShader._AlphaMask, Traverse.Create(chaControl).Property("texBodyAlphaMask").GetValue() as Texture);
            Traverse.Create(chaControl).Property("updateAlphaMask").SetValue(true);
        }
        /// <summary>
        /// Update the mesh of the penis and set the visibility
        /// </summary>
        internal static void ReloadCharacterPenis(ChaControl chaControl, PenisData penis, bool showPenis)
        {
            bool temp = chaControl.fileStatus.visibleSonAlways;
            UpdatePenis(chaControl, penis);

            chaControl.fileStatus.visibleSonAlways = StudioAPI.InsideStudio ? temp : showPenis;
        }
        /// <summary>
        /// Update the mesh of the balls and set the visibility
        /// </summary>
        internal static void ReloadCharacterBalls(ChaControl chaControl, BallsData balls, bool showBalls)
        {
            UpdateBalls(chaControl, balls);

            SkinnedMeshRenderer ballsSMR = chaControl?.gameObject?.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x?.name == "o_dan_f");
            if (ballsSMR != null)
                ballsSMR.gameObject.GetComponent<Renderer>().enabled = showBalls;
        }
        /// <summary>
        /// Load the body asset, copy its mesh, and delete it
        /// </summary>
        private static void UpdateUncensor(ChaControl chaControl, BodyData uncensor)
        {
            string OOBase = uncensor?.OOBase ?? Defaults.OOBase;
            string Asset = uncensor?.Asset ?? (chaControl.sex == 0 ? Defaults.AssetMale : Defaults.AssetFemale);
            if (chaControl.hiPoly == false)
                Asset += "_low";

            GameObject uncensorCopy = CommonLib.LoadAsset<GameObject>(OOBase, Asset, true);
            SkinnedMeshRenderer o_body_a = chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(x => x.name == "o_body_a");

            //Copy any additional parts to the character
            if (uncensor != null && uncensor.AdditionalParts.Count > 0)
            {
                foreach (var mesh in uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (uncensor.AdditionalParts.Contains(mesh.name))
                    {
                        SkinnedMeshRenderer part = chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name);
                        Transform parent = o_body_a.gameObject.GetComponentsInChildren<Transform>(true).FirstOrDefault(c => c.name == mesh.transform.parent.name);
                        if (part == null && parent != null)
                        {
                            var copy = Instantiate(mesh);
                            copy.name = mesh.name;
                            copy.transform.parent = parent;
                            copy.bones = o_body_a.bones.Where(b => b != null && copy.bones.Any(t => t.name.Equals(b.name))).ToArray();
                        }
                    }
                }
            }

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

                //Destroy all additional parts attached to the current body that shouldn't be there
                if (AllAdditionalParts.Contains(mesh.name))
                    if (uncensor == null || !uncensor.AdditionalParts.Contains(mesh.name))
                        Destroy(mesh);
                    else
                        UpdateMeshRenderer(uncensorCopy.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), mesh, true);
            }

            Destroy(uncensorCopy);
        }
        /// <summary>
        /// Load the asset, copy its mesh, and delete it
        /// </summary>
        private static void UpdatePenis(ChaControl chaControl, PenisData penisData)
        {
            if (chaControl.hiPoly == false)
                return;

            if (penisData == null)
                return;

            GameObject dick = CommonLib.LoadAsset<GameObject>(penisData.File, penisData.Asset, true);

            foreach (var mesh in dick.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (PenisParts.Contains(mesh.name))
                    UpdateMeshRenderer(mesh, chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), true);

            Destroy(dick);
        }
        /// <summary>
        /// Load the asset, copy its mesh, and delete it
        /// </summary>
        private static void UpdateBalls(ChaControl chaControl, BallsData ballsData)
        {
            if (chaControl.hiPoly == false)
                return;

            if (ballsData == null)
                return;

            GameObject balls = CommonLib.LoadAsset<GameObject>(ballsData.File, ballsData.Asset, true);
            foreach (var mesh in balls.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (BallsParts.Contains(mesh.name))
                    UpdateMeshRenderer(mesh, chaControl.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == mesh.name), true);

            Destroy(balls);
        }
        /// <summary>
        /// Rebuild the character's skin textures
        /// </summary>
        private static void UpdateSkin(ChaControl chaControl, BodyData uncensor)
        {
            Traverse.Create(chaControl).Method("InitBaseCustomTextureBody", new object[] { uncensor?.Sex ?? chaControl.sex }).GetValue();
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
        /// Set the normals for the character's chest. This fixes the shadowing for small-chested characters.
        /// By default it is not applied to males so we do it manually for all characters in case the male is using a female body.
        /// </summary>
        public static void SetChestNormals(ChaControl chaControl, BodyData uncensor)
        {
            if (chaControl.dictBustNormal.TryGetValue(ChaControl.BustNormalKind.NmlBody, out BustNormal bustNormal))
                bustNormal.Release();

            bustNormal = new BustNormal();
            bustNormal.Init(chaControl.objBody, uncensor?.OOBase ?? Defaults.OOBase, uncensor?.Normals ?? Defaults.Normals, string.Empty);
            chaControl.dictBustNormal[ChaControl.BustNormalKind.NmlBody] = bustNormal;
        }
        #endregion
        private object[] GetConfigBodyList() => BodyConfigListFull?.Keys.OrderBy(x => x[0] == '[').ThenBy(x => x).Cast<object>().ToArray();
        private object[] GetConfigPenisList() => PenisConfigListFull?.Keys.OrderBy(x => x).Cast<object>().ToArray();
        private object[] GetConfigBallsList() => BallsConfigListFull?.Keys.OrderBy(x => x).Cast<object>().ToArray();
        /// <summary>
        /// Read all the manifest.xml files and generate a dictionary of uncensors to be used in config manager dropdown
        /// </summary>
        private static void PopulateUncensorLists()
        {
            BodyDictionary.Clear();
            BodyConfigListFull.Clear();
            PenisDictionary.Clear();
            PenisConfigListFull.Clear();
            BallsDictionary.Clear();
            BallsConfigListFull.Clear();

            //Add the default body options
            BodyConfigListFull.Add("None (censored)", UncensorKeyNone);
            BodyConfigListFull.Add("Random", UncensorKeyRandom);

            BodyData DefaultMale = new BodyData(0, "Default.Body.Male", "Default Body");
            BodyDictionary.Add(DefaultMale.BodyGUID, DefaultMale);
            BodyConfigListFull.Add($"[{(DefaultMale.Sex == 0 ? "Male" : "Female")}] {DefaultMale.DisplayName}", DefaultMale.BodyGUID);

            BodyData DefaultFemale = new BodyData(1, "Default.Body.Female", "Default Body");
            BodyDictionary.Add(DefaultFemale.BodyGUID, DefaultFemale);
            BodyConfigListFull.Add($"[{(DefaultFemale.Sex == 0 ? "Male" : "Female")}] {DefaultFemale.DisplayName}", DefaultFemale.BodyGUID);

            //Add the default penis options
            PenisConfigListFull.Add("None", UncensorKeyNone);
            PenisConfigListFull.Add("Random", UncensorKeyRandom);

            PenisData DefaultPenis = new PenisData("Default.Penis", "Censored Penis");
            PenisDictionary.Add(DefaultPenis.PenisGUID, DefaultPenis);
            PenisConfigListFull.Add(DefaultPenis.DisplayName, DefaultPenis.PenisGUID);

            //Add the default balls options
            BallsConfigListFull.Add("None", UncensorKeyNone);
            BallsConfigListFull.Add("Random", UncensorKeyRandom);

            BallsData DefaultBalls = new BallsData("Default.Balls", "Censored Balls");
            BallsDictionary.Add(DefaultBalls.BallsGUID, DefaultBalls);
            BallsConfigListFull.Add(DefaultBalls.DisplayName, DefaultBalls.BallsGUID);

            foreach (var manifest in Sideloader.Sideloader.LoadedManifests)
            {
                XDocument manifestDocument = manifest.manifestDocument;
                XElement uncensorSelectorElement = manifestDocument?.Root?.Element(PluginNameInternal);
                if (uncensorSelectorElement != null && uncensorSelectorElement.HasElements)
                {
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("body"))
                    {
                        BodyData uncensor = new BodyData(uncensorElement);
                        if (uncensor.BodyGUID == null)
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
                        BodyDictionary.Add(uncensor.BodyGUID, uncensor);
                        BodyConfigListFull.Add($"[{(uncensor.Sex == 0 ? "Male" : "Female")}] {uncensor.DisplayName}", uncensor.BodyGUID);
                        foreach (var part in uncensor.AdditionalParts)
                            AllAdditionalParts.Add(part);
                    }
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("penis"))
                    {
                        PenisData penis = new PenisData(uncensorElement);
                        if (penis.PenisGUID == null)
                        {
                            Logger.Log(LogLevel.Warning, "Penis failed to load due to missing GUID.");
                            continue;
                        }
                        if (penis.DisplayName == null)
                        {
                            Logger.Log(LogLevel.Warning, "Penis failed to load due to missing display name.");
                            continue;
                        }
                        if (penis.File == null)
                        {
                            Logger.Log(LogLevel.Warning, "Penis failed to load due to missing file.");
                            continue;
                        }
                        if (penis.Asset == null)
                        {
                            Logger.Log(LogLevel.Warning, "Penis failed to load due to missing asset.");
                            continue;
                        }
                        PenisDictionary.Add(penis.PenisGUID, penis);
                    }
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("balls"))
                    {
                        BallsData balls = new BallsData(uncensorElement);
                        if (balls.BallsGUID == null)
                        {
                            Logger.Log(LogLevel.Warning, "Balls failed to load due to missing GUID.");
                            continue;
                        }
                        if (balls.DisplayName == null)
                        {
                            Logger.Log(LogLevel.Warning, "Balls failed to load due to missing display name.");
                            continue;
                        }
                        if (balls.File == null)
                        {
                            Logger.Log(LogLevel.Warning, "Balls failed to load due to missing file.");
                            continue;
                        }
                        if (balls.Asset == null)
                        {
                            Logger.Log(LogLevel.Warning, "Balls failed to load due to missing asset.");
                            continue;
                        }
                        BallsDictionary.Add(balls.BallsGUID, balls);
                    }
                }
            }
        }

        /// <summary>
        /// Get the UncensorData for the specified character
        /// </summary>
        public static BodyData GetUncensorData(ChaControl character)
        {
            try
            {
                BodyData uncensor = null;

                if (character?.chaFile != null)
                {
                    PluginData extendedData = ExtendedSave.GetExtendedDataById(character.chaFile, GUID);
                    if (extendedData != null)
                    {
                        if (extendedData.data.TryGetValue("UncensorGUID", out var uncensorGUID))
                        {
                            if (uncensorGUID != null && BodyDictionary.TryGetValue(uncensorGUID.ToString(), out var uncensorData))
                                uncensor = uncensorData;
                        }
                    }
                }

                if (character?.chaFile != null)
                {
                    //If no uncensor has been found get the default uncensor
                    if (!MakerAPI.InsideMaker && uncensor == null)
                    {
                        var uncensorKey = GetDefaultUncensorGuid(character.sex);

                        if (uncensorKey == UncensorKeyRandom)
                        {
                            // Calculate a value that is unique for a character and unlikely to change
                            // Use System.Random to spread the results out to full int span while keeping them deterministic (so most girls don't use the same uncensor)
                            var charaHash = new System.Random(character.fileParam.birthDay + character.fileParam.personality + character.fileParam.bloodType).Next();

                            // Find a close match that is unlikely to change even if number of uncensors change
                            var query = from unc in BodyDictionary
                                        where unc.Value.Sex == character.sex && unc.Value.AllowRandom
                                        let uncHash = new System.Random(unc.Key.GetHashCode()).Next()
                                        orderby Mathf.Abs(uncHash - charaHash)
                                        select unc.Value;

                            var closestUncensor = query.FirstOrDefault();
                            if (closestUncensor != null)
                                uncensor = closestUncensor;
                        }
                        else
                        {
                            if (BodyDictionary.TryGetValue(uncensorKey, out BodyData defaultUncensor))
                                uncensor = defaultUncensor;
                        }
                    }
                }

                return uncensor;
            }
            catch { }
            return null;
        }
        public static PenisData GetPenisData(ChaControl character, string penisGUID)
        {
            if (penisGUID == null)
                return null;
            else if (PenisDictionary.TryGetValue(penisGUID, out PenisData penis))
                return penis;
            else
                return null;
        }
        public static BallsData GetBallsData(ChaControl character, string ballsGUID)
        {
            if (ballsGUID == null)
                return null;
            else if (BallsDictionary.TryGetValue(ballsGUID, out BallsData balls))
                return balls;
            else
                return null;
        }
        /// <summary>
        /// Check if the uncensor is permitted in the character maker
        /// </summary>
        public static bool UncensorAllowedInMaker(byte uncensorSex, byte characterSex)
        {
            if (uncensorSex == 0 && (MaleDisplay.Value == "Both" || (characterSex == 0 && MaleDisplay.Value == "Male")))
                return true;

            if (uncensorSex == 1 && (FemaleDisplay.Value == "Both" || (characterSex == 1 && FemaleDisplay.Value == "Female")))
                return true;

            return false;
        }

        private static UncensorSelectorController GetController(ChaControl character) => character?.gameObject?.GetComponent<UncensorSelectorController>();
        public static BodyData SelectedUncensor => MakerAPI.InsideAndLoaded ? BodyDropdown.Value == 0 ? null : BodyDictionary[BodyList[BodyDropdown.Value]] : null;
        public static PenisData SelectedPenis => MakerAPI.InsideAndLoaded ? (PenisDropdown.Value == 0 || PenisDropdown.Value == 1) ? null : PenisDictionary[PenisList[PenisDropdown.Value]] : null;
        public static bool SelectedPenisVisible => PenisDropdown.Value == 1 ? false : true;
        public static BallsData SelectedBalls => MakerAPI.InsideAndLoaded ? (BallsDropdown.Value == 0 || BallsDropdown.Value == 1) ? null : BallsDictionary[BallsList[BallsDropdown.Value]] : null;
        public static bool SelectedBallsVisible => BallsDropdown.Value == 1 ? false : true;

        public class UncensorSelectorController : CharaCustomFunctionController
        {
            internal string UncensorGUID { get; set; }
            internal string PenisGUID { get; set; }
            internal string BallsGUID { get; set; }
            internal bool DisplayPenis { get; set; }
            internal bool DisplayBalls { get; set; }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                if (currentGameMode == GameMode.Maker)
                {
                    UncensorGUID = SelectedUncensor?.BodyGUID;
                    PenisGUID = SelectedPenis?.PenisGUID;
                    BallsGUID = SelectedBalls?.BallsGUID;
                    DisplayPenis = SelectedPenisVisible;
                    DisplayBalls = SelectedBallsVisible;
                }

                var data = new PluginData();
                data.data.Add("UncensorGUID", UncensorGUID);
                data.data.Add("PenisGUID", PenisGUID);
                data.data.Add("BallsGUID", BallsGUID);
                data.data.Add("DisplayPenis", DisplayPenis);
                data.data.Add("DisplayBalls", DisplayBalls);
                data.version = 2;
                SetExtendedData(data);
            }

            protected override void OnReload(GameMode currentGameMode)
            {
                UncensorGUID = null;
                PenisGUID = null;
                BallsGUID = null;
                DisplayPenis = ChaControl.sex == 0;
                DisplayBalls = ChaControl.sex == 0;

                var data = GetExtendedData();
                if (data != null)
                {
                    if (data.data.TryGetValue("UncensorGUID", out var loadedUncensorGUID) && loadedUncensorGUID != null)
                    {
                        UncensorGUID = loadedUncensorGUID.ToString();
                        if (UncensorGUID.IsNullOrWhiteSpace())
                            UncensorGUID = null;
                    }
                    if (data.data.TryGetValue("PenisGUID", out var loadedPenisGUID) && loadedPenisGUID != null)
                    {
                        PenisGUID = loadedPenisGUID.ToString();
                        if (PenisGUID.IsNullOrWhiteSpace())
                            PenisGUID = null;
                    }
                    if (data.data.TryGetValue("BallsGUID", out var loadedBallsGUID) && loadedBallsGUID != null)
                    {
                        BallsGUID = loadedBallsGUID.ToString();
                        if (BallsGUID.IsNullOrWhiteSpace())
                            BallsGUID = null;
                    }
                    if (data.data.TryGetValue("DisplayPenis", out var loadedDisplayPenis))
                    {
                        DisplayPenis = (bool)loadedDisplayPenis;
                    }
                    if (data.data.TryGetValue("DisplayBalls", out var loadedDisplayBalls))
                    {
                        DisplayBalls = (bool)loadedDisplayBalls;
                    }
                }

                if (MakerAPI.InsideAndLoaded)
                {
                    if (MakerAPI.GetCharacterLoadFlags().Body)
                    {
                        if (MakerAPI.GetMakerBase().chaCtrl == ChaControl)
                        {
                            //Change the UI settings which will update the character's uncensor
                            if (BodyList.IndexOf(UncensorGUID) == -1)
                            {
                                //The loaded uncensor isn't on the list, possibly due to being forbidden
                                BodyDropdown.Value = 0;
                                UncensorGUID = null;
                            }
                            else
                            {
                                BodyDropdown.Value = BodyList.IndexOf(UncensorGUID);
                            }

                            if (PenisList.IndexOf(PenisGUID) == -1)
                            {
                                if (DisplayPenis)
                                    PenisDropdown.Value = 0;
                                else
                                    PenisDropdown.Value = 1;
                                PenisGUID = null;
                            }
                            else
                            {
                                PenisDropdown.Value = PenisList.IndexOf(PenisGUID);
                            }

                            if (BallsList.IndexOf(BallsGUID) == -1)
                            {
                                if (DisplayBalls)
                                    BallsDropdown.Value = 0;
                                else
                                    BallsDropdown.Value = 1;
                                BallsGUID = null;
                            }
                            else
                            {
                                BallsDropdown.Value = BallsList.IndexOf(BallsGUID);
                            }
                        }
                    }
                    else
                    {
                        //Set the uncensor stuff to whatever is set in the maker
                        UncensorGUID = BodyDropdown.Value == 0 ? null : BodyList[BodyDropdown.Value];
                        PenisGUID = PenisDropdown.Value == 0 ? null : PenisList[PenisDropdown.Value];
                        BallsGUID = BallsDropdown.Value == 0 ? null : BallsList[BallsDropdown.Value];
                    }
                }
                else
                {
                    //Reload the uncensor on every load or reload
                    ReloadCharacterUncensor(ChaControl, GetUncensorData(ChaControl), GetPenisData(ChaControl, PenisGUID), DisplayPenis, GetBallsData(ChaControl, BallsGUID), DisplayBalls);
                }
            }
        }
    }
}