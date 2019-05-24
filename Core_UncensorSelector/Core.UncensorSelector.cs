using BepInEx;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UniRx;
#if KK
using Sideloader;
#elif EC
using EC.Core.Sideloader;
#endif

namespace UncensorSelector
{
    /// <summary>
    /// Plugin for assigning uncensors to characters individually
    /// </summary>
    internal partial class UncensorSelector
    {
        public const string GUID = "com.deathweasel.bepinex.uncensorselector";
        public const string PluginName = "Uncensor Selector";
        public const string PluginNameInternal = "KK_UncensorSelector";
        public const string Version = "3.6.1";
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
        private static MakerDropdown BodyDropdown;
        private static MakerDropdown PenisDropdown;
        private static MakerDropdown BallsDropdown;
        private static readonly HashSet<string> BodyParts = new HashSet<string>() { "o_dankon", "o_dan_f", "o_gomu", "o_mnpa", "o_mnpb", "o_shadowcaster" };
        private static readonly HashSet<string> PenisParts = new HashSet<string>() { "o_dankon", "o_gomu" };
        private static readonly HashSet<string> BallsParts = new HashSet<string>() { "o_dan_f" };
        internal static string CurrentBodyGUID;

        private void Main()
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

            PopulateUncensorLists();

            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
            CharacterApi.RegisterExtraBehaviour<UncensorSelectorController>(GUID);
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
#elif EC
            PenisDropdown = e.AddControl(new MakerDropdown("Penis", PenisListDisplay.ToArray(), MakerConstants.Body.All, 0, this));
#endif
            PenisDropdown.ValueChanged.Subscribe(Observer.Create<int>(PenisDropdownChanged));
            void PenisDropdownChanged(int ID)
            {
                if (DoDropdownEvents == false)
                    return;

                var controller = GetController(MakerAPI.GetCharacterControl());
#if KK
                controller.PenisGUID = ID == 0 || ID == 1 ? null : PenisList[ID];
                controller.DisplayPenis = ID == 1 ? false : true;
#elif EC
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
                    PenisDropdown.SetValue(PenisList.IndexOf(controller.PenisGUID), false);
                else if (controller.PenisGUID == null)
#if KK
                    PenisDropdown.SetValue(controller.DisplayPenis ? 0 : 1, false);
#elif EC
                    PenisDropdown.SetValue(0, false);
#endif

                if (controller.BallsGUID != null && BallsList.IndexOf(controller.BallsGUID) != -1)
                    BallsDropdown.SetValue(BallsList.IndexOf(controller.BallsGUID), false);
                else if (controller.BallsGUID == null)
                    BallsDropdown.SetValue(controller.DisplayBalls ? 0 : 1, false);

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

            BodyData DefaultMale = new BodyData(0, "Default.Body.Male", "Default Body M");
            BodyDictionary.Add(DefaultMale.BodyGUID, DefaultMale);
            BodyConfigListFull.Add($"[{(DefaultMale.Sex == 0 ? "Male" : "Female")}] {DefaultMale.DisplayName}", DefaultMale.BodyGUID);

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

#if KK
            var loadedManifests = Sideloader.Sideloader.LoadedManifests;
#elif EC
            var loadedManifests = Sideloader.LoadedManifests;
#endif

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
                            Log(LogLevel.Warning, "Body failed to load due to missing GUID.");
                            continue;
                        }
                        if (bodyData.DisplayName == null)
                        {
                            Log(LogLevel.Warning, "Body failed to load due to missing display name.");
                            continue;
                        }
                        if (bodyData.OOBase == Defaults.OOBase)
                        {
                            Log(LogLevel.Warning, "Body was not loaded because oo_base is the default.");
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
                            Log(LogLevel.Warning, "Penis failed to load due to missing GUID.");
                            continue;
                        }
                        if (penisData.DisplayName == null)
                        {
                            Log(LogLevel.Warning, "Penis failed to load due to missing display name.");
                            continue;
                        }
                        if (penisData.File == null)
                        {
                            Log(LogLevel.Warning, "Penis failed to load due to missing file.");
                            continue;
                        }
                        if (penisData.Asset == null)
                        {
                            Log(LogLevel.Warning, "Penis failed to load due to missing asset.");
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
                            Log(LogLevel.Warning, "Balls failed to load due to missing GUID.");
                            continue;
                        }
                        if (ballsData.DisplayName == null)
                        {
                            Log(LogLevel.Warning, "Balls failed to load due to missing display name.");
                            continue;
                        }
                        if (ballsData.File == null)
                        {
                            Log(LogLevel.Warning, "Balls failed to load due to missing file.");
                            continue;
                        }
                        if (ballsData.Asset == null)
                        {
                            Log(LogLevel.Warning, "Balls failed to load due to missing asset.");
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
                            Log(LogLevel.Warning, "Migration data failed to load due to missing Uncensor GUID.");
                            continue;
                        }
                        if (migrationData.BodyGUID == null)
                        {
                            Log(LogLevel.Warning, "Migration data failed to load due to missing Body GUID.");
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
        private object[] GetConfigBodyList() => BodyConfigListFull?.Keys.OrderBy(x => x[0] == '[').ThenBy(x => x).Cast<object>().ToArray();
        private object[] GetConfigPenisList() => PenisConfigListFull?.Keys.OrderBy(x => x).Cast<object>().ToArray();
        private object[] GetConfigBallsList() => BallsConfigListFull?.Keys.OrderBy(x => x).Cast<object>().ToArray();
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
        private static string PenisGuidToDisplayName(string guid) => PenisConfigListFull.FirstOrDefault(x => x.Value == guid).Key ?? "Random";
        private static string BallsGuidToDisplayName(string guid) => BallsConfigListFull.FirstOrDefault(x => x.Value == guid).Key ?? "Random";
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