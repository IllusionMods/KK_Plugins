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
                    BindCategory((PropertyCategoryRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.TextureProperty:
                    BindTexture((TexturePropertyRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.TextureOffsetScale:
                    BindOffsetScale((TextureOffsetScaleRowModel)item, listeners);
                    break;
            }
        }

        private void BindCategory(PropertyCategoryRowModel item, ListenerScope listeners)
        {
            var controls = _controls.PropertyCategory;
            controls.SetVisible(true);
            ChangedStateBinding.SetLabel(controls.Label, item.LabelText);
            TooltipBinding.Bind(
                controls.Label.gameObject,
                item.TooltipText,
                "Category name");
            controls.CollapseButton.GetComponentInChildren<Text>().text =
                item.Collapsed ? FoldGlyphs.Collapsed : FoldGlyphs.Expanded;
            listeners.Listen(controls.CollapseButton, () =>
            {
                item.Collapsed = !item.Collapsed;
                item.CollapsedOnChange?.Invoke(item.Collapsed);
            });
        }

        private void BindTexture(TexturePropertyRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Texture;
            controls.SetVisible(true);
            TooltipBinding.Bind(controls.Label.gameObject, item.TooltipText);

            System.Action refreshState = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Changed,
                    controls.ResetButton,
                    controls.Panel);
            System.Action refreshExport = () =>
            {
                var text = controls.ExportButton.GetComponentInChildren<Text>();
                controls.ExportButton.enabled = item.Exists;
                text.text = item.Exists ? "Export Texture" : "No Texture";
                text.color = item.Exists ? Color.black : Color.gray;
            };

            refreshState();
            refreshExport();
            listeners.Listen(controls.ExportButton, () => item.Export());
            listeners.Listen(controls.ImportButton, () =>
            {
                item.Changed = true;
                item.Exists = true;
                item.Import();
                refreshExport();
                refreshState();
            });
            listeners.Listen(controls.ResetButton, () =>
            {
                item.Changed = false;
                item.Reset();
                refreshState();
            });
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolable());
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.TextureProperty,
                () => item.PropertyName);
        }

        private void BindOffsetScale(
            TextureOffsetScaleRowModel item,
            ListenerScope listeners)
        {
            var controls = _controls.OffsetScale;
            controls.SetVisible(true);
            TooltipBinding.Bind(controls.Label.gameObject, item.TooltipText);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Offset != item.OriginalOffset || item.Scale != item.OriginalScale,
                    controls.ResetButton,
                    controls.Panel);
            System.Action applyOffset = () =>
            {
                if (item.Offset == item.OriginalOffset)
                    item.OffsetOnReset();
                else
                    item.OffsetOnChange(item.Offset);
                refresh();
            };
            System.Action applyScale = () =>
            {
                if (item.Scale == item.OriginalScale)
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
                item.Offset = item.OriginalOffset;
                item.Scale = item.OriginalScale;
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
