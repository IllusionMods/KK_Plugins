using System;
using System.Collections;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KKAPI;
using KKAPI.Studio;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    // Based on based work by based essu
    [BepInPlugin(GUID, PluginNameInternal, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInIncompatibility("Fix_StudioWindow")] // guid of original Sabakan's version without texture slicing
    public class StudioWindowResizePlugin : BaseUnityPlugin
    {
        public const string GUID = "StudioWindowResize";
        public const string PluginName = "StudioWindowResize";
        public const string Version = "1.1";
        public const string PluginNameInternal = Constants.Prefix + "_StudioWindowResize";

        // the Y offset is entirely arbitrary, not sure if it plays nice across aspect ratios or resize, probably not
        // allowing the user to configure it wouldn't be too involved.
        // it would just be a case of separating sprite operations from resize operations, not a big deal
        private static float resizeY = -973;
        private const float resizeYmin = -300;
        //private const float resizeYdefault = -950;//-973;
        private const float resizeYmax = -1400;

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

            // Use a relatively low number by default to not change how the UI looks too drastically while still providing most of the benefit
            var defaultRatio = 0.3f; //(resizeYdefault - resizeYmin) / (resizeYmax - resizeYmin);

            var userRatio = Config.Bind("Expand", "Amount of height expansion", defaultRatio,
                new ConfigDescription("How much to expand the lists by. Arbitrary units. 0 is roughly the same as default. 0.5 fills almost the entire screen under default UI scale. only use values above 0.5 if you change UI scale with another plugin. Needs a studio restart to apply.",
                    new AcceptableValueRange<float>(0, 1))).Value;

            resizeY = Mathf.Lerp(resizeYmin, resizeYmax, userRatio);


            // Sprite border values
            // extending it for new images is straightforward
            // dump image to disk, bring it into paint or unity
            // unity lets you see the result of adjusting borders with 9 slice
            // copy values back to code using (LEFT, BOTTOM, RIGHT, TOP) borders in vector4
            var ADD_CHARA = new Vector4(106, 14, 19, 29);
            var ADD_ITEM = new Vector4(0, 16, 0, 16);
            var ADD_BG = new Vector4(145, 13, 19, 29);
            var POSE = new Vector4(108, 70, 19, 108);

            var addMenu = mainMenu.Find("01_Add");

            if (Config.Bind("Expand", "Character lists", false,
                "Increase height of the add girl / guy character lists. Warning: Might interfere with some other plugins that modify the UI. " +
                "It will overlap with the FolderBrowser plugin until it's updated. Needs a studio restart to apply.").Value)
            {
                ResizeScrollRect(addMenu.Find("00_Female").Find("Scroll View"), ADD_CHARA);
                ResizeScrollRect(addMenu.Find("01_Male").Find("Scroll View"), ADD_CHARA);
            }

            if (Config.Bind("Expand", "Item and map lists", true, "Increase height of the add item / map / light / frame / etc. lists. Needs a studio restart to apply.").Value)
            {
                var addItem = addMenu.Find("02_Item");
                ResizeScrollRectStrict(addItem.Find("Scroll View Group"), ADD_ITEM, 20, -20);
                ResizeScrollRectStrict(addItem.Find("Scroll View Category"), ADD_ITEM, 20, -20);
                ResizeScrollRect(addItem.Find("Scroll View Item"), ADD_ITEM);

                var addMap = addMenu.Find("03_Map");
#if KKS
                //ResizeScrollRect(addMap.Find("Map Category"), ADD_ITEM);
                ResizeScrollRect(addMap.Find("Map"), ADD_ITEM);
#else
                ResizeScrollRect(addMap, ADD_ITEM);
#endif

                ResizeScrollRect(addMenu.Find("04_Light"), ADD_ITEM);
                ResizeScrollRect(addMenu.Find("05_Background"), ADD_BG);
                ResizeScrollRect(addMenu.Find("06_Frame"), ADD_BG);
#if KKS
                ResizeScrollRect(addMenu.Find("07_Text"), ADD_ITEM);
#endif
            }

            var manipulateMenu = mainMenu.Find("02_Manipulate");
            {
                var manipulateChara = manipulateMenu.Find("00_Chara");

                if (Config.Bind("Expand", "Chara State list", true, "Increase height of the anim/Curent State list. Needs a studio restart to apply.").Value)
                    ResizeScrollRect(manipulateChara.Find("01_State"), ADD_BG);

                if (Config.Bind("Expand", "Pose list", true,
                    "Increase height of the anim/Kinematics/Pose list. Warning: Might interfere with some other plugins that modify the UI. Needs a studio restart to apply.").Value)
                    ResizeScrollRectPose(manipulateChara.Find("02_Kinematic").Find("07_Pause"), POSE);

                if (Config.Bind("Expand", "Costume list", false,
                    "Increase height of the anim/Kinematics/Costume (coordinate / outfit) list. Warning: Might interfere with some other plugins that modify the UI. " +
                    "It will overlap with the FolderBrowser plugin until it's updated. Needs a studio restart to apply.").Value)
                    ResizeScrollRectStrict(manipulateChara.Find("05_Costume").Find("Scroll View"), ADD_CHARA, 20, -60);

                if (Config.Bind("Expand", "Animation lists", true, "Increase height of the anim/Animation lists.").Value)
                {
                    var manipulateAnime = manipulateMenu.Find("03_Anime");
                    ResizeScrollRectStrict(manipulateAnime.Find("Group Panel"), ADD_ITEM, 20, -20);
                    ResizeScrollRectStrict(manipulateAnime.Find("Category Panel"), ADD_ITEM, 20, -20);
                    ResizeScrollRectStrict(manipulateAnime.Find("Anime Panel"), ADD_ITEM, 20, -20);
                }
            }

            if (Config.Bind("Expand", "Image Board list", true, "Increase height of the Image Board list. Needs a studio restart to apply.").Value)
                ResizeScrollRect(mainMenu.Find("02_01_Panel"), ADD_BG);

            if (Config.Bind("Expand", "Sound lists", true, "Increase height of the sound/BGM lists. Needs a studio restart to apply.").Value)
            {
                var soundMenu = mainMenu.Find("03_Sound");

                ResizeScrollRectStrict(soundMenu.Find("00_BGM Player").Find("Scroll View"), ADD_ITEM, 20, 0);
                ResizeScrollRectStrict(soundMenu.Find("01_ENV Player").Find("Bottom Scroll View"), ADD_ITEM, 20, 0);
                ResizeScrollRectStrict(soundMenu.Find("02_Outside Player").Find("Bottom Scroll View"), ADD_ITEM, 20, 0);
            }

            if (Config.Bind("Expand", "Effect lists", true, "Increase height of the system / Option and Screen Effect lists. Needs a studio restart to apply.").Value)
            {
                var systemMenu = mainMenu.Find("04_System");

                ResizeScrollRect(systemMenu.Find("01_Screen Effect").Find("Screen Effect"), ADD_ITEM);
                ResizeScrollRect(systemMenu.Find("02_Option").Find("Option"), ADD_ITEM);
            }
        }

        private static void SliceTexture(Image image, Vector4 border)
        {
            var tr = image.sprite.textureRect;
            var newTexture = new Texture2D((int)tr.width, (int)tr.height, image.sprite.texture.format, false);
            Graphics.CopyTexture(image.sprite.texture, 0, 0, (int)tr.x, (int)tr.y, (int)tr.width, (int)tr.height, newTexture, 0, 0, 0, 0);
            //TODO: skip copy entirely, use source texture + offset.

            //Debug: dump background image to measure borders.
            //File.WriteAllBytes("e:\\image.png", );
            //DestroyImmediate(newTexture);

            var s = Sprite.Create(newTexture,
                new Rect(0, 0, newTexture.width, newTexture.height),
                new Vector2(0, newTexture.height),
                75f, //TODO: derive //todo use image.sprite.pixelsPerUnit? nope doesnt work wrong size
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

        // Resize recttransform to the new long long version while keeping existing offsets and such
        private static void ResizeScrollRect(Transform t, Vector4 slice)
        {
            var searchBox = AdjustSearchBox(t);
            if (searchBox) Console.WriteLine("hit " + t.name);

            var sr = t.GetComponent<ScrollRect>();
            var rt = sr.GetComponent<RectTransform>();
            var offsetMin = rt.offsetMin;
            Console.WriteLine(offsetMin.y);
            offsetMin.y = resizeY;
            rt.offsetMin = offsetMin;
            SliceTexture(sr.GetComponent<Image>(), slice);
            var vp = sr.viewport;
            vp.anchorMin = new Vector2(0, 0);
            vp.anchorMax = new Vector2(1, 1);
            var sb = sr.verticalScrollbar.GetComponent<RectTransform>();
            sb.offsetMin = new Vector2(sb.offsetMin.x, rt.offsetMin.y + (searchBox ? 40 : 20));
        }

        // ResizeScrollRect except it force resizes the recttransform to fill the parent with hardcoded y min/max offsets
        private static void ResizeScrollRectStrict(Transform t, Vector4 slice, int offsetMin, int offsetMax)
        {
            if (AdjustSearchBox(t))
                offsetMin += 20;

            var sr = t.GetComponent<ScrollRect>();
            var rt = sr.GetComponent<RectTransform>();

            var rtoffsetMin = rt.offsetMin;
            rtoffsetMin.y = resizeY;
            rt.offsetMin = rtoffsetMin;
            SliceTexture(sr.GetComponent<Image>(), slice);
            var vp = sr.viewport;
            vp.anchorMin = new Vector2(vp.anchorMin.x, 0);
            vp.anchorMax = new Vector2(vp.anchorMax.x, 1);
            vp.offsetMin = new Vector2(vp.offsetMin.x, offsetMin);
            vp.offsetMax = new Vector2(vp.offsetMax.x, offsetMax);
            var sb = sr.verticalScrollbar.GetComponent<RectTransform>();
            //sb.offsetMin = new Vector2(sb.offsetMin.x, rt.offsetMin.y + 20);
            sb.anchorMin = new Vector2(sb.anchorMin.x, 0);
            sb.anchorMax = new Vector2(sb.anchorMax.x, 1);
            sb.offsetMin = new Vector2(sb.offsetMin.x, offsetMin);
            sb.offsetMax = new Vector2(sb.offsetMax.x, offsetMax);
        }

        // Adjust black search boxes from 2155's search plugin. Doesn't affect KKUS boxes
        private static bool AdjustSearchBox(Transform t)
        {
            var searchBox = t.Cast<Transform>().FirstOrDefault(x => x && x.name.StartsWith("Search "));
            if (searchBox != null)
            {
                var sbrt = searchBox.GetComponent<RectTransform>();
                var sblp = sbrt.localPosition;
                sbrt.anchorMin = new Vector2(sbrt.anchorMin.x, 0);
                sbrt.anchorMax = new Vector2(sbrt.anchorMax.x, 0);
                sbrt.localPosition = sblp;
            }

            return searchBox;
        }

        // The pose window is special and needs special handling
        // bug: the image seems to not fit quite right still, probably caused by wrong sprite dpi
        private static void ResizeScrollRectPose(Transform t, Vector4 slice)
        {
            // Attache the delete button to the bottom so it moves as the window is resized
            var btnrt = t.Find("Button Delete").GetComponent<RectTransform>();
            var btnlp = btnrt.localPosition;
            btnrt.anchorMin = new Vector2(0, 0);
            btnrt.anchorMax = new Vector2(0, 0);
            btnrt.localPosition = btnlp;

            var sr = t.GetComponentInChildren<ScrollRect>();
            if (sr == null) throw new ArgumentNullException(nameof(sr));
            var rt = sr.GetComponent<RectTransform>();
            if (rt == null) throw new ArgumentNullException(nameof(rt));
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(rt.offsetMin.x, 40);
            rt.offsetMax = new Vector2(rt.offsetMax.x, -90);

            var img = t.GetComponent<Image>();
            if (img == null) throw new ArgumentNullException(nameof(img));
            var imgrt = img.GetComponent<RectTransform>();
            if (imgrt == null) throw new ArgumentNullException(nameof(imgrt));
            var offsetMin = imgrt.offsetMin;
            offsetMin.y = resizeY;
            imgrt.offsetMin = offsetMin;

            SliceTexture(img, slice);

            var vp = sr.viewport;
            vp.anchorMin = new Vector2(0, 0);
            vp.anchorMax = new Vector2(1, 1);
            vp.offsetMin = new Vector2(vp.offsetMin.x, 0);
            vp.offsetMax = new Vector2(vp.offsetMax.x, 0);
            var sb = sr.verticalScrollbar.GetComponent<RectTransform>();
            //sb.offsetMin = new Vector2(sb.offsetMin.x, rt.offsetMin.y + 20);
            sb.anchorMin = new Vector2(sb.anchorMin.x, 0);
            sb.anchorMax = new Vector2(sb.anchorMax.x, 1);
            sb.offsetMin = new Vector2(sb.offsetMin.x, 0);
            sb.offsetMax = new Vector2(sb.offsetMax.x, 0);
        }
    }
}
