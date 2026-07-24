using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal sealed class ListenerScope : IDisposable
    {
        private readonly List<Action> _removeListeners = new List<Action>();

        internal void Listen(Button button, UnityAction listener)
        {
            button.onClick.AddListener(listener);
            _removeListeners.Add(() => button.onClick.RemoveListener(listener));
        }

        internal void Listen(Toggle toggle, UnityAction<bool> listener)
        {
            toggle.onValueChanged.AddListener(listener);
            _removeListeners.Add(() => toggle.onValueChanged.RemoveListener(listener));
        }

        internal void Listen(Dropdown dropdown, UnityAction<int> listener)
        {
            dropdown.onValueChanged.AddListener(listener);
            _removeListeners.Add(() => dropdown.onValueChanged.RemoveListener(listener));
        }

        internal void Listen(InputField input, UnityAction<string> listener)
        {
            input.onEndEdit.AddListener(listener);
            _removeListeners.Add(() => input.onEndEdit.RemoveListener(listener));
        }

        internal void Listen(Slider slider, UnityAction<float> listener)
        {
            slider.onValueChanged.AddListener(listener);
            _removeListeners.Add(() => slider.onValueChanged.RemoveListener(listener));
        }

        internal void OnDispose(Action removeListener)
        {
            _removeListeners.Add(removeListener);
        }

        public void Dispose()
        {
            for (var i = _removeListeners.Count - 1; i >= 0; i--)
                _removeListeners[i]();
            _removeListeners.Clear();
        }
    }

    internal static class ChangedStateBinding
    {
        internal static void Apply(
            Text label,
            string text,
            bool changed,
            Button resetButton,
            CanvasGroup panel)
        {
            label.text = text ?? string.Empty;
            panel.gameObject.GetComponent<Image>().color =
                changed ? MaterialEditorUI.ItemColorChanged : MaterialEditorUI.ItemColor;
            if (resetButton)
                resetButton.interactable = changed;
        }

        internal static void SetLabel(Text label, string text)
        {
            label.text = text ?? string.Empty;
        }
    }

    internal static class LabelClickBinding
    {
        internal static void Bind(
            ListenerScope listeners,
            LabelClickTrigger trigger,
            RowModel item,
            MaterialEditorLabelType labelType,
            Func<string> getName)
        {
            Action<UnityEngine.EventSystems.PointerEventData> handler = pointerEventData =>
            {
                var name = getName();
                MaterialEditorUI.RaiseLabelClicked(
                    new MaterialEditorLabelClickEventArgs(
                        labelType,
                        name,
                        item.GameObject,
                        item.Data,
                        item.Renderer,
                        item.Material,
                        item.Projector,
                        pointerEventData));
                MaterialEditorExtensionRegistry.RaiseLabelSelection(item, labelType, name);
            };
            trigger.Clicked = handler;
            listeners.OnDispose(() =>
            {
                if (trigger.Clicked == handler)
                    trigger.Clicked = null;
            });
        }
    }

    internal static class ToggleBinding
    {
        internal static void Bind(
            ListenerScope listeners,
            ToggleRowControls controls,
            RowModel item,
            Func<bool> getValue,
            Func<bool> getOriginal,
            Action<bool> setValue,
            Action<bool> changeValue,
            Action resetValue)
        {
            Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    getValue() != getOriginal(),
                    controls.ResetButton,
                    controls.Panel);

            controls.Toggle.Set(getValue(), false);
            refresh();

            listeners.Listen(controls.Toggle, value =>
            {
                setValue(value);
                if (getValue() == getOriginal())
                    resetValue();
                else
                    changeValue(getValue());
                refresh();
            });

            listeners.Listen(controls.ResetButton, () =>
            {
                setValue(getOriginal());
                controls.Toggle.Set(getValue(), false);
                resetValue();
                refresh();
            });
        }
    }

    internal static class InputFieldBinding
    {
        internal static void BindFloat(
            ListenerScope listeners,
            NumericInputView input,
            Func<float> getValue,
            Action<float> setValue)
        {
            input.SetValue(getValue());
            listeners.Listen(input.InputField, value =>
            {
                float parsed;
                if (!input.TryParse(value, out parsed))
                {
                    input.CommitValue(getValue());
                    return;
                }

                setValue(parsed);
                input.CommitValue(getValue());
            });
        }

        internal static void BindInt(
            ListenerScope listeners,
            InputField input,
            Func<int> getValue,
            Action<int> setValue)
        {
            input.Set(getValue().ToString(), false);
            listeners.Listen(input, value =>
            {
                int parsed;
                if (!int.TryParse(value, out parsed))
                {
                    input.Set(getValue().ToString(), false);
                    return;
                }

                setValue(parsed);
                input.Set(getValue().ToString(), false);
            });
        }
    }

    internal static class SliderBinding
    {
        internal static void Bind(
            ListenerScope listeners,
            Slider slider,
            float minimum,
            float maximum,
            float value,
            Action<float> changeValue)
        {
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.Set(value, false);
            listeners.Listen(slider, currentValue => changeValue(currentValue));
        }
    }

    internal interface IRowTypeBinder
    {
        void Bind(RowModel item, ListenerScope listeners);
    }

    internal sealed class RowHandlerRegistry
    {
        private readonly Dictionary<RowModel.RowItemType, IRowTypeBinder> _handlers =
            new Dictionary<RowModel.RowItemType, IRowTypeBinder>();

        internal void Register(IRowTypeBinder handler, params RowModel.RowItemType[] itemTypes)
        {
            foreach (var itemType in itemTypes)
                _handlers[itemType] = handler;
        }

        internal bool TryGet(RowModel.RowItemType itemType, out IRowTypeBinder handler)
        {
            return _handlers.TryGetValue(itemType, out handler);
        }
    }
}
