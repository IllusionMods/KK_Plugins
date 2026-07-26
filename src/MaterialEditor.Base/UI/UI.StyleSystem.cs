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
        internal const float CategoryNavigatorWidth = 150f;

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
        Input,
        Tooltip
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

    internal static class MaterialEditorStyles
    {
        internal static readonly Color MainPanelColor = Color.white;
        internal static readonly Color HeaderColor = Color.gray;
        internal static readonly Color SidePanelColor = new Color(0.42f, 0.42f, 0.42f);
        internal static readonly Color NavigatorShaderHeaderColor = new Color(0.64f, 0.64f, 0.64f);
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

            var styleState = text.GetComponent<MaterialEditorTextStyleState>()
                             ?? text.gameObject.AddComponent<MaterialEditorTextStyleState>();
            styleState.SetRole(role);

            var visualCenter = text.GetComponent<RowTextVisualCenter>();
            if (visualCenter == null)
                visualCenter = text.gameObject.AddComponent<RowTextVisualCenter>();
            visualCenter.SetMode(
                role == MaterialEditorTextRole.Tooltip
                    ? TextVisualCenterMode.VisibleBounds
                    : TextVisualCenterMode.TypographicBody);
            visualCenter.enabled = true;

            text.SetVerticesDirty();
        }

        internal static void ApplyTypography(GameObject root)
        {
            if (root == null)
                return;

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                var styleState = text.GetComponent<MaterialEditorTextStyleState>();
                ApplyText(
                    text,
                    styleState != null
                        ? styleState.Role
                        : MaterialEditorTextRole.PreserveHorizontal);
            }
        }

        internal static void ApplyButton(Button button)
        {
            if (button == null)
                return;

            foreach (var text in button.GetComponentsInChildren<Text>(true))
                ApplyText(text, MaterialEditorTextRole.Button);
        }

        internal static void ApplyInputField(InputField inputField)
        {
            if (inputField == null)
                return;

            inputField.lineType = InputField.LineType.SingleLine;
            inputField.textComponent.resizeTextForBestFit = true;
            inputField.textComponent.resizeTextMinSize = 2;
            inputField.textComponent.resizeTextMaxSize = UIUtility.defaultFontSize;
            inputField.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            inputField.textComponent.verticalOverflow = VerticalWrapMode.Truncate;

            ApplyText(inputField.textComponent, MaterialEditorTextRole.Input);
            if (inputField.placeholder is Text placeholder)
            {
                placeholder.resizeTextForBestFit = true;
                placeholder.resizeTextMinSize = 2;
                placeholder.resizeTextMaxSize = UIUtility.defaultFontSize;
                ApplyText(placeholder, MaterialEditorTextRole.Input);
            }
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

    internal sealed class MaterialEditorTextStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorTextRole _role;

        internal MaterialEditorTextRole Role => _role;

        internal void SetRole(MaterialEditorTextRole role)
        {
            _role = role;
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
            string placeholder = "")
        {
            var inputField = UIUtility.CreateInputField(name, parent, placeholder);
            MaterialEditorStyles.ApplyInputField(inputField);
            return inputField;
        }

        internal static NumericInputView CreateNumericInput(
            string name,
            Transform parent,
            NumericInputSpec spec)
        {
            var inputField = CreateInputField(name, parent);
            var view = inputField.gameObject.AddComponent<NumericInputView>();
            view.Initialize(spec);
            return view;
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
