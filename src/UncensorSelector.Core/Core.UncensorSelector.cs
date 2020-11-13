using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
{
    /// <summary>
    /// Plugin for assigning uncensors to characters individually
    /// </summary>
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(KoiSkinOverlayX.KoiSkinOverlayMgr.GUID, "5.1")]
    [BepInPlugin(GUID, PluginName, Version)]
    internal partial class UncensorSelector : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.uncensorselector";
        public const string PluginName = "Uncensor Selector";
        public const string PluginNameInternal = "KK_UncensorSelector";
        public const string Version = "3.11.1";
        internal static new ManualLogSource Logger;
        private static readonly HashSet<string> AllAdditionalParts = new HashSet<string>();
        public static readonly Dictionary<string, BodyData> BodyDictionary = new Dictionary<string, BodyData>();
        public static readonly Dictionary<string, PenisData> PenisDictionary = new Dictionary<string, PenisData>();
        public static readonly Dictionary<string, BallsData> BallsDictionary = new Dictionary<string, BallsData>();
        public static readonly Dictionary<string, MigrationData> MigrationDictionary = new Dictionary<string, MigrationData>();
        public static readonly List<string> BodyList = new List<string>();
        public static readonly List<string> BodyListDisplay = new List<string>();
        public static readonly List<string> PenisList = new List<string>();
        public static readonly List<string> PenisListDisplay = new List<string>();
        public static readonly List<string> BallsList = new List<string>();
        public static readonly List<string> BallsListDisplay = new List<string>();
        public static readonly Dictionary<string, string> BodyConfigListFull = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> PenisConfigListFull = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> BallsConfigListFull = new Dictionary<string, string>();
        internal static MakerDropdown BodyDropdown;
        internal static MakerDropdown PenisDropdown;
        internal static MakerDropdown BallsDropdown;
        private static readonly HashSet<string> BodyNames = new HashSet<string> { "o_body_a", "o_body_cf", "o_body_cm" };
        private static readonly HashSet<string> BodyParts = new HashSet<string> { "o_dankon", "o_dan_f", "o_gomu", "o_mnpa", "o_mnpb", "o_shadowcaster", "cm_o_dan00", "cm_o_dan_f" };
        private static readonly HashSet<string> PenisParts = new HashSet<string> { "o_dankon", "o_gomu", "cm_o_dan00" };
        private static readonly HashSet<string> BallsParts = new HashSet<string> { "o_dan_f", "cm_o_dan_f" };
        internal static string CurrentBodyGUID;
        internal static bool DidErrorMessage = false;

        public const string DefaultBodyFemaleGUID = "Default.Body.Female";
        public const string DefaultBodyMaleGUID = "Default.Body.Male";
        public const string DefaultPenisGUID = "Default.Penis";
        public const string DefaultBallsGUID = "Default.Balls";

#if KK
        public static ConfigEntry<bool> _GenderBender { get; private set; }
        public static bool GenderBender => _GenderBender.Value;
#else
        public static bool GenderBender => false;
