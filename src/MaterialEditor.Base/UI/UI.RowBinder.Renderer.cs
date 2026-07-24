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
                    BindRenderer(item, listeners);
                    break;
                case RowModel.RowItemType.RendererEnabled:
                    BindToggle(
                        item,
                        listeners,
                        _controls.RendererEnabled,
                        () => item.RendererEnabled,
                        () => item.RendererEnabledOriginal,
                        value => item.RendererEnabled = value,
                        item.RendererEnabledOnChange,
                        item.RendererEnabledOnReset);
                    break;
                case RowModel.RowItemType.RendererShadowCastingMode:
                    BindShadowCastingMode(item, listeners);
                    break;
                case RowModel.RowItemType.RendererReceiveShadows:
                    BindToggle(
                        item,
                        listeners,
                        _controls.RendererReceiveShadows,
                        () => item.RendererReceiveShadows,
                        () => item.RendererReceiveShadowsOriginal,
                        value => item.RendererReceiveShadows = value,
                        item.RendererReceiveShadowsOnChange,
                        item.RendererReceiveShadowsOnReset);
                    break;
                case RowModel.RowItemType.RendererUpdateWhenOffscreen:
                    BindToggle(
                        item,
                        listeners,
                        _controls.RendererUpdateWhenOffscreen,
                        () => item.RendererUpdateWhenOffscreen,
                        () => item.RendererUpdateWhenOffscreenOriginal,
                        value => item.RendererUpdateWhenOffscreen = value,
                        item.RendererUpdateWhenOffscreenOnChange,
                        item.RendererUpdateWhenOffscreenOnReset);
                    break;
                case RowModel.RowItemType.RendererRecalculateNormals:
                    BindToggle(
                        item,
                        listeners,
                        _controls.RendererRecalculateNormals,
                        () => item.RendererRecalculateNormals,
                        () => item.RendererRecalculateNormalsOriginal,
                        value => item.RendererRecalculateNormals = value,
                        item.RendererRecalculateNormalsOnChange,
                        item.RendererRecalculateNormalsOnReset);
                    break;
            }
        }

        private void BindRenderer(RowModel item, ListenerScope listeners)
        {
            var controls = _controls.Renderer;
            controls.SetVisible(true);
            ChangedStateBinding.SetLabel(controls.Label, item.LabelText);
            controls.Name.text = item.RendererName;
            listeners.Listen(controls.ExportUvButton, () => item.ExportUVOnClick());
            listeners.Listen(controls.ExportObjButton, () => item.ExportObjOnClick());
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolableButtonRendererOnClick());
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.Renderer,
                () => item.RendererName);
        }

        private static void BindToggle(
            RowModel item,
            ListenerScope listeners,
            ToggleRowControls controls,
            System.Func<bool> getValue,
            System.Func<bool> getOriginal,
            System.Action<bool> setValue,
            System.Action<bool> changeValue,
            System.Action resetValue)
        {
            controls.SetVisible(true);
            ToggleBinding.Bind(
                listeners,
                controls,
                item,
                getValue,
                getOriginal,
                setValue,
                changeValue,
                resetValue);
        }

        private void BindShadowCastingMode(RowModel item, ListenerScope listeners)
        {
            var controls = _controls.RendererShadowCastingMode;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.RendererShadowCastingMode != item.RendererShadowCastingModeOriginal,
                    controls.ResetButton,
                    controls.Panel);

            controls.Dropdown.Set(item.RendererShadowCastingMode);
            refresh();
            listeners.Listen(controls.Dropdown, value =>
            {
                item.RendererShadowCastingMode = value;
                if (item.RendererShadowCastingMode == item.RendererShadowCastingModeOriginal)
                    item.RendererShadowCastingModeOnReset();
                else
                    item.RendererShadowCastingModeOnChange(value);
                refresh();
            });
            listeners.Listen(
                controls.ResetButton,
                () => controls.Dropdown.value = item.RendererShadowCastingModeOriginal);
        }
    }
}
