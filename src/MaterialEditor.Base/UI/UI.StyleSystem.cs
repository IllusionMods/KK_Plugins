using System.Collections;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class MaterialEditorStyles
    {
        internal static Color WindowColor =>
            MaterialEditorPanelTextStyles.WindowColor;
        internal static Color LeftPanelColor =>
            MaterialEditorPanelTextStyles.LeftPanelColor;
        internal static Color CenterPanelColor =>
            MaterialEditorPanelTextStyles.CenterPanelColor;
        internal static Color RightPanelColor =>
            MaterialEditorPanelTextStyles.RightPanelColor;
        internal static Color MainPanelColor =>
            MaterialEditorPanelTextStyles.MainPanelColor;
        internal static Color HeaderColor =>
            MaterialEditorPanelTextStyles.HeaderColor;
        internal static Color SidePanelColor =>
            MaterialEditorPanelTextStyles.SidePanelColor;
        internal static Color NavigatorShaderHeaderColor =>
            MaterialEditorPanelTextStyles.NavigatorShaderHeaderColor;
        internal static Color RowColor =>
            MaterialEditorPanelTextStyles.RowColor;
        internal static Color RendererColor =>
            MaterialEditorPanelTextStyles.RendererColor;
        internal static Color MaterialColor =>
            MaterialEditorPanelTextStyles.MaterialColor;
        internal static Color ShaderColor =>
            MaterialEditorPanelTextStyles.ShaderColor;
        internal static Color CategoryColor =>
            MaterialEditorPanelTextStyles.CategoryColor;
        internal static Color SubcategoryColor =>
            MaterialEditorPanelTextStyles.SubcategoryColor;
        internal static Color PropertyColor =>
            MaterialEditorPanelTextStyles.PropertyColor;
        internal static Color AlternatePropertyColor =>
            MaterialEditorPanelTextStyles.AlternatePropertyColor;
        internal static Color TransparentRowColor =>
            MaterialEditorPanelTextStyles.TransparentRowColor;
        internal static Color ChangedRowColor =>
            MaterialEditorPanelTextStyles.ChangedRowColor;
        internal static Color ScrollbarColor =>
            MaterialEditorPanelTextStyles.ScrollbarColor;
        internal static Color ShaderHintUnderlineColor =>
            MaterialEditorPanelTextStyles.ShaderHintUnderlineColor;

        internal static void ApplyPanel(
            Image panel,
            MaterialEditorPanelRole role)
        {
            MaterialEditorPanelTextStyles.ApplyPanel(panel, role);
        }

        internal static void ApplyText(
            Text text,
            MaterialEditorTextRole role =
                MaterialEditorTextRole.PreserveHorizontal)
        {
            MaterialEditorPanelTextStyles.ApplyText(text, role);
        }

        internal static void ApplyTypography(GameObject root)
        {
            MaterialEditorPanelTextStyles.ApplyTypography(root);
        }

        internal static void ApplyButton(Button button)
        {
            MaterialEditorSelectionStyles.ApplyButton(button);
        }

        internal static void SetControlAvailability(
            Button button,
            bool available,
            MaterialEditorControlAvailabilityMode mode =
                MaterialEditorControlAvailabilityMode.Disabled)
        {
            if (button == null)
                return;

            var state = button.GetComponent<MaterialEditorControlStyleState>()
                        ?? MaterialEditorControlStyleState.Assign(
                            button,
                            MaterialEditorControlStyleRole.Button);
            state.SetAvailability(available, mode);

            if (mode == MaterialEditorControlAvailabilityMode.Hidden)
            {
                if (button.gameObject.activeSelf != available)
                    button.gameObject.SetActive(available);
                if (!available)
                    return;
            }

            ReapplyControlState(state);
        }

        internal static void ReapplyControlState(
            MaterialEditorControlStyleState state)
        {
            if (state == null || !state.BeginApply())
                return;

            try
            {
                MaterialEditorSelectionStyles.ReapplyTheme(state);
                MaterialEditorInputStyles.ReapplyTheme(state);
                MaterialEditorDropdownStyles.ReapplyTheme(state);
                ApplyControlAvailability(state);
            }
            finally
            {
                state.EndApply();
            }
        }

        private static void ApplyControlAvailability(
            MaterialEditorControlStyleState state)
        {
            var selectable = state.GetComponent<Selectable>();
            if (selectable == null)
                return;

            // Swatch color is data, not theme state. Its binder remains the
            // sole owner of the target graphic tint.
            if (state.Role == MaterialEditorControlStyleRole.Swatch)
                return;

            if (state.AvailabilityMode
                    == MaterialEditorControlAvailabilityMode.Hidden
                && !state.Available)
            {
                if (selectable.gameObject.activeSelf)
                    selectable.gameObject.SetActive(false);
                return;
            }

            selectable.enabled = true;
            if (state.AvailabilityMode
                    == MaterialEditorControlAvailabilityMode.LegacyPassive
                && !state.Available
                && MaterialEditorTheme.Mode == MaterialEditorThemeMode.Legacy)
            {
                selectable.interactable = true;
                MaterialEditorScrollSelectableStyles.SynchronizeCurrentState(
                    selectable);
                ApplyUnavailableText(selectable, Color.gray);
                // Unavailable Light controls retain their normal white surface
                // and gray text, while the disabled Behaviour rejects pointer input.
                selectable.enabled = false;
                return;
            }

            selectable.interactable = state.Available;
            MaterialEditorScrollSelectableStyles.SynchronizeCurrentState(
                selectable);
            if (!state.Available
                && MaterialEditorTheme.Mode == MaterialEditorThemeMode.Dark)
            {
                ApplyUnavailableText(
                    selectable,
                    MaterialEditorTheme.Colors.DisabledText);
            }
        }

        private static void ApplyUnavailableText(
            Selectable selectable,
            Color color)
        {
            foreach (var text in selectable.GetComponentsInChildren<Text>(true))
            {
                text.color = color;
                MaterialEditorPanelTextStyles.RefreshTextRendering(text);
            }
        }
        internal static void ApplyPropertyCategoryButton(Button button)
        {
            MaterialEditorSelectionStyles.ApplyPropertyCategoryButton(button);
        }

        internal static void SetPropertyCategoryExpanded(
            Button button,
            bool expanded)
        {
            MaterialEditorSelectionStyles.SetPropertyCategoryExpanded(
                button,
                expanded);
        }

        internal static void ApplyPropertySubcategoryButton(Button button)
        {
            MaterialEditorSelectionStyles.ApplyPropertySubcategoryButton(
                button);
        }

        internal static void SetPropertySubcategoryExpanded(
            Button button,
            bool expanded)
        {
            MaterialEditorSelectionStyles.SetPropertySubcategoryExpanded(
                button,
                expanded);
        }

        internal static void ApplyCategoryNavigationButton(Button button)
        {
            MaterialEditorSelectionStyles.ApplyCategoryNavigationButton(
                button);
        }

        internal static void ApplySelectionListRowButton(Button button)
        {
            MaterialEditorSelectionStyles.ApplySelectionListRowButton(button);
        }

        internal static void SetCategoryNavigationSelected(
            Button button,
            bool selected)
        {
            MaterialEditorSelectionStyles.SetCategoryNavigationSelected(
                button,
                selected);
        }

        internal static void ApplySwatchButton(Button button)
        {
            MaterialEditorSelectionStyles.ApplySwatchButton(button);
        }

        internal static void ApplyInputField(InputField inputField)
        {
            MaterialEditorInputStyles.ApplyInputField(inputField);
        }

        internal static void ApplyToggle(Toggle toggle)
        {
            MaterialEditorInputStyles.ApplyToggle(toggle);
        }

        internal static void ApplyDropdown(Dropdown dropdown)
        {
            MaterialEditorDropdownStyles.ApplyDropdown(dropdown);
        }

        internal static void ApplyDropdownPopup(
            Dropdown dropdown,
            Transform popupRoot,
            InputField filter,
            Button clearButton)
        {
            MaterialEditorDropdownStyles.ApplyDropdownPopup(
                dropdown,
                popupRoot,
                filter,
                clearButton);
        }

        internal static void ApplyDropdownItemState(
            Toggle toggle,
            Text text,
            bool selected)
        {
            MaterialEditorDropdownStyles.ApplyDropdownItemState(
                toggle,
                text,
                selected);
        }

        internal static void ApplyScrollView(ScrollRect scrollRect)
        {
            MaterialEditorScrollSelectableStyles.ApplyScrollView(scrollRect);
        }

        internal static void ApplySlider(Slider slider)
        {
            MaterialEditorInputStyles.ApplySlider(slider);
        }

        internal static void ApplyRow(GameObject row)
        {
            MaterialEditorSelectionStyles.ApplyRow(row);
        }

        internal static void ApplyGraphicColor(
            Graphic graphic,
            MaterialEditorThemeColorRole role)
        {
            if (graphic == null)
                return;

            MaterialEditorGraphicStyleState.Assign(graphic, role);
            graphic.color = MaterialEditorTheme.Colors.Resolve(role);
            graphic.SetVerticesDirty();
        }

        internal static void ApplyOutline(
            Graphic graphic,
            MaterialEditorThemeColorRole role)
        {
            MaterialEditorScrollSelectableStyles.ApplyControlOutline(
                graphic,
                role);
        }

        internal static void ReapplyTheme(GameObject root)
        {
            if (root == null)
                return;

            foreach (var panelState in
                     root.GetComponentsInChildren<MaterialEditorPanelStyleState>(true))
            {
                MaterialEditorPanelTextStyles.ApplyPanel(
                    panelState.GetComponent<Image>(),
                    panelState.Role);
            }

            foreach (var textState in
                     root.GetComponentsInChildren<MaterialEditorTextStyleState>(true))
            {
                MaterialEditorPanelTextStyles.ApplyText(
                    textState.GetComponent<Text>(),
                    textState.Role);
            }

            foreach (var controlState in
                     root.GetComponentsInChildren<MaterialEditorControlStyleState>(true))
                ReapplyControlState(controlState);
            foreach (var scrollState in
                     root.GetComponentsInChildren<MaterialEditorScrollStyleState>(true))
                MaterialEditorScrollSelectableStyles.ReapplyTheme(scrollState);

            foreach (var outlineState in
                     root.GetComponentsInChildren<MaterialEditorOutlineStyleState>(true))
                MaterialEditorScrollSelectableStyles.ReapplyOutline(
                    outlineState);

            foreach (var graphicState in
                     root.GetComponentsInChildren<MaterialEditorGraphicStyleState>(true))
            {
                var graphic = graphicState.GetComponent<Graphic>();
                if (graphic == null)
                    continue;
                graphic.canvasRenderer.SetColor(Color.white);
                graphic.canvasRenderer.SetAlpha(
                    MaterialEditorTheme.States.VisibleAlpha);
                graphic.color =
                    MaterialEditorTheme.Colors.Resolve(graphicState.Role);
                graphic.SetVerticesDirty();
            }

            foreach (var popupStyle in
                     root.GetComponentsInChildren<MaterialEditorDropdownPopupStyle>(true))
                popupStyle.ReapplyTheme();

            foreach (var itemStyle in
                     root.GetComponentsInChildren<MaterialEditorDropdownItemStyle>(true))
                itemStyle.ReapplyTheme();

            foreach (var underline in
                     root.GetComponentsInChildren<ShaderHintUnderline>(true))
            {
                underline.color =
                    MaterialEditorTheme.Colors.ShaderHintUnderline;
                underline.SetVerticesDirty();
            }

            // Selectable reapplication can start ColorTint work and dropdown
            // templates can activate while the semantic pass is running. Text
            // is therefore synchronized last, after all owners have assigned
            // their final role colors.
            RefreshTextRendering(root, true);
        }

        internal static void RefreshTextRendering(
            GameObject root,
            bool includeInactive)
        {
            if (root == null)
                return;

            foreach (var text in
                     root.GetComponentsInChildren<Text>(includeInactive))
            {
                MaterialEditorPanelTextStyles.RefreshTextRendering(text);
            }
        }
    }

    internal enum MaterialEditorTextRole
    {
        PreserveHorizontal,
        Title,
        Chrome,
        SecondaryChrome,
        Label,
        CenteredLabel,
        Button,
        Input,
        Placeholder,
        Tooltip
    }

    internal enum MaterialEditorPanelRole
    {
        Default,
        Window,
        Main,
        CenterPanel,
        Header,
        SidePanel,
        LeftPanel,
        RightPanel,
        Row,
        PropertyRow,
        AlternatePropertyRow,
        RendererRow,
        MaterialRow,
        ShaderRow,
        CategoryRow,
        SubcategoryRow,
        SelectedRow,
        HoverRow,
        DisabledRow,
        ModifiedRow,
        TransparentRow,
        RowStencilMask,
        StencilMask,
        RowBackdrop
    }

    internal sealed class MaterialEditorTextStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorTextRole _role;
        [SerializeField] private bool _assigned;

        internal MaterialEditorTextRole Role => _role;
        internal bool Assigned => _assigned;

        internal void SetRole(MaterialEditorTextRole role)
        {
            _role = role;
            _assigned = true;
        }

        private void OnEnable()
        {
            var text = GetComponent<Text>();
            if (!_assigned)
            {
                MaterialEditorPanelTextStyles.RefreshTextRendering(text);
                return;
            }

            MaterialEditorPanelTextStyles.ApplyText(text, _role);
            var owner = GetComponentInParent<MaterialEditorControlStyleState>();
            if (owner != null)
                MaterialEditorStyles.ReapplyControlState(owner);
        }
    }

    internal sealed class MaterialEditorPanelStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorPanelRole _role;

        internal MaterialEditorPanelRole Role => _role;

        internal void SetRole(MaterialEditorPanelRole role)
        {
            _role = role;
        }
    }

    internal enum MaterialEditorControlStyleRole
    {
        Button,
        PropertyCategory,
        PropertySubcategory,
        CategoryNavigation,
        SelectionListRow,
        Swatch,
        InputField,
        Toggle,
        Dropdown,
        Slider
    }

    internal enum MaterialEditorControlAvailabilityMode
    {
        Disabled,
        LegacyPassive,
        Hidden,
        TimelineSlot
    }

    internal sealed class MaterialEditorControlStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorControlStyleRole _role;
        [SerializeField] private bool _logicalState;
        [SerializeField] private bool _available = true;
        [SerializeField] private MaterialEditorControlAvailabilityMode _availabilityMode;
        [SerializeField] private bool _assigned;
        private bool _applying;

        internal MaterialEditorControlStyleRole Role => _role;
        internal bool LogicalState => _logicalState;
        internal bool Available => _available;
        internal MaterialEditorControlAvailabilityMode AvailabilityMode =>
            _availabilityMode;

        internal static MaterialEditorControlStyleState Assign(
            Selectable selectable,
            MaterialEditorControlStyleRole role)
        {
            if (selectable == null)
                return null;

            var state = selectable.GetComponent<MaterialEditorControlStyleState>();
            if (state == null)
                state = selectable.gameObject.AddComponent<MaterialEditorControlStyleState>();
            if (!state._assigned)
            {
                state._available = true;
                state._availabilityMode =
                    MaterialEditorControlAvailabilityMode.Disabled;
            }
            state._role = role;
            state._assigned = true;
            return state;
        }

        internal void SetLogicalState(bool value)
        {
            if (_logicalState == value)
                return;
            _logicalState = value;
            if (_assigned && !_applying)
                MaterialEditorStyles.ReapplyControlState(this);
        }

        internal void SetAvailability(
            bool available,
            MaterialEditorControlAvailabilityMode mode)
        {
            _available = available;
            _availabilityMode = mode;
        }

        internal bool BeginApply()
        {
            if (_applying)
                return false;
            _applying = true;
            return true;
        }

        internal void EndApply()
        {
            _applying = false;
        }

        private void OnEnable()
        {
            // AddComponent invokes OnEnable before Assign can set the role.
            // Existing pooled controls, however, already own complete semantic
            // state and must restore it synchronously when reactivated.
            if (_assigned && !_applying)
                MaterialEditorStyles.ReapplyControlState(this);
        }
    }
    internal sealed class MaterialEditorScrollStyleState : MonoBehaviour
    {
        [SerializeField] private bool _popup;

        internal bool Popup => _popup;
        internal bool SideList { get; set; }

        internal static MaterialEditorScrollStyleState Assign(
            ScrollRect scrollRect,
            bool popup)
        {
            if (scrollRect == null)
                return null;

            var state = scrollRect.GetComponent<MaterialEditorScrollStyleState>()
                        ?? scrollRect.gameObject.AddComponent<MaterialEditorScrollStyleState>();
            state._popup = popup;
            return state;
        }
    }

    internal sealed class MaterialEditorGraphicStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorThemeColorRole _role;

        internal MaterialEditorThemeColorRole Role => _role;

        internal static MaterialEditorGraphicStyleState Assign(
            Graphic graphic,
            MaterialEditorThemeColorRole role)
        {
            if (graphic == null)
                return null;
            var state = graphic.GetComponent<MaterialEditorGraphicStyleState>()
                        ?? graphic.gameObject.AddComponent<MaterialEditorGraphicStyleState>();
            state._role = role;
            return state;
        }
    }

    internal sealed class MaterialEditorOutlineStyleState : MonoBehaviour
    {
        [SerializeField] private MaterialEditorThemeColorRole _role;

        internal MaterialEditorThemeColorRole Role => _role;

        internal static MaterialEditorOutlineStyleState Assign(
            Graphic graphic,
            MaterialEditorThemeColorRole role)
        {
            if (graphic == null)
                return null;
            var state = graphic.GetComponent<MaterialEditorOutlineStyleState>()
                        ?? graphic.gameObject.AddComponent<MaterialEditorOutlineStyleState>();
            state._role = role;
            return state;
        }
    }

    // Theme application assigns semantic colors immediately. This component
    // owns only the render-cache synchronization needed by Unity UI after an
    // inactive/pooled hierarchy is enabled or a Selectable transition settles.
    // Each request replaces the previous two-frame pass, so repeated theme
    // toggles cannot accumulate coroutines or stale work.
    internal sealed class MaterialEditorThemeRepaintCoordinator : MonoBehaviour
    {
        private Coroutine _routine;
        private int _generation;
        private bool _pending;

        internal void RequestRepaint()
        {
            _pending = true;
            _generation++;
            SynchronizeActiveText();

            if (isActiveAndEnabled)
                Restart(_generation);
        }

        private void OnEnable()
        {
            if (_pending)
                Restart(_generation);
        }

        private void OnDisable()
        {
            _generation++;
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = null;
        }

        private void Restart(int generation)
        {
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(RepaintAfterActivation(generation));
        }

        private IEnumerator RepaintAfterActivation(int generation)
        {
            // One frame lets parent activation and virtual-row binding finish.
            yield return null;
            if (!CanContinue(generation))
                yield break;
            SynchronizeActiveText();

            // A second frame catches dropdown clones and the final ColorTint
            // tick without rebuilding presentation data or the row layout.
            yield return null;
            if (!CanContinue(generation))
                yield break;
            SynchronizeActiveText();

            _pending = false;
            _routine = null;
        }

        private bool CanContinue(int generation)
        {
            return generation == _generation && isActiveAndEnabled;
        }

        private void SynchronizeActiveText()
        {
            MaterialEditorStyles.RefreshTextRendering(gameObject, false);
        }
    }
}
