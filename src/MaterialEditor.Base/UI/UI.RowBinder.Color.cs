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
            var colorItem = (ColorPropertyRowModel)item;
            _controls.SetVisible(true);

            System.Action refreshInputs = () =>
            {
                _controls.RInput.SetValue(colorItem.Value.r);
                _controls.GInput.SetValue(colorItem.Value.g);
                _controls.BInput.SetValue(colorItem.Value.b);
                _controls.AInput.SetValue(colorItem.Value.a);
                _controls.EditButton.image.color = colorItem.Value;
            };
            System.Action refreshState = () =>
                ChangedStateBinding.Apply(
                    _controls.Label,
                    colorItem.LabelText,
                    colorItem.Value != colorItem.OriginalValue,
                    _controls.ResetButton,
                    _controls.Panel);
            System.Action applyValue = () =>
            {
                if (colorItem.Value == colorItem.OriginalValue)
                    colorItem.ValueOnReset();
                else
                    colorItem.ValueOnChange(colorItem.Value);
                _controls.EditButton.image.color = colorItem.Value;
                colorItem.SetToPalette(colorItem.LabelText, colorItem.Value);
                refreshState();
            };

            InputFieldBinding.BindFloat(
                listeners,
                _controls.RInput,
                () => colorItem.Value.r,
                value =>
                {
                    colorItem.Value =
                        new Color(value, colorItem.Value.g, colorItem.Value.b, colorItem.Value.a);
                    applyValue();
                });
            InputFieldBinding.BindFloat(
                listeners,
                _controls.GInput,
                () => colorItem.Value.g,
                value =>
                {
                    colorItem.Value =
                        new Color(colorItem.Value.r, value, colorItem.Value.b, colorItem.Value.a);
                    applyValue();
                });
            InputFieldBinding.BindFloat(
                listeners,
                _controls.BInput,
                () => colorItem.Value.b,
                value =>
                {
                    colorItem.Value =
                        new Color(colorItem.Value.r, colorItem.Value.g, value, colorItem.Value.a);
                    applyValue();
                });
            InputFieldBinding.BindFloat(
                listeners,
                _controls.AInput,
                () => colorItem.Value.a,
                value =>
                {
                    colorItem.Value =
                        new Color(colorItem.Value.r, colorItem.Value.g, colorItem.Value.b, value);
                    applyValue();
                });
            refreshInputs();
            refreshState();

            listeners.Listen(_controls.ResetButton, () =>
            {
                colorItem.Value = colorItem.OriginalValue;
                refreshInputs();
                colorItem.SetToPalette(colorItem.LabelText, colorItem.Value);
                colorItem.ValueOnReset();
                refreshState();
            });
            listeners.Listen(_controls.EditButton, () =>
            {
                colorItem.Edit(colorItem.LabelText, colorItem.Value, value =>
                {
                    colorItem.Value = value;
                    refreshInputs();
                    if (colorItem.Value == colorItem.OriginalValue)
                        colorItem.ValueOnReset();
                    else
                        colorItem.ValueOnChange(colorItem.Value);
                    refreshState();
                });
            });
            listeners.Listen(
                _controls.SelectInterpolableButton,
                () => colorItem.SelectInterpolable());
            LabelClickBinding.Bind(
                listeners,
                _controls.LabelClickTrigger,
                colorItem,
                MaterialEditorLabelType.ColorProperty,
                () => colorItem.PropertyName);
        }
    }
}
