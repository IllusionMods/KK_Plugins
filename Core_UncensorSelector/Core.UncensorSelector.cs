using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
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
#if AI
using AIChara;
#endif

namespace KK_Plugins
{
    /// <summary>
    /// Plugin for assigning uncensors to characters individually
    /// </summary>
    internal partial class UncensorSelector
    {
        public const string GUID = "com.deathweasel.bepinex.uncensorselector";
        public const string PluginName = "Uncensor Selector";
        public const string PluginNameInternal = "KK_UncensorSelector";
        public const string Version = "3.8.3";
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
        internal static MakerDropdown BodyDropdown = null;
        internal static MakerDropdown PenisDropdown = null;
        internal static MakerDropdown BallsDropdown = null;
        private static readonly HashSet<string> BodyNames = new HashSet<string>() { "o_body_a", "o_body_cf", "o_body_cm" };
        private static readonly HashSet<string> BodyParts = new HashSet<string>() { "o_dankon", "o_dan_f", "o_gomu", "o_mnpa", "o_mnpb", "o_shadowcaster", "cm_o_dan00", "cm_o_dan_f" };
        private static readonly HashSet<string> PenisParts = new HashSet<string>() { "o_dankon", "o_gomu", "cm_o_dan00" };
        private static readonly HashSet<string> BallsParts = new HashSet<string>() { "o_dan_f", "cm_o_dan_f" };
        internal static string CurrentBodyGUID;
        internal static bool DidErrorMessage = false;

        public static ConfigEntry<bool> GenderBender { get; private set; }
        public static ConfigEntry<string> DefaultMaleBody { get; private set; }
        public static ConfigEntry<string> DefaultMalePenis { get; private set; }
        public static ConfigEntry<string> DefaultMaleBalls { get; private set; }
        public static ConfigEntry<string> DefaultFemaleBody { get; private set; }
        public static ConfigEntry<string> DefaultFemalePenis { get; private set; }
        public static ConfigEntry<string> DefaultFemaleBalls { get; private set; }
        public static ConfigEntry<bool> DefaultFemaleDisplayBalls { get; private set; }

