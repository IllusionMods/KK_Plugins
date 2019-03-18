using BepInEx;
using BepInEx.Logging;
using Harmony;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UniRx;
using Logger = BepInEx.Logger;

namespace KK_UncensorSelector
{
    /// <summary>
    /// Plugin for assigning uncensors to characters individually
    /// </summary>
    [BepInDependency("com.bepis.bepinex.sideloader")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInDependency("com.bepis.bepinex.configurationmanager")]
    [BepInDependency("marco.kkapi")]
    [BepInPlugin(GUID, PluginName, Version)]
    partial class KK_UncensorSelector : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.uncensorselector";
        public const string PluginName = "Uncensor Selector";
        public const string PluginNameInternal = nameof(KK_UncensorSelector);
        public const string Version = "3.3";
        private const string UncensorKeyRandom = "Random";
        private const string UncensorKeyNone = "None";
        private const string MaleBodyDefaultValue = UncensorKeyRandom;
        private const string MalePenisDefaultValue = UncensorKeyRandom;
        private const string MaleBallsDefaultValue = UncensorKeyRandom;
        private const string FemaleBodyDefaultValue = UncensorKeyRandom;
        private const string FemalePenisDefaultValue = UncensorKeyNone;
        private const string FemaleBallsDefaultValue = UncensorKeyNone;
        private static HashSet<string> AllAdditionalParts = new HashSet<string>();
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
        private static MakerDropdown BodyDropdown;
        private static MakerDropdown PenisDropdown;
        private static MakerDropdown BallsDropdown;
        private static bool DoDropdownEvents = false;
        private static readonly HashSet<string> BodyParts = new HashSet<string>() { "o_dankon", "o_dan_f", "o_gomu", "o_mnpa", "o_mnpb", "o_shadowcaster" };
        private static readonly HashSet<string> PenisParts = new HashSet<string>() { "o_dankon", "o_gomu" };
        private static readonly HashSet<string> BallsParts = new HashSet<string>() { "o_dan_f" };
        internal static string CurrentBodyGUID;

        #region Config
        [DisplayName("Genderbender allowed")]
        [Category("Config")]
        [Description("Whether or not genderbender characters are allowed. " +
            "When disabled, girls will always have a female body with no penis, boys will always have a male body and a penis. " +
            "Genderbender characters will still load in Studio for scene compatibility.")]
        public static ConfigWrapper<bool> GenderBender { get; private set; }
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

        private object[] GetConfigBodyList() => BodyConfigListFull?.Keys.OrderBy(x => x[0] == '[').ThenBy(x => x).Cast<object>().ToArray();
        private object[] GetConfigPenisList() => PenisConfigListFull?.Keys.OrderBy(x => x).Cast<object>().ToArray();
        private object[] GetConfigBallsList() => BallsConfigListFull?.Keys.OrderBy(x => x).Cast<object>().ToArray();
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
        private static string BodyGuidToDisplayName(string guid)
        {
            var displayName = BodyConfigListFull.FirstOrDefault(x => x.Value == guid).Key;
            return displayName.IsNullOrWhiteSpace() ? UncensorKeyRandom : displayName;
        }
        private static string MalePenisGuidToDisplayName(string guid)
        {
            var displayName = PenisConfigListFull.FirstOrDefault(x => x.Value == guid).Key;
            return displayName.IsNullOrWhiteSpace() ? MalePenisDefaultValue : displayName;
        }
        private static string FemalePenisGuidToDisplayName(string guid)
        {
            var displayName = PenisConfigListFull.FirstOrDefault(x => x.Value == guid).Key;
            return displayName.IsNullOrWhiteSpace() ? FemalePenisDefaultValue : displayName;
        }
        private static string MaleBallsGuidToDisplayName(string guid)
        {
            var displayName = BallsConfigListFull.FirstOrDefault(x => x.Value == guid).Key;
            return displayName.IsNullOrWhiteSpace() ? MaleBallsDefaultValue : displayName;
        }
        private static string FemaleBallsGuidToDisplayName(string guid)
        {
            var displayName = BallsConfigListFull.FirstOrDefault(x => x.Value == guid).Key;
            return displayName.IsNullOrWhiteSpace() ? FemaleBallsDefaultValue : displayName;
        }
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