#endif
        public static ConfigEntry<string> DefaultMaleBody { get; private set; }
        public static ConfigEntry<string> DefaultMalePenis { get; private set; }
        public static ConfigEntry<string> DefaultMaleBalls { get; private set; }
        public static ConfigEntry<string> DefaultFemaleBody { get; private set; }
        public static ConfigEntry<string> DefaultFemalePenis { get; private set; }
        public static ConfigEntry<string> DefaultFemaleBalls { get; private set; }
        public static ConfigEntry<bool> DefaultFemaleDisplayBalls { get; private set; }
        public static ConfigEntry<string> RandomExcludedMaleBody { get; private set; }
        public static ConfigEntry<string> RandomExcludedMalePenis { get; private set; }
        public static ConfigEntry<string> RandomExcludedMaleBalls { get; private set; }
        public static ConfigEntry<string> RandomExcludedFemaleBody { get; private set; }
        public static ConfigEntry<string> RandomExcludedFemalePenis { get; private set; }
        public static ConfigEntry<string> RandomExcludedFemaleBalls { get; private set; }

        private static readonly Dictionary<byte, Dictionary<string, HashSet<string>>> RandomExcludedSets = new Dictionary<byte, Dictionary<string, HashSet<string>>>();

        internal static List<ISettingsGUI> SettingsGUIs = new List<ISettingsGUI>();
        internal void Main()
        {
            Logger = base.Logger;

            PopulateUncensorLists();

#if KK
            _GenderBender = Config.Bind("Config", "Genderbender Allowed", true, new ConfigDescription("Whether or not genderbender characters are allowed. When disabled, girls will always have a female body with no penis, boys will always have a male body and a penis. Genderbender characters will still load in Studio for scene compatibility.", null, new ConfigurationManagerAttributes { Order = 29 }));
#endif
            InitUncensorConfigs("Male", "Body", BodyDictionary, GetConfigBodyList(), 19, out var defaultConf, out var excludeConf);
            DefaultMaleBody = defaultConf;
            RandomExcludedMaleBody = excludeConf;
            InitUncensorConfigs("Male", "Penis", PenisDictionary, GetConfigPenisList(), 18, out defaultConf, out excludeConf);
            DefaultMalePenis = defaultConf;
            RandomExcludedMalePenis = excludeConf;
            InitUncensorConfigs("Male", "Balls", BallsDictionary, GetConfigBallsList(), 17, out defaultConf, out excludeConf);
            DefaultMaleBalls = defaultConf;
            RandomExcludedMaleBalls = excludeConf;
            InitUncensorConfigs("Female", "Body", BodyDictionary, GetConfigBodyList(), 9, out defaultConf, out excludeConf);
            DefaultFemaleBody = defaultConf;
            RandomExcludedFemaleBody = excludeConf;
            InitUncensorConfigs("Female", "Penis", PenisDictionary, GetConfigPenisList(), 8, out defaultConf, out excludeConf);
            DefaultFemalePenis = defaultConf;
            RandomExcludedFemalePenis = excludeConf;
            InitUncensorConfigs("Female", "Balls", BallsDictionary, GetConfigBallsList(), 7, out defaultConf, out excludeConf);
            DefaultFemaleBalls = defaultConf;
            RandomExcludedFemaleBalls = excludeConf;

            DefaultFemaleDisplayBalls = Config.Bind("Config", "Default Female Balls Display", false, new ConfigDescription("Whether balls will be displayed on females if not otherwise configured.", null, new ConfigurationManagerAttributes { Order = 6 }));

            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
            CharacterApi.RegisterExtraBehaviour<UncensorSelectorController>(GUID);
#if !EC
            RegisterStudioControls();
#endif

            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

#if KK
            Type loadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).First(x => x.Name.StartsWith("<LoadAsync>c__Iterator"));
            MethodInfo loadAsyncIteratorMoveNext = loadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(loadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.LoadAsyncTranspiler), AccessTools.all)));
