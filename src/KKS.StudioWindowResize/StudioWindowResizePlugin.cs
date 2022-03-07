using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using KKAPI;
using KKAPI.Studio;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;

/* by essu
todo add incompat attribute with sabakans version
dict shouldn't be global
const too
lots of arbitrary braces to stop confusing intellisense
extending it for new images is straightforward
dump image to disk, bring it into paint or unity
unity lets you see the result of adjusting borders with 9 slice
copy values back to code using (LEFT, BOTTOM, RIGHT, TOP) borders in vector4
the Y offset is entirely arbitrary, not sure if it plays nice across aspect ratios or resize, probably not
allowing the user to configure it wouldn't be too involved.
it would just be a case of separating sprite operations from resize operations, not a big deal
other than this, adjusts scrollbar anchors to "fill parent"
er no, viewport mask sorry
and adjusts scrollbar max value to fill parent based on parent rectransform min value + 20

this entire thing was reduced over and over until the only meaningful values were offsetMin (which tends to do just fine hardcoded) and sprite slicing
*/

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginNameInternal, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInProcess(Constants.StudioProcessName)]
    public class StudioWindowResizePlugin : BaseUnityPlugin
    {
        public const string GUID = "StudioWindowResize";
        public const string PluginName = "StudioWindowResize";
        public const string Version = "1.0";
        public const string PluginNameInternal = Constants.Prefix + "_StudioWindowResize";

        private const float resizeY = -973;

        private Dictionary<string, Vector4> sliceInfo = new Dictionary<string, Vector4>()
        {
            { "ADD_CHARA", new Vector4(106, 14, 19, 29) },
            { "ADD_ITEM", new Vector4(0, 16, 0, 16) },
            { "ADD_BG", new Vector4(145, 13, 19, 29) }
        };

        private static void SliceTexture(Image image, Vector4 border)
        {
            var tr = image.sprite.textureRect;
            var newTexture = new Texture2D((int)tr.width, (int)tr.height);
            Graphics.CopyTexture(image.sprite.texture, 0, 0, (int)tr.x, (int)tr.y, (int)tr.width, (int)tr.height, newTexture, 0, 0, 0, 0);
            //TODO: skip copy entirely, use source texture + offset.

            //Debug: dump background image to measure borders.
            //File.WriteAllBytes("e:\\image.png", );
            //DestroyImmediate(newTexture);

            var s = Sprite.Create(newTexture,
                new Rect(0, 0, newTexture.width, newTexture.height),
                new Vector2(0, newTexture.height),
                70f,   //TODO: derive //todo use image.sprite.pixelsPerUnit?
                0,
                SpriteMeshType.FullRect,
                border
            );

            image.sprite = s;
            image.type = Image.Type.Sliced;

            //In a perfect world,
            //Garbage in memory would not exist
            //But this is not a perfect world.
            //Destroying the old sprites has side effects with other UI elements.
        }

        private static void ResizeScrollRect(Transform t, Vector4 slice)
        {
            var sr = t.GetComponent<ScrollRect>();
            var rt = sr.GetComponent<RectTransform>();
            var offsetMin = rt.offsetMin;
            offsetMin.y = resizeY;
            rt.offsetMin = offsetMin;
            SliceTexture(sr.GetComponent<Image>(), slice);
            var vp = sr.viewport;
            vp.anchorMin = new Vector2(0, 0);
            vp.anchorMax = new Vector2(1, 1);
            var sb = sr.verticalScrollbar.GetComponent<RectTransform>();
            sb.offsetMin = new Vector2(sb.offsetMin.x, rt.offsetMin.y + 20);
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => StudioAPI.StudioLoaded);

            //var screenDimensions = new Vector2(Screen.width, Screen.height);

            var studioScene = Studio.Studio.Instance.transform;

            var mainMenu = studioScene.Find("Canvas Main Menu");

            //TODO: Derive magics from other UI elements + scale factor? Nice to have.

            //var scaler = mainMenu.GetComponent<CanvasScaler>();
            //var scaleFactor = screenDimensions / scaler.referenceResolution;

            //var guideInput = studioScene.Find("Canvas Guide Input/Guide Input").GetComponent<RectTransform>();
            //var guideInputHeight = guideInput.sizeDelta.y;  //80
            //var guideInputMargin = guideInput.offsetMax.y - guideInputHeight;   //88 - 80 -> 8

            //Add Menu
            {
                var addMenu = mainMenu.Find("01_Add");

                ResizeScrollRect(addMenu.Find("00_Female").Find("Scroll View"), sliceInfo["ADD_CHARA"]);
                ResizeScrollRect(addMenu.Find("01_Male").Find("Scroll View"), sliceInfo["ADD_CHARA"]);

                var addItem = addMenu.Find("02_Item");
                ResizeScrollRect(addItem.Find("Scroll View Group"), sliceInfo["ADD_ITEM"]);
                ResizeScrollRect(addItem.Find("Scroll View Category"), sliceInfo["ADD_ITEM"]);
                ResizeScrollRect(addItem.Find("Scroll View Item"), sliceInfo["ADD_ITEM"]);

                var addMap = addMenu.Find("03_Map");
                ResizeScrollRect(addMap.Find("Map Category"), sliceInfo["ADD_ITEM"]);
                ResizeScrollRect(addMap.Find("Map"), sliceInfo["ADD_ITEM"]);


                ResizeScrollRect(addMenu.Find("04_Light"), sliceInfo["ADD_ITEM"]);
                ResizeScrollRect(addMenu.Find("05_Background"), sliceInfo["ADD_BG"]);
                ResizeScrollRect(addMenu.Find("06_Frame"), sliceInfo["ADD_BG"]);
                ResizeScrollRect(addMenu.Find("07_Text"), sliceInfo["ADD_ITEM"]);
            }
            //Manipulate Menu
            {
                var manipulateMenu = mainMenu.Find("02_Manipulate");
                {
                    var manipulateChara = manipulateMenu.Find("00_Chara");
                    ResizeScrollRect(manipulateChara.Find("01_State"), sliceInfo["ADD_BG"]);
                    ResizeScrollRect(manipulateChara.Find("05_Costume").Find("Scroll View"), sliceInfo["ADD_CHARA"]);
                }
                //TODO: looks like anim -> kinematics -> pose menu has a delete button underneath. Relocate it after resize.
                {
                    var manipulateAnime = manipulateMenu.Find("03_Anime");
                    ResizeScrollRect(manipulateAnime.Find("Group Panel"), sliceInfo["ADD_ITEM"]);
                    ResizeScrollRect(manipulateAnime.Find("Category Panel"), sliceInfo["ADD_ITEM"]);
                    ResizeScrollRect(manipulateAnime.Find("Anime Panel"), sliceInfo["ADD_ITEM"]);
                }
            }
            //Sound Menu
            {
                var soundMenu = mainMenu.Find("03_Sound");

                ResizeScrollRect(soundMenu.Find("00_BGM Player").Find("Scroll View"), sliceInfo["ADD_ITEM"]);
                ResizeScrollRect(soundMenu.Find("01_ENV Player").Find("Bottom Scroll View"), sliceInfo["ADD_ITEM"]);
                ResizeScrollRect(soundMenu.Find("02_Outside Player").Find("Bottom Scroll View"), sliceInfo["ADD_ITEM"]);
            }
            //System Menu
            {
                var systemMenu = mainMenu.Find("04_System");

                ResizeScrollRect(systemMenu.Find("01_Screen Effect").Find("Screen Effect"), sliceInfo["ADD_ITEM"]);
                ResizeScrollRect(systemMenu.Find("02_Option").Find("Option"), sliceInfo["ADD_ITEM"]);
            }
        }
    }
}