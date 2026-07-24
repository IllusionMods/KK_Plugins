using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorLayout
    {
        internal const float Margin = 5f;
        internal const float HeaderHeight = 20f;
        internal const float ScrollbarOffset = -15f;
        internal const float RowHeight = 22f;

        internal const float LabelWidth = 0f;
        internal const float ButtonWidth = 100f;
        internal const float SmallButtonWidth = 20f;
        internal const float ResetButtonWidth = SmallButtonWidth * 2f;
        internal const float InterpolableButtonWidth = SmallButtonWidth;
        internal const float ContentWidth = 316f;

        internal const float RendererButtonWidth = ButtonWidth;
        internal const float RendererToggleWidth = 20f;
        internal const float RendererDropdownWidth = 94f;
        internal const float MaterialButtonWidth = ButtonWidth * 0.75f;
        internal const float MaterialRenameButtonWidth = SmallButtonWidth;
        internal const float ShaderDropdownWidth = ContentWidth;
        internal const float RenderQueueInputWidth = 94f;
        internal const float OffsetScaleLabelXWidth = 48f;
        internal const float OffsetScaleLabelYWidth = 10f;
        internal const float OffsetScaleInputWidth = 50f;
        internal const float ColorLabelWidth = 10f;
        internal const float ColorInputWidth = 64f;
        internal const float ColorEditButtonWidth = 20f;
        internal const float FloatSliderWidth = ContentWidth - 94f;
        internal const float FloatInputWidth = 94f;
        internal const float KeywordToggleWidth = ContentWidth;

        internal static readonly RectOffset RowPadding = new RectOffset(1, 1, 1, 1);
    }

    internal enum MaterialEditorTextRole
    {
        PreserveHorizontal,
        Title,
        Label,
        CenteredLabel,
        Button,
        Input
    }

    internal enum MaterialEditorInputRole
    {
        Standard,
        ColorComponent
    }

    internal enum MaterialEditorPanelRole
    {
        Default,
        Main,
        Header,
        SidePanel,
        Row,
        RendererRow,
        MaterialRow,
        CategoryRow,
        TransparentRow
    }

    // InputField is itself an ILayoutElement and reports a preferred width based
    // on its current text. Use a higher-priority element so numeric content can
    // never resize fixed-width columns.
    internal sealed class FixedWidthLayoutOverride : MonoBehaviour, ILayoutElement
    {
        [SerializeField]
        private float _width;

        internal void SetWidth(float width)
        {
            _width = width;
            var rect = transform as RectTransform;
            if (rect != null)
                LayoutRebuilder.MarkLayoutForRebuild(rect);
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public float minWidth => _width;
        public float preferredWidth => _width;
        public float flexibleWidth => 0f;
        public float minHeight => -1f;
        public float preferredHeight => -1f;
        public float flexibleHeight => -1f;
        public int layoutPriority => 100;
    }

    internal static class MaterialEditorStyles
    {
        internal static readonly Color MainPanelColor = Color.white;
        internal static readonly Color HeaderColor = Color.gray;
        internal static readonly Color SidePanelColor = new Color(0.42f, 0.42f, 0.42f);
        internal static readonly Color RowColor = new Color(1f, 1f, 1f, 0.6f);
        internal static readonly Color RendererColor = new Color(0.984f, 0.600f, 0.008f, 0.5f);
        internal static readonly Color MaterialColor = new Color(0.400f, 0.690f, 0.196f, 0.5f);
        internal static readonly Color CategoryColor = new Color(0.627f, 0.004f, 0.812f, 0.5f);
        internal static readonly Color TransparentRowColor = new Color(1f, 1f, 1f, 0f);
        internal static readonly Color ChangedRowColor = new Color(0f, 0f, 0f, 0.3f);
        internal static readonly Color ScrollbarColor = new Color(1f, 1f, 1f, 0.6f);

        internal static void ApplyPanel(Image panel, MaterialEditorPanelRole role)
        {
            if (panel == null)
                return;

            switch (role)
            {
                case MaterialEditorPanelRole.Main:
                    panel.color = MainPanelColor;
                    break;
                case MaterialEditorPanelRole.Header:
                    panel.color = HeaderColor;
                    break;
                case MaterialEditorPanelRole.SidePanel:
                    panel.color = SidePanelColor;
                    break;
                case MaterialEditorPanelRole.Row:
                    panel.color = RowColor;
                    break;
                case MaterialEditorPanelRole.RendererRow:
                    panel.color = RendererColor;
                    break;
                case MaterialEditorPanelRole.MaterialRow:
                    panel.color = MaterialColor;
                    break;
                case MaterialEditorPanelRole.CategoryRow:
                    panel.color = CategoryColor;
                    break;
                case MaterialEditorPanelRole.TransparentRow:
                    panel.color = TransparentRowColor;
                    break;
            }
        }

        internal static void ApplyText(Text text, MaterialEditorTextRole role = MaterialEditorTextRole.PreserveHorizontal)
        {
            if (text == null)
                return;

            text.alignment = GetAlignment(text.alignment, role);
            text.fontSize = Mathf.Min(text.fontSize, UIUtility.defaultFontSize);
            if (text.resizeTextForBestFit)
                text.resizeTextMaxSize = Mathf.Min(text.resizeTextMaxSize, UIUtility.defaultFontSize);

            if (text.GetComponent<RowTextVisualCenter>() == null)
                text.gameObject.AddComponent<RowTextVisualCenter>();
            text.SetVerticesDirty();
        }

        internal static void ApplyTypography(GameObject root)
        {
            if (root == null)
                return;

            foreach (var text in root.GetComponentsInChildren<Text>(true))
                ApplyText(text);
        }

        internal static void ApplyButton(Button button)
        {
            if (button == null)
                return;

            foreach (var text in button.GetComponentsInChildren<Text>(true))
                ApplyText(text, MaterialEditorTextRole.Button);
        }

        internal static void ApplyInputField(
            InputField inputField,
            MaterialEditorInputRole role = MaterialEditorInputRole.Standard)
        {
            if (inputField == null)
                return;

            // Keep the complete value in InputField.text and let InputField move its
            // visible draw range with the caret instead of shrinking or overflowing.
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.textComponent.resizeTextForBestFit = false;
            inputField.textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            inputField.textComponent.verticalOverflow = VerticalWrapMode.Truncate;

            if (role == MaterialEditorInputRole.ColorComponent)
            {
                var textRect = inputField.textComponent.rectTransform;
                var leftInset = Mathf.Max(0f, textRect.offsetMin.x);
                var rightInset = Mathf.Max(0f, -textRect.offsetMax.x);
                ApplyFixedWidth(
                    inputField.gameObject,
                    MaterialEditorLayout.ColorInputWidth);
                ApplyInputViewport(inputField, leftInset, rightInset);
            }

            ApplyText(inputField.textComponent, MaterialEditorTextRole.Input);
            if (inputField.placeholder is Text placeholder)
                ApplyText(placeholder, MaterialEditorTextRole.Input);
        }

        private static void ApplyFixedWidth(GameObject control, float width)
        {
            var layout = control.GetComponent<FixedWidthLayoutOverride>()
                         ?? control.AddComponent<FixedWidthLayoutOverride>();
            layout.SetWidth(width);

            var rect = control.GetComponent<RectTransform>();
            if (rect != null)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private static void ApplyInputViewport(
            InputField inputField,
            float leftInset,
            float rightInset)
        {
            var textRect = inputField.textComponent.rectTransform;
            var bottomInset = textRect.offsetMin.y;
            var topInset = -textRect.offsetMax.y;

            var viewportObject = new GameObject(
                inputField.gameObject.name + "Viewport",
                typeof(RectTransform),
                typeof(RectMask2D));
            var viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.SetParent(inputField.transform, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(leftInset, bottomInset);
            viewportRect.offsetMax = new Vector2(-rightInset, -topInset);

            if (inputField.placeholder is Graphic placeholder)
            {
                var placeholderRect = placeholder.rectTransform;
                placeholderRect.SetParent(viewportRect, false);
                SetRectToFill(placeholderRect);
            }

            textRect.SetParent(viewportRect, false);
            SetRectToFill(textRect);
        }

        private static void SetRectToFill(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        internal static void ApplyToggle(Toggle toggle)
        {
            if (toggle == null)
                return;

            foreach (var text in toggle.GetComponentsInChildren<Text>(true))
                ApplyText(text);
        }

        internal static void ApplyDropdown(Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            ApplyText(dropdown.captionText, MaterialEditorTextRole.Input);
            if (dropdown.itemText != null)
                ApplyText(dropdown.itemText, MaterialEditorTextRole.Input);
            ApplyTypography(dropdown.gameObject);
        }

        internal static void ApplyScrollView(ScrollRect scrollRect)
        {
            if (scrollRect?.verticalScrollbar == null)
                return;

            var image = scrollRect.verticalScrollbar.GetComponent<Image>();
            if (image != null)
                image.color = ScrollbarColor;
        }

        internal static void ApplyRow(GameObject row)
        {
            if (row == null)
                return;

            foreach (var layout in row.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = true;

                var panelRect = layout.GetComponent<RectTransform>();
                if (panelRect == null)
                    continue;

                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                panelRect.localScale = Vector3.one;
            }

            ApplyTypography(row);
        }

        private static TextAnchor GetAlignment(TextAnchor current, MaterialEditorTextRole role)
        {
            switch (role)
            {
                case MaterialEditorTextRole.Title:
                case MaterialEditorTextRole.CenteredLabel:
                case MaterialEditorTextRole.Button:
                    return TextAnchor.MiddleCenter;
                case MaterialEditorTextRole.Label:
                case MaterialEditorTextRole.Input:
                    return TextAnchor.MiddleLeft;
                default:
                    return WithMiddleVerticalAlignment(current);
            }
        }

        private static TextAnchor WithMiddleVerticalAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    return TextAnchor.MiddleCenter;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return TextAnchor.MiddleRight;
                default:
                    return TextAnchor.MiddleLeft;
            }
        }
    }

    internal static class MaterialEditorControlFactory
    {
        internal static Canvas CreateNewUISystem(string name)
        {
            return UIUtility.CreateNewUISystem(name);
        }

        internal static Image CreatePanel(string name, Transform parent, MaterialEditorPanelRole role = MaterialEditorPanelRole.Default)
        {
            var panel = UIUtility.CreatePanel(name, parent);
            MaterialEditorStyles.ApplyPanel(panel, role);
            return panel;
        }

        internal static Text CreateText(string name, Transform parent, string value = "", MaterialEditorTextRole role = MaterialEditorTextRole.PreserveHorizontal)
        {
            var text = UIUtility.CreateText(name, parent, value);
            MaterialEditorStyles.ApplyText(text, role);
            return text;
        }

        internal static Button CreateButton(string name, Transform parent, string value)
        {
            var button = UIUtility.CreateButton(name, parent, value);
            MaterialEditorStyles.ApplyButton(button);
            return button;
        }

        internal static InputField CreateInputField(
            string name,
            Transform parent,
            string placeholder = "",
            MaterialEditorInputRole role = MaterialEditorInputRole.Standard)
        {
            var inputField = UIUtility.CreateInputField(name, parent, placeholder);
            MaterialEditorStyles.ApplyInputField(inputField, role);
            return inputField;
        }

        internal static Toggle CreateToggle(string name, Transform parent, string value)
        {
            var toggle = UIUtility.CreateToggle(name, parent, value);
            MaterialEditorStyles.ApplyToggle(toggle);
            return toggle;
        }

        internal static Dropdown CreateDropdown(string name, Transform parent)
        {
            var dropdown = UIUtility.CreateDropdown(name, parent);
            MaterialEditorStyles.ApplyDropdown(dropdown);
            return dropdown;
        }

        internal static Slider CreateSlider(string name, Transform parent)
        {
            return UIUtility.CreateSlider(name, parent);
        }

        internal static ScrollRect CreateScrollView(string name, Transform parent)
        {
            var scrollView = UIUtility.CreateScrollView(name, parent);
            MaterialEditorStyles.ApplyScrollView(scrollView);
            return scrollView;
        }
    }
}
