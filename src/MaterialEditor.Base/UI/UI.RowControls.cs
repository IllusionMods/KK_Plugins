using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal abstract class RowControls
    {
        protected RowControls(CanvasGroup panel)
        {
            Panel = panel;
        }

        internal CanvasGroup Panel { get; }

        internal void SetVisible(bool visible)
        {
            Panel.alpha = visible ? 1 : 0;
            Panel.blocksRaycasts = visible;
        }
    }

    internal sealed class ToggleRowControls : RowControls
    {
        internal ToggleRowControls(
            CanvasGroup panel,
            Text label,
            Toggle toggle,
            Button resetButton,
            LabelClickTrigger labelClickTrigger = null)
            : base(panel)
        {
            Label = label;
            Toggle = toggle;
            ResetButton = resetButton;
            LabelClickTrigger = labelClickTrigger;
        }

        internal Text Label { get; }
        internal Toggle Toggle { get; }
        internal Button ResetButton { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
    }

    internal sealed class RendererRowControls : RowControls
    {
        internal RendererRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("RendererPanel"))
        {
            Label = owner.GetUIComponent<Text>("RendererLabel");
            Name = owner.GetUIComponent<Text>("RendererText");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("RendererText");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableRendererButton");
            ExportUvButton = owner.GetUIComponent<Button>("ExportUVButton");
            ExportObjButton = owner.GetUIComponent<Button>("ExportObjButton");
        }

        internal Text Label { get; }
        internal Text Name { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Button ExportUvButton { get; }
        internal Button ExportObjButton { get; }
    }

    internal sealed class DropdownRowControls : RowControls
    {
        internal DropdownRowControls(
            CanvasGroup panel,
            Text label,
            Dropdown dropdown,
            Button resetButton)
            : base(panel)
        {
            Label = label;
            Dropdown = dropdown;
            ResetButton = resetButton;
        }

        internal Text Label { get; }
        internal Dropdown Dropdown { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class MaterialRowControls : RowControls
    {
        internal MaterialRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("MaterialPanel"))
        {
            CollapseButton = owner.GetUIComponent<Button>("MaterialCollapseButton");
            Label = owner.GetUIComponent<Text>("MaterialLabel");
            Name = owner.GetUIComponent<Text>("MaterialText");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("MaterialText");
            CopyButton = owner.GetUIComponent<Button>("MaterialCopy");
            PasteButton = owner.GetUIComponent<Button>("MaterialPaste");
            CopyRemoveButton = owner.GetUIComponent<Button>("MaterialCopyRemove");
            RenameButton = owner.GetUIComponent<Button>("MaterialRename");
        }

        internal Button CollapseButton { get; }
        internal Text Label { get; }
        internal Text Name { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button CopyButton { get; }
        internal Button PasteButton { get; }
        internal Button CopyRemoveButton { get; }
        internal Button RenameButton { get; }
    }

    internal sealed class ShaderRowControls : RowControls
    {
        internal ShaderRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("ShaderPanel"))
        {
            CollapseButton = owner.GetUIComponent<Button>("ShaderCollapseButton");
            CategoriesCollapseButton = owner.GetUIComponent<Button>("ShaderCategoriesCollapseButton");
            Label = owner.GetUIComponent<Text>("ShaderLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("ShaderLabel");
            Dropdown = owner.GetUIComponent<Dropdown>("ShaderDropdown");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableShaderButton");
            ResetButton = owner.GetUIComponent<Button>("ShaderResetButton");
        }

        internal Button CollapseButton { get; }
        internal Button CategoriesCollapseButton { get; }
        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Dropdown Dropdown { get; }
        internal Button SelectInterpolableButton { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class InputRowControls : RowControls
    {
        internal InputRowControls(
            CanvasGroup panel,
            Text label,
            LabelClickTrigger labelClickTrigger,
            InputField input,
            Button resetButton)
            : base(panel)
        {
            Label = label;
            LabelClickTrigger = labelClickTrigger;
            Input = input;
            ResetButton = resetButton;
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal InputField Input { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class PropertyCategoryRowControls : RowControls
    {
        internal PropertyCategoryRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("PropertyCategoryPanel"))
        {
            CollapseButton = owner.GetUIComponent<Button>("PropertyCategoryCollapseButton");
            Label = owner.GetUIComponent<Text>("PropertyCategoryLabel");
        }

        internal Button CollapseButton { get; }
        internal Text Label { get; }
    }

    internal sealed class TextureRowControls : RowControls
    {
        internal TextureRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("TexturePanel"))
        {
            Label = owner.GetUIComponent<Text>("TextureLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("TextureLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableTextureButton");
            ExportButton = owner.GetUIComponent<Button>("TextureExportButton");
            ImportButton = owner.GetUIComponent<Button>("TextureImportButton");
            ResetButton = owner.GetUIComponent<Button>("TextureResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Button ExportButton { get; }
        internal Button ImportButton { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class OffsetScaleRowControls : RowControls
    {
        internal OffsetScaleRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("OffsetScalePanel"))
        {
            Label = owner.GetUIComponent<Text>("OffsetScaleLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("OffsetScaleLabel");
            OffsetXLabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("OffsetXText");
            OffsetXInput = owner.GetUIComponent<NumericInputView>("OffsetXInput");
            OffsetYInput = owner.GetUIComponent<NumericInputView>("OffsetYInput");
            ScaleXInput = owner.GetUIComponent<NumericInputView>("ScaleXInput");
            ScaleYInput = owner.GetUIComponent<NumericInputView>("ScaleYInput");
            ResetButton = owner.GetUIComponent<Button>("OffsetScaleResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal LabelClickTrigger OffsetXLabelClickTrigger { get; }
        internal NumericInputView OffsetXInput { get; }
        internal NumericInputView OffsetYInput { get; }
        internal NumericInputView ScaleXInput { get; }
        internal NumericInputView ScaleYInput { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class ColorRowControls : RowControls
    {
        internal ColorRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("ColorPanel"))
        {
            Label = owner.GetUIComponent<Text>("ColorLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("ColorLabel");
            RInput = owner.GetUIComponent<NumericInputView>("ColorRInput");
            GInput = owner.GetUIComponent<NumericInputView>("ColorGInput");
            BInput = owner.GetUIComponent<NumericInputView>("ColorBInput");
            AInput = owner.GetUIComponent<NumericInputView>("ColorAInput");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableColorButton");
            ResetButton = owner.GetUIComponent<Button>("ColorResetButton");
            EditButton = owner.GetUIComponent<Button>("ColorEditButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal NumericInputView RInput { get; }
        internal NumericInputView GInput { get; }
        internal NumericInputView BInput { get; }
        internal NumericInputView AInput { get; }
        internal Button SelectInterpolableButton { get; }
        internal Button ResetButton { get; }
        internal Button EditButton { get; }
    }

    internal sealed class FloatRowControls : RowControls
    {
        internal FloatRowControls(RowBinder owner)
            : base(owner.GetUIComponent<CanvasGroup>("FloatPanel"))
        {
            Label = owner.GetUIComponent<Text>("FloatLabel");
            LabelClickTrigger = owner.GetUIComponent<LabelClickTrigger>("FloatLabel");
            SelectInterpolableButton = owner.GetUIComponent<Button>("SelectInterpolableFloatButton");
            Slider = owner.GetUIComponent<Slider>("FloatSlider");
            Input = owner.GetUIComponent<NumericInputView>("FloatInputField");
            ResetButton = owner.GetUIComponent<Button>("FloatResetButton");
        }

        internal Text Label { get; }
        internal LabelClickTrigger LabelClickTrigger { get; }
        internal Button SelectInterpolableButton { get; }
        internal Slider Slider { get; }
        internal NumericInputView Input { get; }
        internal Button ResetButton { get; }
    }

    internal sealed class RowControlSet
    {
        private readonly List<RowControls> _rows;

        private RowControlSet(RowBinder owner)
        {
            Renderer = new RendererRowControls(owner);
            RendererEnabled = CreateToggle(owner, "RendererEnabled");
            RendererShadowCastingMode = new DropdownRowControls(
                owner.GetUIComponent<CanvasGroup>("RendererShadowCastingModePanel"),
                owner.GetUIComponent<Text>("RendererShadowCastingModeLabel"),
                owner.GetUIComponent<Dropdown>("RendererShadowCastingModeDropdown"),
                owner.GetUIComponent<Button>("RendererShadowCastingModeResetButton"));
            RendererReceiveShadows = CreateToggle(owner, "RendererReceiveShadows");
            RendererUpdateWhenOffscreen = CreateToggle(owner, "RendererUpdateWhenOffscreen");
            RendererRecalculateNormals = CreateToggle(owner, "RendererRecalculateNormals");
            Material = new MaterialRowControls(owner);
            Shader = new ShaderRowControls(owner);
            ShaderRenderQueue = new InputRowControls(
                owner.GetUIComponent<CanvasGroup>("ShaderRenderQueuePanel"),
                owner.GetUIComponent<Text>("ShaderRenderQueueLabel"),
                owner.GetUIComponent<LabelClickTrigger>("ShaderRenderQueueLabel"),
                owner.GetUIComponent<InputField>("ShaderRenderQueueInput"),
                owner.GetUIComponent<Button>("ShaderRenderQueueResetButton"));
            PropertyCategory = new PropertyCategoryRowControls(owner);
            Texture = new TextureRowControls(owner);
            OffsetScale = new OffsetScaleRowControls(owner);
            Color = new ColorRowControls(owner);
            Float = new FloatRowControls(owner);
            Keyword = CreateToggle(owner, "Keyword", "KeywordLabel");

            _rows = new List<RowControls>
            {
                Renderer,
                RendererEnabled,
                RendererShadowCastingMode,
                RendererReceiveShadows,
                RendererUpdateWhenOffscreen,
                RendererRecalculateNormals,
                Material,
                Shader,
                ShaderRenderQueue,
                PropertyCategory,
                Texture,
                OffsetScale,
                Color,
                Float,
                Keyword
            };
        }

        internal RendererRowControls Renderer { get; }
        internal ToggleRowControls RendererEnabled { get; }
        internal DropdownRowControls RendererShadowCastingMode { get; }
        internal ToggleRowControls RendererReceiveShadows { get; }
        internal ToggleRowControls RendererUpdateWhenOffscreen { get; }
        internal ToggleRowControls RendererRecalculateNormals { get; }
        internal MaterialRowControls Material { get; }
        internal ShaderRowControls Shader { get; }
        internal InputRowControls ShaderRenderQueue { get; }
        internal PropertyCategoryRowControls PropertyCategory { get; }
        internal TextureRowControls Texture { get; }
        internal OffsetScaleRowControls OffsetScale { get; }
        internal ColorRowControls Color { get; }
        internal FloatRowControls Float { get; }
        internal ToggleRowControls Keyword { get; }

        internal static RowControlSet Create(RowBinder owner)
        {
            return new RowControlSet(owner);
        }

        internal void HideAll()
        {
            foreach (var row in _rows)
                row.SetVisible(false);
        }

        private static ToggleRowControls CreateToggle(
            RowBinder owner,
            string prefix,
            string labelClickObjectName = null)
        {
            return new ToggleRowControls(
                owner.GetUIComponent<CanvasGroup>($"{prefix}Panel"),
                owner.GetUIComponent<Text>($"{prefix}Label"),
                owner.GetUIComponent<Toggle>($"{prefix}Toggle"),
                owner.GetUIComponent<Button>($"{prefix}ResetButton"),
                labelClickObjectName == null
                    ? null
                    : owner.GetUIComponent<LabelClickTrigger>(labelClickObjectName));
        }
    }
}
