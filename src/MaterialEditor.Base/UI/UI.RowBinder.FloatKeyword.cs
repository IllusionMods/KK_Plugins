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
            System.Action<float> applyValue = value =>
            {
                item.FloatValue = value;
                controls.Slider.Set(item.FloatValue, false);
                controls.Input.SetValue(item.FloatValue);
                if (item.FloatValue == item.FloatValueOriginal)
                    item.FloatValueOnReset();
                else
                    item.FloatValueOnChange(item.FloatValue);
                refresh();
            };

            InputFieldBinding.BindFloat(
                listeners,
                controls.Input,
                () => item.FloatValue,
                applyValue);
            SliderBinding.Bind(
                listeners,
                controls.Slider,
                item.FloatValueSliderMin,
                item.FloatValueSliderMax,
                item.FloatValue,
                applyValue);
            refresh();

            listeners.Listen(controls.ResetButton, () =>
            {
                item.FloatValue = item.FloatValueOriginal;
                controls.Slider.Set(item.FloatValue, false);
                controls.Input.SetValue(item.FloatValue);
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
