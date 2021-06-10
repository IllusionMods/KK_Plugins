using ChaCustom;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System;
using TMPro;
using UniRx;
using UnityEngine;
using static KK_Plugins.MoreOutfits.Plugin;

namespace KK_Plugins.MoreOutfits
{
    internal static class MakerUI
    {
        private static MakerText RenameCoordinateText;
        private static MakerDropdown RenameCoordinateDropdown;
        private static MakerTextbox RenameCoordinateTextbox;

        internal static void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent ev)
        {
            MakerCategory category = new MakerCategory("03_ClothesTop", "tglSettings", MakerConstants.Clothes.Copy.Position + 1, "Settings");

            var addRemoveText = new MakerText("Add or remove outfit slots", category, Instance);
            ev.AddControl(addRemoveText);

            var coordinateNameTextbox = new MakerTextbox(category, "Outfit Name", TextboxDefault, Instance);
            ev.AddControl(coordinateNameTextbox);

            var addCoordinateButton = new MakerButton("Add additional clothing slot", category, Instance);
            ev.AddControl(addCoordinateButton);
            addCoordinateButton.OnClick.AddListener(() =>
            {
                var chaControl = MakerAPI.GetCharacterControl();
                SetCoordinateName(chaControl, chaControl.chaFile.coordinate.Length, coordinateNameTextbox.Value);
                AddCoordinateSlot(chaControl);
            });

            var removeCoordinateButton = new MakerButton("Remove last additional clothing slot", category, Instance);
            ev.AddControl(removeCoordinateButton);
            removeCoordinateButton.OnClick.AddListener(() => { RemoveCoordinateSlot(MakerAPI.GetCharacterControl()); });

            RenameCoordinateText = new MakerText("Rename outfit slots", category, Instance);
            ev.AddControl(RenameCoordinateText);

            RenameCoordinateDropdown = new MakerDropdown("Outfit", new string[] { "none" }, category, 0, Instance);
            ev.AddControl(RenameCoordinateDropdown);

            RenameCoordinateTextbox = new MakerTextbox(category, "New Name", TextboxDefault, Instance);
            RenameCoordinateTextbox.ValueChanged.Subscribe(value =>
            {
                if (value != TextboxDefault)
                {
                    var chaControl = MakerAPI.GetCharacterControl();
                    SetCoordinateName(chaControl, OriginalCoordinateLength + RenameCoordinateDropdown.Value, coordinateNameTextbox.Value);
                    UpdateMakerUI();
                }
            });
            ev.AddControl(RenameCoordinateTextbox);

            ev.AddSubCategory(category);
        }

        internal static void MakerAPI_ReloadCustomInterface(object sender, EventArgs e)
        {
            UpdateMakerUI();
        }

        /// <summary>
        /// Expand the dropdowns in character maker to include the additional coordinates, or remove them if necessary.
        /// Show or hide coordinate rename stuff.
        /// </summary>
        public static void UpdateMakerUI()
        {
            if (!MakerAPI.InsideMaker)
                return;

            var chaControl = MakerAPI.GetCharacterControl();

            //Remove extras
            var customControl = GameObject.FindObjectOfType<CustomControl>();

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
                    string name = GetCoodinateName(chaControl, OriginalCoordinateLength + i);
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
    }
}