            Type loadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).First(x => x.Name.StartsWith("<LoadAsync>c__Iterator"));
            MethodInfo loadAsyncIteratorMoveNext = loadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(loadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.LoadAsyncTranspiler), BindingFlags.Static | BindingFlags.Public)));

            PopulateUncensorLists();

            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
            CharacterApi.RegisterExtraBehaviour<UncensorSelectorController>(GUID);

            GenderBender = new ConfigWrapper<bool>(nameof(GenderBender), PluginNameInternal, true);
            DefaultMaleBody = new ConfigWrapper<string>(nameof(DefaultMaleBody), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, MaleBodyDefaultValue);
            DefaultMalePenis = new ConfigWrapper<string>(nameof(DefaultMalePenis), PluginNameInternal, MalePenisGuidToDisplayName, DisplayNameToPenisGuid, MalePenisDefaultValue);
            DefaultMaleBalls = new ConfigWrapper<string>(nameof(DefaultMaleBalls), PluginNameInternal, MaleBallsGuidToDisplayName, DisplayNameToBallsGuid, MaleBallsDefaultValue);
            DefaultFemaleBody = new ConfigWrapper<string>(nameof(DefaultFemaleBody), PluginNameInternal, BodyGuidToDisplayName, DisplayNameToBodyGuid, FemaleBodyDefaultValue);
            DefaultFemalePenis = new ConfigWrapper<string>(nameof(DefaultFemalePenis), PluginNameInternal, FemalePenisGuidToDisplayName, DisplayNameToPenisGuid, FemalePenisDefaultValue);
            DefaultFemaleBalls = new ConfigWrapper<string>(nameof(DefaultFemaleBalls), PluginNameInternal, FemaleBallsGuidToDisplayName, DisplayNameToBallsGuid, FemaleBallsDefaultValue);
        }
        /// <summary>
        /// Initialize the character maker GUI
        /// </summary>
        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            DoDropdownEvents = false;
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
            BodyDropdown.ValueChanged.Subscribe(Observer.Create<int>(BodyDropdownChanged));
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
            PenisList.Add("None");
            PenisListDisplay.Add("None");

            foreach (PenisData penis in PenisDictionary.Select(x => x.Value).OrderByDescending(x => x.DisplayName))
            {
                PenisList.Add(penis.PenisGUID);
                PenisListDisplay.Add(penis.DisplayName);
            }

            PenisDropdown = e.AddControl(new MakerDropdown("Penis", PenisListDisplay.ToArray(), MakerConstants.Body.All, characterSex == 0 ? 0 : 1, this));
            PenisDropdown.ValueChanged.Subscribe(Observer.Create<int>(PenisDropdownChanged));
            void PenisDropdownChanged(int ID)
            {
                if (DoDropdownEvents == false)
                    return;

                var controller = GetController(MakerAPI.GetCharacterControl());
                controller.PenisGUID = ID == 0 || ID == 1 ? null : PenisList[ID];
                controller.DisplayPenis = ID == 1 ? false : true;
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

            BallsDropdown = e.AddControl(new MakerDropdown("Balls", BallsListDisplay.ToArray(), MakerConstants.Body.All, characterSex == 0 ? 0 : 1, this));
            BallsDropdown.ValueChanged.Subscribe(Observer.Create<int>(BallsDropdownChanged));
            void BallsDropdownChanged(int ID)
            {
                if (DoDropdownEvents == false)
                    return;

                var controller = GetController(MakerAPI.GetCharacterControl());
                controller.BallsGUID = ID == 0 || ID == 1 ? null : BallsList[ID];
                controller.DisplayBalls = ID == 1 ? false : true;
                controller.UpdateUncensor();
            }
        }
        /// <summary>
        /// Set initial values for the loaded character and enable dropdown events
        /// </summary>
        private void MakerAPI_MakerFinishedLoading(object sender, EventArgs e)
        {
            var controller = GetController(MakerAPI.GetCharacterControl());

            if (controller != null)
            {
                if (controller.BodyGUID != null && BodyList.IndexOf(controller.BodyGUID) != -1)
                    BodyDropdown.Value = BodyList.IndexOf(controller.BodyGUID);

                if (controller.PenisGUID != null && PenisList.IndexOf(controller.PenisGUID) != -1)
                    PenisDropdown.Value = PenisList.IndexOf(controller.PenisGUID);
                else if (controller.PenisGUID == null)
                    if (controller.DisplayPenis == false)
                        PenisDropdown.Value = 1;
                    else
                        PenisDropdown.Value = 0;

                if (controller.BallsGUID != null && BallsList.IndexOf(controller.BallsGUID) != -1)
                    BallsDropdown.Value = BallsList.IndexOf(controller.BallsGUID);
                else if (controller.BallsGUID == null)
                    if (controller.DisplayBalls)
                        BallsDropdown.Value = 0;
                    else
                        BallsDropdown.Value = 1;
            }

            DoDropdownEvents = true;
        }
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

            BodyData DefaultMale = new BodyData(0, "Default.Body.Male", "Default Body M");
            BodyDictionary.Add(DefaultMale.BodyGUID, DefaultMale);
            BodyConfigListFull.Add($"[{(DefaultMale.Sex == 0 ? "Male" : "Female")}] {DefaultMale.DisplayName}", DefaultMale.BodyGUID);

            BodyData DefaultFemale = new BodyData(1, "Default.Body.Female", "Default Body F");
            BodyDictionary.Add(DefaultFemale.BodyGUID, DefaultFemale);
            BodyConfigListFull.Add($"[{(DefaultFemale.Sex == 0 ? "Male" : "Female")}] {DefaultFemale.DisplayName}", DefaultFemale.BodyGUID);

            //Add the default penis options
            PenisConfigListFull.Add("None", UncensorKeyNone);
            PenisConfigListFull.Add("Random", UncensorKeyRandom);

            PenisData DefaultPenis = new PenisData("Default.Penis", "Mosaic Penis");
            PenisDictionary.Add(DefaultPenis.PenisGUID, DefaultPenis);
            PenisConfigListFull.Add(DefaultPenis.DisplayName, DefaultPenis.PenisGUID);

            //Add the default balls options
            BallsConfigListFull.Add("None", UncensorKeyNone);
            BallsConfigListFull.Add("Random", UncensorKeyRandom);

            BallsData DefaultBalls = new BallsData("Default.Balls", "Mosaic Balls");
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
                        BodyData bodyData = new BodyData(uncensorElement);
                        if (bodyData.BodyGUID == null)
                        {
                            Logger.Log(LogLevel.Warning, "Body failed to load due to missing GUID.");
                            continue;
                        }
                        if (bodyData.DisplayName == null)
                        {
                            Logger.Log(LogLevel.Warning, "Body failed to load due to missing display name.");
                            continue;
                        }
                        if (bodyData.OOBase == Defaults.OOBase)
                        {
                            Logger.Log(LogLevel.Warning, "Body was not loaded because oo_base is the default.");
                            continue;
                        }
                        BodyDictionary.Add(bodyData.BodyGUID, bodyData);
                        BodyConfigListFull.Add($"[{(bodyData.Sex == 0 ? "Male" : "Female")}] {bodyData.DisplayName}", bodyData.BodyGUID);
                        foreach (var part in bodyData.AdditionalParts)
                            AllAdditionalParts.Add(part);
                    }
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("penis"))
                    {
                        PenisData penisData = new PenisData(uncensorElement);
                        if (penisData.PenisGUID == null)
                        {
                            Logger.Log(LogLevel.Warning, "Penis failed to load due to missing GUID.");
                            continue;
                        }
                        if (penisData.DisplayName == null)
                        {
                            Logger.Log(LogLevel.Warning, "Penis failed to load due to missing display name.");
                            continue;
                        }
                        if (penisData.File == null)
                        {
                            Logger.Log(LogLevel.Warning, "Penis failed to load due to missing file.");
                            continue;
                        }
                        if (penisData.Asset == null)
                        {
                            Logger.Log(LogLevel.Warning, "Penis failed to load due to missing asset.");
                            continue;
                        }
                        PenisDictionary.Add(penisData.PenisGUID, penisData);
                        PenisConfigListFull.Add(penisData.DisplayName, penisData.PenisGUID);
                    }
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("balls"))
                    {
                        BallsData ballsData = new BallsData(uncensorElement);
                        if (ballsData.BallsGUID == null)
                        {
                            Logger.Log(LogLevel.Warning, "Balls failed to load due to missing GUID.");
                            continue;
                        }
                        if (ballsData.DisplayName == null)
                        {
                            Logger.Log(LogLevel.Warning, "Balls failed to load due to missing display name.");
                            continue;
                        }
                        if (ballsData.File == null)
                        {
                            Logger.Log(LogLevel.Warning, "Balls failed to load due to missing file.");
                            continue;
                        }
                        if (ballsData.Asset == null)
                        {
                            Logger.Log(LogLevel.Warning, "Balls failed to load due to missing asset.");
                            continue;
                        }
                        BallsDictionary.Add(ballsData.BallsGUID, ballsData);
                        BallsConfigListFull.Add(ballsData.DisplayName, ballsData.BallsGUID);
                    }
                    foreach (XElement uncensorElement in uncensorSelectorElement.Elements("migration"))
                    {
                        MigrationData migrationData = new MigrationData(uncensorElement);
                        if (migrationData.UncensorGUID == null)
                        {
                            Logger.Log(LogLevel.Warning, "Migration data failed to load due to missing Uncensor GUID.");
                            continue;
                        }
                        if (migrationData.BodyGUID == null)
                        {
                            Logger.Log(LogLevel.Warning, "Migration data failed to load due to missing Body GUID.");
                            continue;
                        }
                        MigrationDictionary.Add(migrationData.UncensorGUID, migrationData);
                    }
                }
            }
        }
        /// <summary>
        /// Check if the body is permitted in the character maker
        /// </summary>
        public static bool BodyAllowedInMaker(byte bodySex, byte characterSex)
        {
            if (GenderBender.Value)
                return true;

            if (bodySex == characterSex)
                return true;

            return false;
        }
        /// <summary>
        /// Returns the UncensorSelectorController for the specified character or null if it does not exist
        /// </summary>
        public static UncensorSelectorController GetController(ChaControl character) => character?.gameObject?.GetComponent<UncensorSelectorController>();
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
        internal static string SetMaleBodyLow()
        {
            Logger.Log(LogLevel.Info, "SetMaleBodyLow");
            return "p_cf_body_00_low";
        }
    }
}