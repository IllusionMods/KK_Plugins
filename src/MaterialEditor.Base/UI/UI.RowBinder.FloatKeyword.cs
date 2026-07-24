using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal sealed class FloatKeywordRowTypeBinder : IRowTypeBinder
    {
        private readonly RowControlSet _controls;

        internal FloatKeywordRowTypeBinder(RowControlSet controls)
        {
            _controls = controls;
        }

        public void Bind(RowModel item, ListenerScope listeners)
        {
            switch (item.ItemType)
            {
                case RowModel.RowItemType.FloatProperty:
                    BindFloat((FloatPropertyRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.KeywordProperty:
                    BindKeyword((KeywordPropertyRowModel)item, listeners);
                    break;
            }
        }

        private void BindFloat(FloatPropertyRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Float;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);
            System.Action<float> applyValue = value =>
            {
                item.Value = value;
                controls.Slider.Set(item.Value, false);
                controls.Input.SetValue(item.Value);
                if (item.Value == item.OriginalValue)
                    item.ValueOnReset();
                else
                    item.ValueOnChange(item.Value);
                refresh();
            };

            InputFieldBinding.BindFloat(
                listeners,
                controls.Input,
                () => item.Value,
                applyValue);
            SliderBinding.Bind(
                listeners,
                controls.Slider,
                item.SliderMinimum,
                item.SliderMaximum,
                item.Value,
                applyValue);
            refresh();

            listeners.Listen(controls.ResetButton, () =>
            {
                item.Value = item.OriginalValue;
                controls.Slider.Set(item.Value, false);
                controls.Input.SetValue(item.Value);
                item.ValueOnReset();
                refresh();
            });
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolable());
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.FloatProperty,
                () => item.PropertyName);
        }

        private void BindKeyword(KeywordPropertyRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Keyword;
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
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.KeywordProperty,
                () => item.PropertyName);
        }
    }
}
