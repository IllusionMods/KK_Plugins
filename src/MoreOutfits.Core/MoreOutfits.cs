using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using System;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace KK_Plugins
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MoreOutfits : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.moreoutfits";
        public const string PluginName = "More Outfit Slots";
        public const string PluginNameInternal = Constants.Prefix + "_MoreOutfits";
        public const string PluginVersion = "1.0";
        internal static new ManualLogSource Logger;

        private const string TextboxDefault = "Outfit #";
        private static readonly int OriginalCoordinateLength = Enum.GetNames(typeof(ChaFileDefine.CoordinateType)).Length;
        private MakerText RenameCoordinateText;
        private MakerDropdown RenameCoordinateDropdown;
        private MakerTextbox RenameCoordinateTextbox;

        private void Awake()
        {
            Logger = base.Logger;
            MakerAPI.ReloadCustomInterface += MakerAPI_ReloadCustomInterface;
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            CharacterApi.RegisterExtraBehaviour<MoreOutfitsController>(PluginGUID);
            RegisterStudioControls();

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private void MakerAPI_ReloadCustomInterface(object sender, EventArgs e)
        {
            UpdateMakerUI();
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent ev)
        {
            MakerCategory category = new MakerCategory("03_ClothesTop", "tglSettings", MakerConstants.Clothes.Copy.Position + 1, "Settings");

            var addRemoveText = new MakerText("Add or remove outfit slots", category, this);
            ev.AddControl(addRemoveText);

            var coordinateNameTextbox = new MakerTextbox(category, "Outfit Name", TextboxDefault, this);
            ev.AddControl(coordinateNameTextbox);

            var addCoordinateButton = new MakerButton("Add additional clothing slot", category, this);
            ev.AddControl(addCoordinateButton);
            addCoordinateButton.OnClick.AddListener(() =>
            {
                var chaControl = MakerAPI.GetCharacterControl();
                var controller = GetController(chaControl);
                if (controller != null)
                    controller.SetCoordinateName(chaControl.chaFile.coordinate.Length, coordinateNameTextbox.Value);
                AddCoordinateSlot(chaControl);
            });

            var removeCoordinateButton = new MakerButton("Remove last additional clothing slot", category, this);
            ev.AddControl(removeCoordinateButton);
            removeCoordinateButton.OnClick.AddListener(() => { RemoveCoordinateSlot(MakerAPI.GetCharacterControl()); });

            RenameCoordinateText = new MakerText("Rename outfit slots", category, this);
            ev.AddControl(RenameCoordinateText);

            RenameCoordinateDropdown = new MakerDropdown("Outfit", new string[] { "none" }, category, 0, this);
            ev.AddControl(RenameCoordinateDropdown);

            RenameCoordinateTextbox = new MakerTextbox(category, "New Name", TextboxDefault, this);
            RenameCoordinateTextbox.ValueChanged.Subscribe(value =>
            {
                if (value != TextboxDefault)
                {
                    var chaControl = MakerAPI.GetCharacterControl();
                    var controller = GetController(chaControl);
                    if (controller != null)
                        controller.SetCoordinateName(OriginalCoordinateLength + RenameCoordinateDropdown.Value, value);
                    UpdateMakerUI();
                }
            });
            ev.AddControl(RenameCoordinateTextbox);

            ev.AddSubCategory(category);
        }

        private static void RegisterStudioControls()
        {
            if (!StudioAPI.InsideStudio) return;

            var coordinateDropdown = new CurrentStateCategoryDropdown("Coordinate", Enum.GetNames(typeof(ChaFileDefine.CoordinateType)), c => CoordinateIndex());
            coordinateDropdown.Value.Subscribe(value =>
            {
                var mpCharCtrol = FindObjectOfType<Studio.MPCharCtrl>();
                if (coordinateDropdown.RootGameObject != null)
                {
                    var dd = coordinateDropdown.RootGameObject.GetComponentInChildren<Dropdown>();

                    var character = StudioAPI.GetSelectedCharacters().First();
                    var controller = GetController(character.charInfo);

                    //Remove extras
                    if (dd.options.Count > OriginalCoordinateLength)
                        dd.options.RemoveRange(OriginalCoordinateLength, dd.options.Count - OriginalCoordinateLength);

                    //Add dropdown options for each additional coodinate
                    if (dd.options.Count < character.charInfo.chaFile.coordinate.Length)
                    {
                        for (int i = 0; i < (character.charInfo.chaFile.coordinate.Length - OriginalCoordinateLength); i++)
                        {
                            int slot = OriginalCoordinateLength + i;
                            string name;
                            if (controller == null)
                                name = TextboxDefault.Replace("#", $"{slot + 1}");
                            else
                                name = controller.GetCoodinateName(slot);
                            dd.options.Add(new Dropdown.OptionData(name));
                        }
                        dd.captionText.text = dd.options[dd.value].text;
                    }
                }

                if (mpCharCtrol != null)
                    mpCharCtrol.stateInfo.OnClickCosType(value);
            });

            int CoordinateIndex()
            {
                var character = StudioAPI.GetSelectedCharacters().First();
                return character.charInfo.fileStatus.coordinateType;
            }

            StudioAPI.GetOrCreateCurrentStateCategory("").AddControl(coordinateDropdown);
        }

        /// <summary>
        /// Add another coordinate slot for the specified character
        /// </summary>
        /// <param name="chaControl">The character being modified</param>
        public void AddCoordinateSlot(ChaControl chaControl)
        {
            //Initialize a new bigger array, copy the contents of the old
            var newCoordinate = new ChaFileCoordinate[chaControl.chaFile.coordinate.Length + 1];
            for (int i = 0; i < chaControl.chaFile.coordinate.Length; i++)
                newCoordinate[i] = chaControl.chaFile.coordinate[i];
            newCoordinate[newCoordinate.Length - 1] = new ChaFileCoordinate();
            chaControl.chaFile.coordinate = newCoordinate;

            UpdateMakerUI();
        }

        /// <summary>
        /// Remove the last added coordinate slot for the specified character
        /// </summary>
        /// <param name="chaControl">The character being modified</param>
        public void RemoveCoordinateSlot(ChaControl chaControl)
        {
            //Initialize a new smaller array, copy the contents of the old
            if (chaControl.chaFile.coordinate.Length <= OriginalCoordinateLength)
                return;

            var newCoordinate = new ChaFileCoordinate[chaControl.chaFile.coordinate.Length - 1];
            for (int i = 0; i < newCoordinate.Length; i++)
                newCoordinate[i] = chaControl.chaFile.coordinate[i];
            chaControl.chaFile.coordinate = newCoordinate;

            UpdateMakerUI();
        }

        /// <summary>
        /// Expand the dropdowns in character maker to include the additional coordinates, or remove them if necessary.
        /// Show or hide coordinate rename stuff.
        /// </summary>
        private void UpdateMakerUI()
        {
            if (!MakerAPI.InsideMaker)
                return;

            var chaControl = MakerAPI.GetCharacterControl();
            var controller = GetController(chaControl);

            //Remove extras
            var customControl = FindObjectOfType<CustomControl>();

            if (customControl.ddCoordinate.m_Options.m_Options.Count > OriginalCoordinateLength)
                customControl.ddCoordinate.m_Options.m_Options.RemoveRange(OriginalCoordinateLength, customControl.ddCoordinate.m_Options.m_Options.Count - OriginalCoordinateLength);

            var cvsCopy = CustomBase.Instance.GetComponentInChildren<CvsClothesCopy>(true);
            if (cvsCopy.ddCoordeType[0].m_Options.m_Options.Count > OriginalCoordinateLength)
                cvsCopy.ddCoordeType[0].m_Options.m_Options.RemoveRange(OriginalCoordinateLength, cvsCopy.ddCoordeType[0].m_Options.m_Options.Count - OriginalCoordinateLength);
            if (cvsCopy.ddCoordeType[1].m_Options.m_Options.Count > OriginalCoordinateLength)
                cvsCopy.ddCoordeType[1].m_Options.m_Options.RemoveRange(OriginalCoordinateLength, cvsCopy.ddCoordeType[1].m_Options.m_Options.Count - OriginalCoordinateLength);

            var cvsAccessoryCopy = CustomBase.Instance.GetComponentInChildren<CvsAccessoryCopy>(true);
            if (cvsAccessoryCopy.ddCoordeType[0].m_Options.m_Options.Count > OriginalCoordinateLength)
                cvsAccessoryCopy.ddCoordeType[0].m_Options.m_Options.RemoveRange(OriginalCoordinateLength, cvsAccessoryCopy.ddCoordeType[0].m_Options.m_Options.Count - OriginalCoordinateLength);
            if (cvsAccessoryCopy.ddCoordeType[1].m_Options.m_Options.Count > OriginalCoordinateLength)
                cvsAccessoryCopy.ddCoordeType[1].m_Options.m_Options.RemoveRange(OriginalCoordinateLength, cvsAccessoryCopy.ddCoordeType[1].m_Options.m_Options.Count - OriginalCoordinateLength);

            if (chaControl.chaFile.coordinate.Length > OriginalCoordinateLength)
            {
                RenameCoordinateText.Visible.OnNext(true);
                RenameCoordinateDropdown.Visible.OnNext(true);
                RenameCoordinateTextbox.Visible.OnNext(true);

                var ddRename = RenameCoordinateDropdown.ControlObject.GetComponentInChildren<TMP_Dropdown>();
                ddRename.m_Options.m_Options.Clear();

                //Add dropdown options for each additional coodinate
                for (int i = 0; i < (chaControl.chaFile.coordinate.Length - OriginalCoordinateLength); i++)
                {
                    int slot = OriginalCoordinateLength + i;
                    string name;
                    if (controller == null)
                        name = TextboxDefault.Replace("#", $"{slot + 1}");
                    else
                        name = controller.GetCoodinateName(slot);
                    customControl.ddCoordinate.m_Options.m_Options.Add(new TMP_Dropdown.OptionData(name));
                    cvsCopy.ddCoordeType[0].m_Options.m_Options.Add(new TMP_Dropdown.OptionData(name));
                    cvsCopy.ddCoordeType[1].m_Options.m_Options.Add(new TMP_Dropdown.OptionData(name));
                    cvsAccessoryCopy.ddCoordeType[0].m_Options.m_Options.Add(new TMP_Dropdown.OptionData(name));
                    cvsAccessoryCopy.ddCoordeType[1].m_Options.m_Options.Add(new TMP_Dropdown.OptionData(name));
                    ddRename.m_Options.m_Options.Add(new TMP_Dropdown.OptionData(name));
                }

                if (ddRename.value >= ddRename.m_Options.m_Options.Count)
                    ddRename.value = 0;
                else
                    ddRename.captionText.text = ddRename.m_Options.m_Options[ddRename.value].m_Text;
            }
            else
            {
                RenameCoordinateText.Visible.OnNext(false);
                RenameCoordinateDropdown.Visible.OnNext(false);
                RenameCoordinateTextbox.Visible.OnNext(false);
            }

            //Change outfits if the dropdown no longer contains the selected value
            if (customControl.ddCoordinate.value >= chaControl.chaFile.coordinate.Length)
                customControl.ddCoordinate.value = 0;
        }

        public static MoreOutfitsController GetController(ChaControl character) => character == null || character.gameObject == null ? null : character.gameObject.GetComponent<MoreOutfitsController>();
    }
}
