using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal sealed class RendererRowTypeBinder : IRowTypeBinder
    {
        private readonly RowControlSet _controls;

        internal RendererRowTypeBinder(RowControlSet controls)
        {
            _controls = controls;
        }

        public void Bind(RowModel item, ListenerScope listeners)
        {
            switch (item.ItemType)
            {
                case RowModel.RowItemType.Renderer:
                    BindRenderer((RendererRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.RendererEnabled:
                    BindToggle(
                        (RendererEnabledRowModel)item,
                        listeners,
                        _controls.RendererEnabled);
                    break;
                case RowModel.RowItemType.RendererShadowCastingMode:
                    BindShadowCastingMode((RendererShadowCastingModeRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.RendererReceiveShadows:
                    BindToggle(
                        (RendererReceiveShadowsRowModel)item,
                        listeners,
                        _controls.RendererReceiveShadows);
                    break;
                case RowModel.RowItemType.RendererUpdateWhenOffscreen:
                    BindToggle(
                        (RendererUpdateWhenOffscreenRowModel)item,
                        listeners,
                        _controls.RendererUpdateWhenOffscreen);
                    break;
                case RowModel.RowItemType.RendererRecalculateNormals:
                    BindToggle(
                        (RendererRecalculateNormalsRowModel)item,
                        listeners,
                        _controls.RendererRecalculateNormals);
                    break;
            }
        }

        private void BindRenderer(RendererRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Renderer;
            controls.SetVisible(true);
            System.Action refreshCollapsedGlyph = () =>
                controls.CollapseButton.GetComponentInChildren<Text>().text =
                    item.Collapsed
                        ? FoldGlyphs.Collapsed
                        : FoldGlyphs.Expanded;
            refreshCollapsedGlyph();
            UnityEngine.Events.UnityAction toggleCollapsed = () =>
            {
                item.CollapsedOnChange(!item.Collapsed);
                refreshCollapsedGlyph();
            };
            listeners.Listen(controls.HeaderButton, toggleCollapsed);
            listeners.Listen(controls.CollapseButton, toggleCollapsed);
            controls.Name.text = item.RendererName;
            TooltipBinding.Bind(
                controls.Name.gameObject,
                item.TooltipText,
                item.RendererName,
                controls.Name);
            MaterialEditorStyles.SetControlAvailability(
                controls.ExportUvsButton,
                item.ExportUv != null,
                MaterialEditorControlAvailabilityMode.LegacyPassive);
            MaterialEditorStyles.SetControlAvailability(
                controls.ExportMeshButton,
                item.ExportObj != null,
                MaterialEditorControlAvailabilityMode.LegacyPassive);
            if (item.ExportUv != null)
                listeners.Listen(controls.ExportUvsButton, () => item.ExportUv());
            if (item.ExportObj != null)
                listeners.Listen(controls.ExportMeshButton, () => item.ExportObj());
            TimelineColumnBinding.Bind(
                controls.SelectInterpolableButton,
                listeners,
                item.SelectInterpolable);
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.Renderer,
                () => item.RendererName,
                pointerEventData =>
                {
                    if (pointerEventData.button
                        == UnityEngine.EventSystems.PointerEventData.InputButton.Left)
                        toggleCollapsed();
                });
        }

        private static void BindToggle(
            BooleanValueRowModel item,
            ListenerScope listeners,
            ToggleRowControls controls)
        {
            controls.SetVisible(true);
            ToggleBinding.Bind(
                listeners,
                controls,
                item,
                () => item.Value,
                () => item.OriginalValue,
                value => item.Value = value,
                item.ValueOnChange,
                item.ValueOnReset);
        }

        private void BindShadowCastingMode(
            RendererShadowCastingModeRowModel item,
            ListenerScope listeners)
        {
            var controls = _controls.RendererShadowCastingMode;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);

            controls.Dropdown.Set(item.Value);
            MaterialEditorDropdownCaptionFitter.Refresh(controls.Dropdown);
            refresh();
            listeners.Listen(controls.Dropdown, value =>
            {
                item.Value = value;
                if (item.Value == item.OriginalValue)
                    item.ValueOnReset();
                else
                    item.ValueOnChange(value);
                refresh();
            });
            listeners.Listen(
                controls.ResetButton,
                () => controls.Dropdown.value = item.OriginalValue);
        }
    }
}
