using BepInEx;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
#if KK
    [BepInProcess(Constants.MainGameProcessNameSteam)]
#endif
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class Profile : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.profile";
        public const string PluginName = "Profile";
        public const string PluginNameInternal = Constants.Prefix + "_Profile";
        public const string Version = "1.0.1";

        internal static MakerTextbox ProfileTextbox;

        internal void Main()
        {
            CharacterApi.RegisterExtraBehaviour<ProfileController>(PluginNameInternal);
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
        }

        private void MakerAPI_MakerFinishedLoading(object sender, System.EventArgs e)
        {
            //Allow multi line text, rich text
            var inputfield = ProfileTextbox.ControlObject.GetComponentInChildren<TMP_InputField>();
            inputfield.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputfield.richText = true;
            inputfield.scrollSensitivity = 10;

            foreach (var text in inputfield.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.alignment = TextAlignmentOptions.TopLeft;
                text.fontSize = 22;
            }

            //Extend the background window
            ProfileTextbox.ControlObject.GetComponent<LayoutElement>().minHeight = 910;

            //Extend the textbox
            var rect = inputfield.GetComponent<RectTransform>();
            rect.offsetMax = new Vector2(rect.offsetMax.y, rect.offsetMax.y);
            rect.sizeDelta = new Vector2(460f, rect.sizeDelta.y);

            //Hide the reset button since that's a recipe for disaster
            var button = ProfileTextbox.ControlObject.GetComponentInChildren<Button>().gameObject;
            button.SetActive(false);
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            MakerCategory category = new MakerCategory(MakerConstants.Parameter.ADK.CategoryName, "Profile");
            e.AddSubCategory(category);
            e.AddControl(new MakerText("Character Description", category, this));
            ProfileTextbox = e.AddControl(new MakerTextbox(category, "", "", this));

            //Starts throwing errors at some point after 30,000 characters
            ProfileTextbox.CharacterLimit = 30000;
        }
    }
}
