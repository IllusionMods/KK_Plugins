using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static KK_Plugins.MoreOutfits.Plugin;

namespace KK_Plugins.MoreOutfits
{
    public static class StudioUI
    {
        private static CurrentStateCategoryDropdown StudioCoordinateCurrentStateCategoryDropdown;
        private static Dropdown StudioCoordinateDropdown;
        private static bool StudioUIInitialized = false;

#if KK
        private static readonly List<string> CoordinateNames = new List<string> { "学生服（校内）", "学生服（下校）", "体操着", "水着", "部活", "私服", "お泊り" };      
#elif KKS
        private static readonly List<string> CoordinateNames = new List<string> { "私服", "水着", "寝間着", "風呂場", "お風呂" };
#endif

        public static void RegisterStudioControls()
        {
            if (!StudioAPI.InsideStudio) return;

            StudioCoordinateCurrentStateCategoryDropdown = new CurrentStateCategoryDropdown("Coordinate", CoordinateNames.ToArray(), c => CoordinateIndex());
            StudioCoordinateCurrentStateCategoryDropdown.Value.Subscribe(value =>
            {
                var mpCharCtrol = UnityEngine.Object.FindObjectOfType<MPCharCtrl>();
                if (StudioCoordinateDropdown != null)
                {
                    var character = StudioAPI.GetSelectedCharacters().First();

                    //Remove extras
                    if (StudioCoordinateDropdown.options.Count > OriginalCoordinateLength)
                        StudioCoordinateDropdown.options.RemoveRange(OriginalCoordinateLength, StudioCoordinateDropdown.options.Count - OriginalCoordinateLength);

                    //Add dropdown options for each additional coodinate
                    if (StudioCoordinateDropdown.options.Count < character.charInfo.chaFile.coordinate.Length)
                    {
                        for (int i = 0; i < (character.charInfo.chaFile.coordinate.Length - OriginalCoordinateLength); i++)
                        {
                            StudioCoordinateDropdown.options.Add(new Dropdown.OptionData(GetCoodinateName(character.charInfo, OriginalCoordinateLength + i)));
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

        public static void InitializeStudioUI(MPCharCtrl mpCharCtrol)
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

#if KK
            //Rearange the UI to fill the gap left by the buttons (not needed in KKS)
            foreach (var button in mpCharCtrol.stateInfo.stateShoesType.buttons)
            {
                var position = button.transform.localPosition;
                button.transform.localPosition = new Vector3(position.x, position.y + 25, position.z);
            }

            foreach (var button in mpCharCtrol.stateInfo.buttonCosState)
            {
                var position = button.transform.localPosition;
                button.transform.localPosition = new Vector3(position.x, position.y + 25, position.z);
            }

            var shoesText = parentTransform.Find("Text Shoes");
            if (shoesText != null)
            {
                var position = shoesText.transform.localPosition;
                shoesText.transform.localPosition = new Vector3(position.x, position.y + 25, position.z);
            }

            var setText = parentTransform.Find("Text Set");
            if (setText != null)
            {
                var position = setText.transform.localPosition;
                setText.transform.localPosition = new Vector3(position.x, position.y + 25, position.z);
            }

            var layoutElement = parentTransform.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredHeight -= 20f;
            }
#endif
        }
    }
}
