using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class ColorRowTypeBinder : IRowTypeBinder
    {
        private readonly ColorRowControls _controls;

        internal ColorRowTypeBinder(RowControlSet controls)
        {
            _controls = controls.Color;
        }

        public void Bind(RowModel item, ListenerScope listeners)
        {
            _controls.SetVisible(true);

            System.Action refreshInputs = () =>
            {
                _controls.RInput.SetValue(item.ColorValue.r);
                _controls.GInput.SetValue(item.ColorValue.g);
                _controls.BInput.SetValue(item.ColorValue.b);
                _controls.AInput.SetValue(item.ColorValue.a);
                _controls.EditButton.image.color = item.ColorValue;
            };
            System.Action refreshState = () =>
                ChangedStateBinding.Apply(
                    _controls.Label,
                    item.LabelText,
                    item.ColorValue != item.ColorValueOriginal,
                    _controls.ResetButton,
                    _controls.Panel);
            System.Action applyValue = () =>
            {
                if (item.ColorValue == item.ColorValueOriginal)
                    item.ColorValueOnReset();
                else
                    item.ColorValueOnChange(item.ColorValue);
                _controls.EditButton.image.color = item.ColorValue;
                item.ColorValueSetToPalette(item.LabelText, item.ColorValue);
                refreshState();
            };

            InputFieldBinding.BindFloat(
                listeners,
                _controls.RInput,
                () => item.ColorValue.r,
                value =>
                {
                    item.ColorValue =
                        new Color(value, item.ColorValue.g, item.ColorValue.b, item.ColorValue.a);
                    applyValue();
                });
            InputFieldBinding.BindFloat(
                listeners,
                _controls.GInput,
                () => item.ColorValue.g,
                value =>
                {
                    item.ColorValue =
                        new Color(item.ColorValue.r, value, item.ColorValue.b, item.ColorValue.a);
                    applyValue();
                });
            InputFieldBinding.BindFloat(
                listeners,
                _controls.BInput,
                () => item.ColorValue.b,
                value =>
                {
                    item.ColorValue =
                        new Color(item.ColorValue.r, item.ColorValue.g, value, item.ColorValue.a);
                    applyValue();
                });
            InputFieldBinding.BindFloat(
                listeners,
                _controls.AInput,
                () => item.ColorValue.a,
                value =>
                {
                    item.ColorValue =
                        new Color(item.ColorValue.r, item.ColorValue.g, item.ColorValue.b, value);
                    applyValue();
                });
            refreshInputs();
            refreshState();

            listeners.Listen(_controls.ResetButton, () =>
            {
                item.ColorValue = item.ColorValueOriginal;
                refreshInputs();
                item.ColorValueSetToPalette(item.LabelText, item.ColorValue);
                item.ColorValueOnReset();
                refreshState();
            });
            listeners.Listen(_controls.EditButton, () =>
            {
                item.ColorValueOnEdit(item.LabelText, item.ColorValue, value =>
                {
                    item.ColorValue = value;
                    refreshInputs();
                    if (item.ColorValue == item.ColorValueOriginal)
                        item.ColorValueOnReset();
                    else
                        item.ColorValueOnChange(item.ColorValue);
                    refreshState();
                });
            });
            listeners.Listen(
                _controls.SelectInterpolableButton,
                () => item.SelectInterpolableButtonColorOnClick());
            LabelClickBinding.Bind(
                listeners,
                _controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.ColorProperty,
                () => item.PropertyName);
        }
    }
}
