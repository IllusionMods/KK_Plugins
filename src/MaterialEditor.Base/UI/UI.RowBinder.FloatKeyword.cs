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
                    BindFloat(item, listeners);
                    break;
                case RowModel.RowItemType.KeywordProperty:
                    BindKeyword(item, listeners);
                    break;
            }
        }

        private void BindFloat(RowModel item, ListenerScope listeners)
        {
            var controls = _controls.Float;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.FloatValue != item.FloatValueOriginal,
                    controls.ResetButton,
                    controls.Panel);

            InputFieldBinding.BindFloat(
                listeners,
                controls.Input,
                () => item.FloatValue,
                value =>
                {
                    item.FloatValue = value;
                    controls.Slider.Set(item.FloatValue, false);
                    if (item.FloatValue == item.FloatValueOriginal)
                        item.FloatValueOnReset();
                    else
                        item.FloatValueOnChange(item.FloatValue);
                    refresh();
                });
            SliderBinding.Bind(
                listeners,
                controls.Slider,
                item.FloatValueSliderMin,
                item.FloatValueSliderMax,
                item.FloatValue,
                value =>
                {
                    controls.Input.Set(value.ToString(), false);
                    controls.Input.onEndEdit.Invoke(value.ToString());
                });
            refresh();

            listeners.Listen(controls.ResetButton, () =>
            {
                item.FloatValue = item.FloatValueOriginal;
                controls.Slider.Set(item.FloatValue, false);
                controls.Input.Set(item.FloatValue.ToString(), false);
                item.FloatValueOnReset();
                refresh();
            });
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolableButtonFloatOnClick());
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.FloatProperty,
                () => item.PropertyName);
        }

        private void BindKeyword(RowModel item, ListenerScope listeners)
        {
            var controls = _controls.Keyword;
            controls.SetVisible(true);
            ToggleBinding.Bind(
                listeners,
                controls,
                item,
                () => item.KeywordValue,
                () => item.KeywordValueOriginal,
                value => item.KeywordValue = value,
                item.KeywordValueOnChange,
                item.KeywordValueOnReset);
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.KeywordProperty,
                () => item.PropertyName);
        }
    }
}
