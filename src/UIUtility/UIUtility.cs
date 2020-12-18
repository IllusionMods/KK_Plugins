using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UILib
{
    internal static class UIUtility
    {
        public const RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;
        public const bool canvasPixelPerfect = false;

        public const CanvasScaler.ScaleMode canvasScalerUiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        public const float canvasScalerReferencePixelsPerUnit = 100f;

        public const bool graphicRaycasterIgnoreReversedGraphics = true;
        public const GraphicRaycaster.BlockingObjects graphicRaycasterBlockingObjects = GraphicRaycaster.BlockingObjects.None;

        public static Sprite checkMark;
        public static Sprite backgroundSprite;
        public static Sprite standardSprite;
        public static Sprite inputFieldBackground;
        public static Sprite knob;
        public static Sprite dropdownArrow;
        public static Sprite mask;
        public static readonly Color whiteColor = new Color(1.000f, 1.000f, 1.000f);
        public static readonly Color grayColor = new Color32(100, 99, 95, 255);
        public static readonly Color lightGrayColor = new Color32(150, 149, 143, 255);
        public static readonly Color greenColor = new Color32(0, 160, 0, 255);
        public static readonly Color lightGreenColor = new Color32(0, 200, 0, 255);
        public static readonly Color purpleColor = new Color(0.000f, 0.007f, 1.000f, 0.545f);
        public static readonly Color transparentGrayColor = new Color32(100, 99, 95, 90);
        public static Font defaultFont;
        public static int defaultFontSize;
        public static int scrollSensitivity = 32;
        public static DefaultControls.Resources resources;

        static UIUtility()
        {
            AssetBundle bundle = AssetBundle.LoadFromMemory(Resource.DefaultResources);

            var sprites = bundle.LoadAllAssets<Sprite>();
            for (var i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                switch (sprite.name)
                {
                    case "Background":
                        backgroundSprite = sprite;
                        break;
                    case "UISprite":
                        standardSprite = sprite;
                        break;
                    case "InputFieldBackground":
                        inputFieldBackground = sprite;
                        break;
                    case "Knob":
                        knob = sprite;
                        break;
                    case "Checkmark":
                        checkMark = sprite;
                        break;
                    case "DropdownArrow":
                        dropdownArrow = sprite;
                        break;
                    case "UIMask":
                        mask = sprite;
                        break;
                }
            }
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            resources = new DefaultControls.Resources { background = backgroundSprite, checkmark = checkMark, dropdown = dropdownArrow, inputField = inputFieldBackground, knob = knob, mask = mask, standard = standardSprite };
            defaultFontSize = 16;
            bundle.Unload(false);
        }

        public static Canvas CreateNewUISystem(string name = "NewUISystem")
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = canvasRenderMode;
            //c.pixelPerfect = canvasPixelPerfect;

            CanvasScaler cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = canvasScalerUiScaleMode;
            cs.referencePixelsPerUnit = canvasScalerReferencePixelsPerUnit;
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            GraphicRaycaster gr = go.GetComponent<GraphicRaycaster>();
            gr.ignoreReversedGraphics = graphicRaycasterIgnoreReversedGraphics;
            gr.blockingObjects = graphicRaycasterBlockingObjects;

            return c;
        }

        public static void SetCustomFont(string customFontName)
        {
            var fonts = Resources.FindObjectsOfTypeAll<Font>();
            for (var i = 0; i < fonts.Length; i++)
            {
                Font font = fonts[i];
                if (font.name.Equals(customFontName))
                    defaultFont = font;
            }
        }

        public static RectTransform CreateNewUIObject() => CreateNewUIObject(null, "UIObject");

        public static RectTransform CreateNewUIObject(string name) => CreateNewUIObject(null, name);

        public static RectTransform CreateNewUIObject(Transform parent) => CreateNewUIObject(parent, "UIObject");

        public static RectTransform CreateNewUIObject(Transform parent, string name)
        {
            RectTransform t = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent != null)
            {
                t.SetParent(parent, false);
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one;
            }
            return t;
        }

        public static InputField CreateInputField(string objectName = "New Input Field", Transform parent = null, string placeholder = "Placeholder...")
        {
            GameObject go = DefaultControls.CreateInputField(resources);
            go.name = objectName;
            var texts = go.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                text.font = defaultFont;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 2;
                text.resizeTextMaxSize = 100;
                text.alignment = TextAnchor.MiddleLeft;
                text.rectTransform.offsetMin = new Vector2(5f, 2f);
                text.rectTransform.offsetMax = new Vector2(-5f, -2f);
            }
            go.transform.SetParent(parent, false);

            InputField f = go.GetComponent<InputField>();
            f.placeholder.GetComponent<Text>().text = placeholder;

            return f;
        }

        public static Button CreateButton(string objectName = "New Button", Transform parent = null, string buttonText = "Button")
        {
            GameObject go = DefaultControls.CreateButton(resources);
            go.name = objectName;

            Text text = go.GetComponentInChildren<Text>(true);
            text.font = defaultFont;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 2;
            text.resizeTextMaxSize = 100;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            text.text = buttonText;
            go.transform.SetParent(parent, false);

            return go.GetComponent<Button>();
        }

        public static Image CreateImage(string objectName = "New Image", Transform parent = null, Sprite sprite = null)
        {
            GameObject go = DefaultControls.CreateImage(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            Image i = go.GetComponent<Image>();
            i.sprite = sprite;
            return i;
        }

        public static Text CreateText(string objectName = "New Text", Transform parent = null, string textText = "Text")
        {
            GameObject go = DefaultControls.CreateText(resources);
            go.name = objectName;

            Text text = go.GetComponentInChildren<Text>(true);
            text.font = defaultFont;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 2;
            text.resizeTextMaxSize = 100;
            text.alignment = TextAnchor.UpperLeft;
            text.text = textText;
            text.color = whiteColor;
            go.transform.SetParent(parent, false);

            return text;
        }

        public static Toggle CreateToggle(string objectName = "New Toggle", Transform parent = null, string label = "Label")
        {
            GameObject go = DefaultControls.CreateToggle(resources);
            go.name = objectName;

            Text text = go.GetComponentInChildren<Text>(true);
            text.font = defaultFont;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 2;
            text.resizeTextMaxSize = 100;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(23f, 1f), new Vector2(-5f, -2f));
            text.text = label;
            go.transform.SetParent(parent, false);

            return go.GetComponent<Toggle>();
        }

        public static Dropdown CreateDropdown(string objectName = "New Dropdown", Transform parent = null)
        {
            GameObject go = DefaultControls.CreateDropdown(resources);
            go.name = objectName;

            var texts = go.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                text.font = defaultFont;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 2;
                text.resizeTextMaxSize = 100;
                text.alignment = TextAnchor.MiddleLeft;
                if (text.name.Equals("Label"))
                    text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(10f, 6f), new Vector2(-25f, -7f));
                else
                    text.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(20f, 1f), new Vector2(-10f, -2f));
            }
            go.transform.SetParent(parent, false);

            var rects = go.GetComponentsInChildren<ScrollRect>(true);
            for (var i = 0; i < rects.Length; i++)
                rects[i].scrollSensitivity = scrollSensitivity;

            return go.GetComponent<Dropdown>();
        }

        public static RawImage CreateRawImage(string objectName = "New Raw Image", Transform parent = null, Texture texture = null)
        {
            GameObject go = DefaultControls.CreateRawImage(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            RawImage i = go.GetComponent<RawImage>();
            i.texture = texture;
            return i;
        }

        public static Scrollbar CreateScrollbar(string objectName = "New Scrollbar", Transform parent = null)
        {
            GameObject go = DefaultControls.CreateScrollbar(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            return go.GetComponent<Scrollbar>();
        }

        public static ScrollRect CreateScrollView(string objectName = "New ScrollView", Transform parent = null)
        {
            GameObject go = DefaultControls.CreateScrollView(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            go.GetComponent<ScrollRect>().scrollSensitivity = scrollSensitivity;
            return go.GetComponent<ScrollRect>();
        }

        public static Slider CreateSlider(string objectName = "New Slider", Transform parent = null)
        {
            GameObject go = DefaultControls.CreateSlider(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            return go.GetComponent<Slider>();
        }
        public static Image CreatePanel(string objectName = "New Panel", Transform parent = null)
        {
            GameObject go = DefaultControls.CreatePanel(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        public static Outline AddOutlineToObject(Transform t) => AddOutlineToObject(t, Color.black, new Vector2(1f, -1f));

        public static Outline AddOutlineToObject(Transform t, Color c) => AddOutlineToObject(t, c, new Vector2(1f, -1f));

        public static Outline AddOutlineToObject(Transform t, Vector2 effectDistance) => AddOutlineToObject(t, Color.black, effectDistance);

        public static Outline AddOutlineToObject(Transform t, Color color, Vector2 effectDistance)
        {
            Outline o = t.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = effectDistance;
            return o;
        }

        public static Toggle AddCheckboxToObject(Transform tr)
        {
            Toggle t = tr.gameObject.AddComponent<Toggle>();

            RectTransform bg = CreateNewUIObject(tr.transform, "Background");
            t.targetGraphic = AddImageToObject(bg, standardSprite);

            RectTransform check = CreateNewUIObject(bg, "CheckMark");
            Image checkM = AddImageToObject(check, checkMark);
            checkM.color = Color.black;
            t.graphic = checkM;

            bg.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            check.SetRect(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return t;
        }

        public static Image AddImageToObject(Transform t, Sprite sprite = null)
        {
            Image i = t.gameObject.AddComponent<Image>();
            i.type = Image.Type.Sliced;
            i.fillCenter = true;
            i.color = whiteColor;
            i.sprite = sprite == null ? backgroundSprite : sprite;
            return i;
        }

        public static MovableWindow MakeObjectDraggable(RectTransform clickableDragZone, RectTransform draggableObject)
        {
            MovableWindow mv = clickableDragZone.gameObject.AddComponent<MovableWindow>();
            mv.ToDrag = draggableObject;
            return mv;
        }
    }

    internal static class UIExtensions
    {
        public static void SetRect(this RectTransform self, Vector2 anchorMin) => SetRect(self, anchorMin, Vector2.one, Vector2.zero, Vector2.zero);
        public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax) => SetRect(self, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin) => SetRect(self, anchorMin, anchorMax, offsetMin, Vector2.zero);
        public static void SetRect(this RectTransform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            self.anchorMin = anchorMin;
            self.anchorMax = anchorMax;
            self.offsetMin = offsetMin;
            self.offsetMax = offsetMax;
        }

        public static void SetRect(this RectTransform self, RectTransform other)
        {
            self.anchorMin = other.anchorMin;
            self.anchorMax = other.anchorMax;
            self.offsetMin = other.offsetMin;
            self.offsetMax = other.offsetMax;
        }

        public static void SetRect(this RectTransform self, float anchorLeft = 0f, float anchorBottom = 0f, float anchorRight = 1f, float anchorTop = 1f, float offsetLeft = 0f, float offsetBottom = 0f, float offsetRight = 0f, float offsetTop = 0f)
        {
            self.anchorMin = new Vector2(anchorLeft, anchorBottom);
            self.anchorMax = new Vector2(anchorRight, anchorTop);
            self.offsetMin = new Vector2(offsetLeft, offsetBottom);
            self.offsetMax = new Vector2(offsetRight, offsetTop);
        }

        public static void SetRect(this Transform self, Transform other) => SetRect(self as RectTransform, other as RectTransform);

        public static void SetRect(this Transform self, Vector2 anchorMin) => SetRect(self as RectTransform, anchorMin, Vector2.one, Vector2.zero, Vector2.zero);
        public static void SetRect(this Transform self, Vector2 anchorMin, Vector2 anchorMax) => SetRect(self as RectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        public static void SetRect(this Transform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin) => SetRect(self as RectTransform, anchorMin, anchorMax, offsetMin, Vector2.zero);
        public static void SetRect(this Transform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rt = self as RectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void SetRect(this Transform self, float anchorLeft = 0f, float anchorBottom = 0f, float anchorRight = 1f, float anchorTop = 1f, float offsetLeft = 0f, float offsetBottom = 0f, float offsetRight = 0f, float offsetTop = 0f)
        {
            RectTransform rt = self as RectTransform;
            rt.anchorMin = new Vector2(anchorLeft, anchorBottom);
            rt.anchorMax = new Vector2(anchorRight, anchorTop);
            rt.offsetMin = new Vector2(offsetLeft, offsetBottom);
            rt.offsetMax = new Vector2(offsetRight, offsetTop);
        }

        public static Button LinkButtonTo(this Transform root, string path, UnityAction onClick)
        {
            Button b = root.Find(path).GetComponent<Button>();
            if (onClick != null)
                b.onClick.AddListener(onClick);
            return b;
        }

        public static Dropdown LinkDropdownTo(this Transform root, string path, UnityAction<int> onValueChanged)
        {
            Dropdown b = root.Find(path).GetComponent<Dropdown>();
            if (onValueChanged != null)
                b.onValueChanged.AddListener(onValueChanged);
            return b;

        }

        public static InputField LinkInputFieldTo(this Transform root, string path, UnityAction<string> onValueChanged, UnityAction<string> onEndEdit)
        {
            InputField b = root.Find(path).GetComponent<InputField>();
            if (onValueChanged != null)
                b.onValueChanged.AddListener(onValueChanged);
            if (onEndEdit != null)
                b.onEndEdit.AddListener(onEndEdit);
            return b;

        }

        public static ScrollRect LinkScrollViewTo(this Transform root, string path, UnityAction<Vector2> onValueChanged)
        {
            ScrollRect b = root.Find(path).GetComponent<ScrollRect>();
            if (onValueChanged != null)
                b.onValueChanged.AddListener(onValueChanged);
            return b;

        }

        public static Scrollbar LinkScrollbarTo(this Transform root, string path, UnityAction<float> onValueChanged)
        {
            Scrollbar b = root.Find(path).GetComponent<Scrollbar>();
            if (onValueChanged != null)
                b.onValueChanged.AddListener(onValueChanged);
            return b;

        }

        public static Slider LinkSliderTo(this Transform root, string path, UnityAction<float> onValueChanged)
        {
            Slider b = root.Find(path).GetComponent<Slider>();
            if (onValueChanged != null)
                b.onValueChanged.AddListener(onValueChanged);
            return b;

        }

        public static Toggle LinkToggleTo(this Transform root, string path, UnityAction<bool> onValueChanged)
        {
            Toggle b = root.Find(path).GetComponent<Toggle>();
            if (onValueChanged != null)
                b.onValueChanged.AddListener(onValueChanged);
            return b;

        }
    }
}