        internal void Start()
        {
            var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));

#if KK
            Type loadAsyncIterator = typeof(ChaControl).GetNestedTypes(AccessTools.all).First(x => x.Name.StartsWith("<LoadAsync>c__Iterator"));
            MethodInfo loadAsyncIteratorMoveNext = loadAsyncIterator.GetMethod("MoveNext");
            harmony.Patch(loadAsyncIteratorMoveNext, null, null, new HarmonyMethod(typeof(Hooks).GetMethod(nameof(Hooks.LoadAsyncTranspiler), AccessTools.all)));
#endif
        }

        internal void Main()
        {
            Logger = base.Logger;

            PopulateUncensorLists();

            GenderBender = Config.AddSetting("Config", "Genderbender Allowed", true, "Whether or not genderbender characters are allowed. When disabled, girls will always have a female body with no penis, boys will always have a male body and a penis. Genderbender characters will still load in Studio for scene compatibility.");
            DefaultMaleBody = Config.AddSetting("Config", "Default Male Body", "Random", new ConfigDescription("Body to use if the character does not have one set. The censored body will not be selected randomly if there are any alternatives.", new AcceptableValueList<string>(GetConfigBodyList())));
            DefaultMalePenis = Config.AddSetting("Config", "Default Male Penis", "Random", new ConfigDescription("Penis to use if the character does not have one set. The mosaic penis will not be selected randomly if there are any alternatives.", new AcceptableValueList<string>(GetConfigPenisList())));
            DefaultMaleBalls = Config.AddSetting("Config", "Default Male Balls", "Random", new ConfigDescription("Balls to use if the character does not have one set. The mosaic penis will not be selected randomly if there are any alternatives.", new AcceptableValueList<string>(GetConfigBallsList())));
            DefaultFemaleBody = Config.AddSetting("Config", "Default Female Body", "Random", new ConfigDescription("Body to use if the character does not have one set. The censored body will not be selected randomly if there are any alternatives.", new AcceptableValueList<string>(GetConfigBodyList())));
            DefaultFemalePenis = Config.AddSetting("Config", "Default Female Penis", "Random", new ConfigDescription("Penis to use if the character does not have one set. The mosaic penis will not be selected randomly if there are any alternatives.", new AcceptableValueList<string>(GetConfigPenisList())));
            DefaultFemaleBalls = Config.AddSetting("Config", "Default Female Balls", "Random", new ConfigDescription("Balls to use if the character does not have one set. The mosaic penis will not be selected randomly if there are any alternatives.", new AcceptableValueList<string>(GetConfigBallsList())));
            DefaultFemaleDisplayBalls = Config.AddSetting("Config", "Default Female Balls Display", false, "Whether balls will be displayed on females if not otherwise configured.");

            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
            CharacterApi.RegisterExtraBehaviour<UncensorSelectorController>(GUID);
#if !EC
            RegisterStudioControls();
#endif

            HarmonyWrapper.PatchAll(typeof(Hooks));
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
            BodyDropdown.ValueChanged.Subscribe(Observer.Create<int>(BodyDropdownChanged));
            void BodyDropdownChanged(int ID)
            {
                if (DoDropdownEvents == false)
                    return;

                var controller = GetController(MakerAPI.GetCharacterControl());
                controller.BodyGUID = ID == 0 ? null : BodyList[ID];
                controller.UpdateUncensor();
            }

#if !AI
            PenisList.Clear();
            PenisListDisplay.Clear();

            PenisList.Add("Default");
            PenisListDisplay.Add("Default");
#if KK || AI
            PenisList.Add("None");
            PenisListDisplay.Add("None");
#endif

            foreach (PenisData penis in PenisDictionary.Select(x => x.Value).OrderByDescending(x => x.DisplayName))
            {
                PenisList.Add(penis.PenisGUID);
                PenisListDisplay.Add(penis.DisplayName);
            }

#if KK || AI
            PenisDropdown = e.AddControl(new MakerDropdown("Penis", PenisListDisplay.ToArray(), MakerConstants.Body.All, characterSex == 0 ? 0 : 1, this));
#else
            PenisDropdown = e.AddControl(new MakerDropdown("Penis", PenisListDisplay.ToArray(), MakerConstants.Body.All, 0, this));
#endif
            PenisDropdown.ValueChanged.Subscribe(Observer.Create<int>(PenisDropdownChanged));
            void PenisDropdownChanged(int ID)
            {
                if (DoDropdownEvents == false)
                    return;

                var controller = GetController(MakerAPI.GetCharacterControl());
#if KK || AI
                controller.PenisGUID = ID == 0 || ID == 1 ? null : PenisList[ID];
                controller.DisplayPenis = ID == 1 ? false : true;
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

            int ballsInitialValue = characterSex == 0 ? 0 : DefaultFemaleDisplayBalls.Value == true ? 0 : 1;
            BallsDropdown = e.AddControl(new MakerDropdown("Balls", BallsListDisplay.ToArray(), MakerConstants.Body.All, ballsInitialValue, this));
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

#if EC
            if (characterSex == 1)
            {
                var dickToggle = e.AddControl(new MakerToggle(MakerConstants.Body.All, "Toggle Penis Display", characterSex == 0, this));
                dickToggle.ValueChanged.Subscribe(Observer.Create<bool>(delegate { MakerAPI.GetCharacterControl().fileStatus.visibleSonAlways = dickToggle.Value; }));
            }
#endif
#endif

            DoDropdownEvents = true;
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
                    BodyDropdown.SetValue(BodyList.IndexOf(controller.BodyGUID), false);

                if (controller.PenisGUID != null && PenisList.IndexOf(controller.PenisGUID) != -1)
                    PenisDropdown?.SetValue(PenisList.IndexOf(controller.PenisGUID), false);
                else if (controller.PenisGUID == null)
#if KK || AI
                    PenisDropdown?.SetValue(controller.DisplayPenis ? 0 : 1, false);
#else
                        PenisDropdown.SetValue(0, false);
#endif

                if (controller.BallsGUID != null && BallsList.IndexOf(controller.BallsGUID) != -1)
                    BallsDropdown?.SetValue(BallsList.IndexOf(controller.BallsGUID), false);
                else if (controller.BallsGUID == null)
                    BallsDropdown?.SetValue(controller.DisplayBalls ? 0 : 1, false);
            }
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
            BodyConfigListFull.Add("Random", "Random");

#if !AI
            BodyData DefaultMale = new BodyData(0, "Default.Body.Male", "Default Body M");
            BodyDictionary.Add(DefaultMale.BodyGUID, DefaultMale);
            BodyConfigListFull.Add($"[{(DefaultMale.Sex == 0 ? "Male" : "Female")}] {DefaultMale.DisplayName}", DefaultMale.BodyGUID);
#endif

            BodyData DefaultFemale = new BodyData(1, "Default.Body.Female", "Default Body F");
            BodyDictionary.Add(DefaultFemale.BodyGUID, DefaultFemale);
            BodyConfigListFull.Add($"[{(DefaultFemale.Sex == 0 ? "Male" : "Female")}] {DefaultFemale.DisplayName}", DefaultFemale.BodyGUID);

            //Add the default penis options
            PenisConfigListFull.Add("Random", "Random");

            PenisData DefaultPenis = new PenisData("Default.Penis", "Mosaic Penis");
            PenisDictionary.Add(DefaultPenis.PenisGUID, DefaultPenis);
            PenisConfigListFull.Add(DefaultPenis.DisplayName, DefaultPenis.PenisGUID);

            //Add the default balls options
            BallsConfigListFull.Add("Random", "Random");

            BallsData DefaultBalls = new BallsData("Default.Balls", "Mosaic Balls");
            BallsDictionary.Add(DefaultBalls.BallsGUID, DefaultBalls);
            BallsConfigListFull.Add(DefaultBalls.DisplayName, DefaultBalls.BallsGUID);

            var loadedManifests = Sideloader.Sideloader.LoadedManifests;

            foreach (var manifest in loadedManifests)
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
                        PenisDictionary.Add(penisData.PenisGUID, penisData);
                        PenisConfigListFull.Add(penisData.DisplayName, penisData.PenisGUID);
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
                        BallsDictionary.Add(ballsData.BallsGUID, ballsData);
                        BallsConfigListFull.Add(ballsData.DisplayName, ballsData.BallsGUID);
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

        internal string[] GetConfigBodyList() => BodyConfigListFull?.Keys.OrderBy(x => x[0] == '[').ThenBy(x => x).ToArray();
        internal string[] GetConfigPenisList() => PenisConfigListFull?.Keys.OrderBy(x => x).ToArray();
        internal string[] GetConfigBallsList() => BallsConfigListFull?.Keys.OrderBy(x => x).ToArray();

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
        internal static string SetMaleBodyLow() => "p_cf_body_00_low";
    }
}