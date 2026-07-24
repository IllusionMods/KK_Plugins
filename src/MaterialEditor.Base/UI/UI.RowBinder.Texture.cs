using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal sealed class TextureRowTypeBinder : IRowTypeBinder
    {
        private readonly RowControlSet _controls;

        internal TextureRowTypeBinder(RowControlSet controls)
        {
            _controls = controls;
        }

        public void Bind(RowModel item, ListenerScope listeners)
        {
            switch (item.ItemType)
            {
                case RowModel.RowItemType.PropertyCategory:
                    BindCategory(item, listeners);
                    break;
                case RowModel.RowItemType.TextureProperty:
                    BindTexture(item, listeners);
                    break;
                case RowModel.RowItemType.TextureOffsetScale:
                    BindOffsetScale(item, listeners);
                    break;
            }
        }

        private void BindCategory(RowModel item, ListenerScope listeners)
        {
            var controls = _controls.PropertyCategory;
            controls.SetVisible(true);
            ChangedStateBinding.SetLabel(controls.Label, item.LabelText);
            controls.CollapseButton.GetComponentInChildren<Text>().text =
                item.CategoryCollapsed ? "+" : "-";
            listeners.Listen(controls.CollapseButton, () =>
            {
                item.CategoryCollapsed = !item.CategoryCollapsed;
                item.CategoryCollapsedOnChange?.Invoke(item.CategoryCollapsed);
            });
        }

        private void BindTexture(RowModel item, ListenerScope listeners)
        {
            var controls = _controls.Texture;
            controls.SetVisible(true);

            System.Action refreshState = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.TextureChanged,
                    controls.ResetButton,
                    controls.Panel);
            System.Action refreshExport = () =>
            {
                var text = controls.ExportButton.GetComponentInChildren<Text>();
                controls.ExportButton.enabled = item.TextureExists;
                text.text = item.TextureExists ? "Export Texture" : "No Texture";
                text.color = item.TextureExists ? Color.black : Color.gray;
            };

            refreshState();
            refreshExport();
            listeners.Listen(controls.ExportButton, () => item.TextureOnExport());
            listeners.Listen(controls.ImportButton, () =>
            {
                item.TextureChanged = true;
                item.TextureExists = true;
                item.TextureOnImport();
                refreshExport();
                refreshState();
            });
            listeners.Listen(controls.ResetButton, () =>
            {
                item.TextureChanged = false;
                item.TextureOnReset();
                refreshState();
            });
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolableButtonTextureOnClick());
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.TextureProperty,
                () => item.PropertyName);
        }

        private void BindOffsetScale(RowModel item, ListenerScope listeners)
        {
            var controls = _controls.OffsetScale;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Offset != item.OffsetOriginal || item.Scale != item.ScaleOriginal,
                    controls.ResetButton,
                    controls.Panel);
            System.Action applyOffset = () =>
            {
                if (item.Offset == item.OffsetOriginal)
                    item.OffsetOnReset();
                else
                    item.OffsetOnChange(item.Offset);
                refresh();
            };
            System.Action applyScale = () =>
            {
                if (item.Scale == item.ScaleOriginal)
                    item.ScaleOnReset();
                else
                    item.ScaleOnChange(item.Scale);
                refresh();
            };

            InputFieldBinding.BindFloat(
                listeners,
                controls.OffsetXInput,
                () => item.Offset.x,
                value =>
                {
                    item.Offset = new Vector2(value, item.Offset.y);
                    applyOffset();
                });
            InputFieldBinding.BindFloat(
                listeners,
                controls.OffsetYInput,
                () => item.Offset.y,
                value =>
                {
                    item.Offset = new Vector2(item.Offset.x, value);
                    applyOffset();
                });
            InputFieldBinding.BindFloat(
                listeners,
                controls.ScaleXInput,
                () => item.Scale.x,
                value =>
                {
                    item.Scale = new Vector2(value, item.Scale.y);
                    applyScale();
                });
            InputFieldBinding.BindFloat(
                listeners,
                controls.ScaleYInput,
                () => item.Scale.y,
                value =>
                {
                    item.Scale = new Vector2(item.Scale.x, value);
                    applyScale();
                });
            refresh();

            listeners.Listen(controls.ResetButton, () =>
            {
                item.Offset = item.OffsetOriginal;
                item.Scale = item.ScaleOriginal;
                controls.OffsetXInput.SetValue(item.Offset.x);
                controls.OffsetYInput.SetValue(item.Offset.y);
                controls.ScaleXInput.SetValue(item.Scale.x);
                controls.ScaleYInput.SetValue(item.Scale.y);
                item.OffsetOnReset();
                item.ScaleOnReset();
                refresh();
            });
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.TextureOffsetScale,
                () => item.PropertyName);
            LabelClickBinding.Bind(
                listeners,
                controls.OffsetXLabelClickTrigger,
                item,
                MaterialEditorLabelType.TextureOffsetScale,
                () => item.PropertyName);
        }
    }
}