#endif
        }

        internal void OnGUI()
        {
            foreach (var settingsGUI in SettingsGUIs.Where(g => g.Visible))
                settingsGUI.DoOnGUI();
        }
        internal static void UpdateGuidSet(string entry, HashSet<string> set)
        {
            set.Clear();
            foreach (var guid in entry.Split(";,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                set.Add(guid);
        }

        private void InitUncensorConfigs<T>(string sex, string part, Dictionary<string, T> uncensorDictionary, string[] defaultChoices, int order, out ConfigEntry<string> defaultConf, out ConfigEntry<string> excludeConf) where T : IUncensorData
        {
            byte sexValue = (byte)(sex == "Male" ? 0 : 1);

            if (!RandomExcludedSets.TryGetValue(sexValue, out var sexExcluded))
                sexExcluded = RandomExcludedSets[sexValue] = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            if (!sexExcluded.TryGetValue(part, out var partExcluded))
                partExcluded = sexExcluded[part] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var defaultDescAttrs = new ConfigurationManagerAttributes { Order = order };
            var defaultDesc = new ConfigDescription($"{part} to use if the character does not have one set. The {(part == "Body" ? "censored" : "mosaic")} {part.ToLower()} will not be selected randomly if there are any alternatives.", new AcceptableValueList<string>(defaultChoices), defaultDescAttrs);
            defaultConf = Config.Bind("Config", $"Default {sex} {part}", "Random", defaultDesc);

            var settingsGUI = new SettingsGUI<T>(uncensorDictionary, sexValue, part);
            SettingsGUIs.Add(settingsGUI);
            var excludedDescAttrs = new ConfigurationManagerAttributes { HideDefaultButton = true, CustomDrawer = settingsGUI.DrawSettingsGUI, Order = order };
            var excludedDesc = new ConfigDescription($"{part} uncensors to exclude from random selection for {sex.ToLower()}s", null, excludedDescAttrs);
            excludeConf = Config.Bind("Random Excluded", $"{sex} {part}", string.Empty, excludedDesc);

            //Apply initial config file value now
            UpdateGuidSet(excludeConf.Value, partExcluded);

            //Update on change
            excludeConf.SettingChanged += (s, a) =>
            {
                if (!(a is SettingChangedEventArgs args) || !(args.ChangedSetting is ConfigEntry<string> conf))
                {
                    Logger.LogError("Unexpected error, unable to handle changed settings.");
                    return;
                }
                UpdateGuidSet(conf.Value, partExcluded);
            };

        }

        public static bool IsExcludedFromRandom(byte sex, string part, string guid)
        {
            return RandomExcludedSets.TryGetValue(sex, out var sexExcluded) &&
                sexExcluded.TryGetValue(part, out var excluded) &&
                excluded.Contains(guid);
        }

        /// <summary>
        /// Initialize the character maker GUI
        /// </summary>
        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            bool DoDropdownEvents = false;
            e.AddControl(new MakerText(PluginName, MakerConstants.Body.All, this));

            BodyList.Clear();
            BodyListDisplay.Clear();

            BodyList.Add("Default");
            BodyListDisplay.Add("Default");

            var characterSex = (byte)MakerAPI.GetMakerSex();

            foreach (BodyData bodyData in BodyDictionary.Select(x => x.Value)
                .OrderByDescending(x => x.Sex == characterSex)
                .ThenBy(x => x.Sex)
                .ThenBy(x => x.DisplayName))
            {
                if (BodyAllowedInMaker(bodyData.Sex, characterSex))
                {
                    BodyList.Add(bodyData.BodyGUID);
                    BodyListDisplay.Add(BodyGuidToDisplayName(bodyData.BodyGUID));
                }
            }

            BodyDropdown = e.AddControl(new MakerDropdown("Body", BodyListDisplay.ToArray(), MakerConstants.Body.All, 0, this));
            BodyDropdown.ValueChanged.Subscribe(BodyDropdownChanged);
            void BodyDropdownChanged(int ID)
            {
                if (DoDropdownEvents == false)
                    return;

                var controller = GetController(MakerAPI.GetCharacterControl());
                controller.BodyGUID = ID == 0 ? null : BodyList[ID];
                controller.UpdateUncensor();
            }

            PenisList.Clear();
            PenisListDisplay.Clear();

            PenisList.Add("Default");
            PenisListDisplay.Add("Default");
#if KK
            PenisList.Add("None");
            PenisListDisplay.Add("None");
#endif

            foreach (PenisData penis in PenisDictionary.Select(x => x.Value).OrderByDescending(x => x.DisplayName))
            {
                PenisList.Add(penis.PenisGUID);
                PenisListDisplay.Add(penis.DisplayName);
            }

#if KK
            PenisDropdown = e.AddControl(new MakerDropdown("Penis", PenisListDisplay.ToArray(), MakerConstants.Body.All, characterSex == 0 ? 0 : 1, this));
#else
            PenisDropdown = e.AddControl(new MakerDropdown("Penis", PenisListDisplay.ToArray(), MakerConstants.Body.All, 0, this));
#endif
            PenisDropdown.ValueChanged.Subscribe(PenisDropdownChanged);
            void PenisDropdownChanged(int ID)
            {
                if (DoDropdownEvents == false)
                    return;

                var controller = GetController(MakerAPI.GetCharacterControl());
#if KK
                controller.PenisGUID = ID == 0 || ID == 1 ? null : PenisList[ID];
                controller.DisplayPenis = ID != 1;
#elif AI || HS2
                controller.PenisGUID = ID == 0 ? null : PenisList[ID];
#else
                controller.PenisGUID = ID == 0 ? null : PenisList[ID];
                controller.DisplayPenis = characterSex == 0;
#endif
                controller.UpdateUncensor();
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

            int ballsInitialValue = characterSex == 0 ? 0 : DefaultFemaleDisplayBalls.Value ? 0 : 1;
            BallsDropdown = e.AddControl(new MakerDropdown("Balls", BallsListDisplay.ToArray(), MakerConstants.Body.All, ballsInitialValue, this));
            BallsDropdown.ValueChanged.Subscribe(BallsDropdownChanged);
            void BallsDropdownChanged(int ID)
            {
                if (DoDropdownEvents == false)
                    return;

                var controller = GetController(MakerAPI.GetCharacterControl());
                controller.BallsGUID = ID == 0 || ID == 1 ? null : BallsList[ID];
                controller.DisplayBalls = ID != 1;
                controller.UpdateUncensor();
            }

#if EC
            if (characterSex == 1)
            {
                var dickToggle = e.AddControl(new MakerToggle(MakerConstants.Body.All, "Toggle Penis Display", characterSex == 0, this));
                dickToggle.ValueChanged.Subscribe(Observer.Create<bool>(obj => MakerAPI.GetCharacterControl().fileStatus.visibleSonAlways = dickToggle.Value));
            }
#endif

#if AI || HS2
            if (characterSex == 0)
                BodyDropdown.Visible.OnNext(false);
            else
                TogglePenisBallsUI(false);
#endif

            DoDropdownEvents = true;
        }
        /// <summary>
        /// Set initial values for the loaded character and enable dropdown events
        /// </summary>
        private static void MakerAPI_MakerFinishedLoading(object sender, EventArgs e)
        {
            var controller = GetController(MakerAPI.GetCharacterControl());

            if (controller != null)
            {
                if (controller.BodyGUID != null && BodyList.IndexOf(controller.BodyGUID) != -1)
                    BodyDropdown.SetValue(BodyList.IndexOf(controller.BodyGUID), false);

                if (controller.PenisGUID != null && PenisList.IndexOf(controller.PenisGUID) != -1)
                    PenisDropdown?.SetValue(PenisList.IndexOf(controller.PenisGUID), false);
                else if (controller.PenisGUID == null)
#if KK
                    PenisDropdown?.SetValue(controller.DisplayPenis ? 0 : 1, false);
#else
                    PenisDropdown.SetValue(0, false);
#endif

                if (controller.BallsGUID != null && BallsList.IndexOf(controller.BallsGUID) != -1)
                    BallsDropdown?.SetValue(BallsList.IndexOf(controller.BallsGUID), false);
                else if (controller.BallsGUID == null)
                    BallsDropdown?.SetValue(controller.DisplayBalls ? 0 : 1, false);

#if AI || HS2
                if (controller.ChaControl.sex == 1)
                {
                    GameObject goFutanari = GameObject.Find("CharaCustom/CustomControl/CanvasMain/SubMenu/SubMenuBody/Scroll View/Viewport/Content/Category/CategoryTop/Futanari/tglFutanari");
                    var tglFutanari = goFutanari.GetComponent<Toggle>();
                    tglFutanari.onValueChanged.AddListener(value =>
                    {
                        TogglePenisBallsUI(value);
                        controller.ChaControl.fileStatus.visibleSonAlways = value;
                    });
                }
#endif
            }
        }
#if AI || HS2
        private static void TogglePenisBallsUI(bool visible)
        {
            PenisDropdown.Visible.OnNext(visible);
            BallsDropdown.Visible.OnNext(visible);
        }
#endif
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
            BodyConfigListFull["Random"] = "Random";

#if KK || EC
            BodyData DefaultMale = new BodyData(0, DefaultBodyMaleGUID, "Default Body M");
            BodyDictionary[DefaultMale.BodyGUID] = DefaultMale;
            BodyConfigListFull[$"[{(DefaultMale.Sex == 0 ? "Male" : "Female")}] {DefaultMale.DisplayName}"] = DefaultMale.BodyGUID;
#endif

            BodyData DefaultFemale = new BodyData(1, DefaultBodyFemaleGUID, "Default Body F");
            BodyDictionary[DefaultFemale.BodyGUID] = DefaultFemale;
            BodyConfigListFull[$"[{(DefaultFemale.Sex == 0 ? "Male" : "Female")}] {DefaultFemale.DisplayName}"] = DefaultFemale.BodyGUID;

            //Add the default penis options
            PenisConfigListFull["Random"] = "Random";

            PenisData DefaultPenis = new PenisData(DefaultPenisGUID, "Mosaic Penis");
            PenisDictionary[DefaultPenis.PenisGUID] = DefaultPenis;
            PenisConfigListFull[DefaultPenis.DisplayName] = DefaultPenis.PenisGUID;

            //Add the default balls options
            BallsConfigListFull["Random"] = "Random";

            BallsData DefaultBalls = new BallsData(DefaultBallsGUID, "Mosaic Balls");
            BallsDictionary[DefaultBalls.BallsGUID] = DefaultBalls;
            BallsConfigListFull[DefaultBalls.DisplayName] = DefaultBalls.BallsGUID;

            var loadedManifests = Sideloader.Sideloader.Manifests;

            foreach (var manifest in loadedManifests.Values)
            {
                XDocument manifestDocument = manifest.manifestDocument;
                XElement uncensorSelectorElement = manifestDocument?.Root?.Element(PluginNameInternal);
                if (uncensorSelectorElement != null && uncensorSelectorElement.HasElements)
                {
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("body"))
                    {
                        BodyData bodyData = new BodyData(uncensorElement);
                        if (bodyData.BodyGUID == null)
                        {
                            Logger.LogWarning("Body failed to load due to missing GUID.");
                            continue;
                        }
                        if (bodyData.DisplayName == null)
                        {
                            Logger.LogWarning("Body failed to load due to missing display name.");
                            continue;
                        }
                        if (bodyData.OOBase == Defaults.OOBase)
                        {
                            Logger.LogWarning("Body was not loaded because oo_base is the default.");
                            continue;
                        }
                        BodyDictionary[bodyData.BodyGUID] = bodyData;
                        BodyConfigListFull[$"[{(bodyData.Sex == 0 ? "Male" : "Female")}] {bodyData.DisplayName}"] = bodyData.BodyGUID;
                        foreach (var part in bodyData.AdditionalParts)
                            AllAdditionalParts.Add(part);
                    }
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("penis"))
                    {
                        PenisData penisData = new PenisData(uncensorElement);
                        if (penisData.PenisGUID == null)
                        {
                            Logger.LogWarning("Penis failed to load due to missing GUID.");
                            continue;
                        }
                        if (penisData.DisplayName == null)
                        {
                            Logger.LogWarning("Penis failed to load due to missing display name.");
                            continue;
                        }
                        if (penisData.File == null)
                        {
                            Logger.LogWarning("Penis failed to load due to missing file.");
                            continue;
                        }
                        if (penisData.Asset == null)
                        {
                            Logger.LogWarning("Penis failed to load due to missing asset.");
                            continue;
                        }
                        PenisDictionary[penisData.PenisGUID] = penisData;
                        PenisConfigListFull[penisData.DisplayName] = penisData.PenisGUID;
                    }
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("balls"))
                    {
                        BallsData ballsData = new BallsData(uncensorElement);
                        if (ballsData.BallsGUID == null)
                        {
                            Logger.LogWarning("Balls failed to load due to missing GUID.");
                            continue;
                        }
                        if (ballsData.DisplayName == null)
                        {
                            Logger.LogWarning("Balls failed to load due to missing display name.");
                            continue;
                        }
                        if (ballsData.File == null)
                        {
                            Logger.LogWarning("Balls failed to load due to missing file.");
                            continue;
                        }
                        if (ballsData.Asset == null)
                        {
                            Logger.LogWarning("Balls failed to load due to missing asset.");
                            continue;
                        }
                        BallsDictionary[ballsData.BallsGUID] = ballsData;
                        BallsConfigListFull[ballsData.DisplayName] = ballsData.BallsGUID;
                    }
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("migration"))
                    {
                        MigrationData migrationData = new MigrationData(uncensorElement);
                        if (migrationData.UncensorGUID == null)
                        {
                            Logger.LogWarning("Migration data failed to load due to missing Uncensor GUID.");
                            continue;
                        }
                        if (migrationData.BodyGUID == null)
                        {
                            Logger.LogWarning("Migration data failed to load due to missing Body GUID.");
                            continue;
                        }
                        MigrationDictionary[migrationData.UncensorGUID] = migrationData;
                    }
                }
            }
        }
        /// <summary>
        /// Check if the body is permitted in the character maker
        /// </summary>
        public static bool BodyAllowedInMaker(byte bodySex, byte characterSex)
        {
            if (GenderBender)
                return true;

            if (bodySex == characterSex)
                return true;

            return false;
        }

        internal string[] GetConfigBodyList() => BodyConfigListFull?.Keys.OrderByDescending(x => x == "Random").ThenBy(x => x[0] == '[').ThenBy(x => x).ToArray();
        internal string[] GetConfigPenisList() => PenisConfigListFull?.Keys.OrderByDescending(x => x == "Random").ThenBy(x => x).ToArray();
        internal string[] GetConfigBallsList() => BallsConfigListFull?.Keys.OrderByDescending(x => x == "Random").ThenBy(x => x).ToArray();

        private static string DisplayNameToBodyGuid(string displayName)
        {
            BodyConfigListFull.TryGetValue(displayName, out var guid);
            return guid.IsNullOrWhiteSpace() ? "Random" : guid;
        }
        private static string DisplayNameToPenisGuid(string displayName)
        {
            PenisConfigListFull.TryGetValue(displayName, out var guid);
            return guid.IsNullOrWhiteSpace() ? "Random" : guid;
        }
        private static string DisplayNameToBallsGuid(string displayName)
        {
            BallsConfigListFull.TryGetValue(displayName, out var guid);
            return guid.IsNullOrWhiteSpace() ? "Random" : guid;
        }
        private static string BodyGuidToDisplayName(string guid)
        {
            var displayName = BodyConfigListFull.FirstOrDefault(x => x.Value == guid).Key;
            return displayName.IsNullOrWhiteSpace() ? "Random" : displayName;
        }
        internal static string PenisGuidToDisplayName(string guid) => PenisConfigListFull.FirstOrDefault(x => x.Value == guid).Key ?? "Random";
        internal static string BallsGuidToDisplayName(string guid) => BallsConfigListFull.FirstOrDefault(x => x.Value == guid).Key ?? "Random";
        /// <summary>
        /// Returns the UncensorSelectorController for the specified character or null if it does not exist
        /// </summary>
        public static UncensorSelectorController GetController(ChaControl character) => character == null || character.gameObject == null ? null : character.gameObject.GetComponent<UncensorSelectorController>();
        internal static string SetOOBase() => CurrentBodyGUID == null ? Defaults.OOBase : BodyDictionary[CurrentBodyGUID].OOBase;
        internal static string SetBodyMainTex() => CurrentBodyGUID == null ? Defaults.BodyMainTex : BodyDictionary[CurrentBodyGUID].BodyMainTex;
        internal static string SetBodyColorMaskMale() => SetColorMask(0);
        internal static string SetBodyColorMaskFemale() => SetColorMask(1);
        internal static string SetColorMask(byte sex) => CurrentBodyGUID == null ? (sex == 0 ? Defaults.BodyColorMaskMale : Defaults.BodyColorMaskFemale) : BodyDictionary[CurrentBodyGUID].BodyColorMask;
        internal static string SetMMBase() => CurrentBodyGUID == null ? Defaults.MMBase : BodyDictionary[CurrentBodyGUID].MMBase;
        internal static string SetBodyMaterialMale() => SetBodyMaterial(0);
        internal static string SetBodyMaterialFemale() => SetBodyMaterial(1);
        internal static string SetBodyMaterial(byte sex) => CurrentBodyGUID == null ? sex == 0 ? Defaults.BodyMaterialMale : Defaults.BodyMaterialFemale : BodyDictionary[CurrentBodyGUID].BodyMaterial;
        internal static string SetBodyMaterialCreate() => CurrentBodyGUID == null ? Defaults.BodyMaterialCreate : BodyDictionary[CurrentBodyGUID].BodyMaterialCreate;
        internal static string SetMaleBodyLow() => "p_cf_body_00_low";
    }
}