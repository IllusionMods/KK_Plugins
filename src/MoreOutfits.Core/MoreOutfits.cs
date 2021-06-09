using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
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
        private static CurrentStateCategoryDropdown StudioCoordinateCurrentStateCategoryDropdown;
        private static Dropdown StudioCoordinateDropdown;
        private static bool StudioUIInitialized = false;

        private static readonly List<string> CoordinateNames = new List<string> { "学生服（校内）", "学生服（下校）", "体操着", "水着", "部活", "私服", "お泊り" };

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

            StudioCoordinateCurrentStateCategoryDropdown = new CurrentStateCategoryDropdown("Coordinate", CoordinateNames.ToArray(), c => CoordinateIndex());
            StudioCoordinateCurrentStateCategoryDropdown.Value.Subscribe(value =>
            {
                var mpCharCtrol = FindObjectOfType<MPCharCtrl>();
                if (StudioCoordinateDropdown != null)
                {
                    var character = StudioAPI.GetSelectedCharacters().First();
                    var controller = GetController(character.charInfo);

                    //Remove extras
                    if (StudioCoordinateDropdown.options.Count > OriginalCoordinateLength)
                        StudioCoordinateDropdown.options.RemoveRange(OriginalCoordinateLength, StudioCoordinateDropdown.options.Count - OriginalCoordinateLength);

                    //Add dropdown options for each additional coodinate
                    if (StudioCoordinateDropdown.options.Count < character.charInfo.chaFile.coordinate.Length)
                    {
                        for (int i = 0; i < (character.charInfo.chaFile.coordinate.Length - OriginalCoordinateLength); i++)
                        {
                            int slot = OriginalCoordinateLength + i;
                            string name;
                            if (controller == null)
                                name = TextboxDefault.Replace("#", $"{slot + 1}");
                            else
                                name = controller.GetCoodinateName(slot);
                            StudioCoordinateDropdown.options.Add(new Dropdown.OptionData(name));
                        }
                        StudioCoordinateDropdown.captionText.text = StudioCoordinateDropdown.options[StudioCoordinateDropdown.value].text;
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

            StudioAPI.GetOrCreateCurrentStateCategory("").AddControl(StudioCoordinateCurrentStateCategoryDropdown);
        }

        internal static void InitializeStudioUI(MPCharCtrl mpCharCtrol)
        {
            if (StudioUIInitialized)
                return;
            StudioUIInitialized = true;

            //Disable the coordinate buttons and replace them with a dropdown
            var button0 = mpCharCtrol.stateInfo.stateCosType.buttons[0];
            var parentTransform = button0.transform.parent;
            StudioCoordinateDropdown = StudioCoordinateCurrentStateCategoryDropdown.RootGameObject.GetComponentInChildren<Dropdown>();
            StudioCoordinateDropdown.transform.SetParent(parentTransform);
            StudioCoordinateDropdown.transform.localPosition = button0.transform.localPosition;
            foreach (var button in mpCharCtrol.stateInfo.stateCosType.buttons)
                button.gameObject.SetActive(false);
            StudioCoordinateCurrentStateCategoryDropdown.RootGameObject.SetActive(false);
            var rectTransform = StudioCoordinateDropdown.gameObject.GetComponent<RectTransform>();
            var offset = rectTransform.offsetMin;
            rectTransform.offsetMin = new Vector2(offset.x - 50, offset.y);


            //Rearange the UI to fill the gap left by the buttons
            foreach (var button in mpCharCtrol.stateInfo.stateShoesType.buttons)
            {
                var position = button.transform.localPosition;
                button.transform.localPosition = new UnityEngine.Vector3(position.x, position.y + 25, position.z);
            }

            foreach (var button in mpCharCtrol.stateInfo.buttonCosState)
            {
                var position = button.transform.localPosition;
                button.transform.localPosition = new UnityEngine.Vector3(position.x, position.y + 25, position.z);
            }

            var shoesText = parentTransform.Find("Text Shoes");
            if (shoesText != null)
            {
                var position = shoesText.transform.localPosition;
                shoesText.transform.localPosition = new UnityEngine.Vector3(position.x, position.y + 25, position.z);
            }

            var setText = parentTransform.Find("Text Set");
            if (setText != null)
            {
                var position = setText.transform.localPosition;
                setText.transform.localPosition = new UnityEngine.Vector3(position.x, position.y + 25, position.z);
            }

            var layoutElement = parentTransform.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredHeight -= 20f;
            }
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
