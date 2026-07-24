using UnityEngine;
using UnityEngine.UI;
using static UILib.Extensions;

namespace MaterialEditorAPI
{
    internal sealed class MaterialShaderRowTypeBinder : IRowTypeBinder
    {
        private readonly RowControlSet _controls;

        internal MaterialShaderRowTypeBinder(RowControlSet controls)
        {
            _controls = controls;
        }

        public void Bind(RowModel item, ListenerScope listeners)
        {
            switch (item.ItemType)
            {
                case RowModel.RowItemType.Material:
                    BindMaterial((MaterialRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.Shader:
                    BindShader((ShaderRowModel)item, listeners);
                    break;
                case RowModel.RowItemType.ShaderRenderQueue:
                    BindRenderQueue((ShaderRenderQueueRowModel)item, listeners);
                    break;
            }
        }

        private void BindMaterial(MaterialRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Material;
            controls.SetVisible(true);
            ChangedStateBinding.SetLabel(controls.Label, item.LabelText);
            controls.Name.text = item.MaterialName;
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.Material,
                () => item.MaterialName);

            listeners.Listen(controls.CopyButton, () => item.Copy.Invoke());
            listeners.Listen(controls.PasteButton, () => item.Paste.Invoke());

            var pasteText = controls.PasteButton.GetComponentInChildren<Text>();
            controls.PasteButton.enabled = !MaterialEditorPluginBase.CopyData.IsEmpty;
            pasteText.color = controls.PasteButton.enabled ? Color.black : Color.gray;

            controls.CopyRemoveButton.GetComponentInChildren<Text>().text =
                item.MaterialName.Contains(MaterialAPI.MaterialCopyPostfix)
                    ? "Remove Material"
                    : "Copy Material";
            controls.CopyRemoveButton.gameObject.SetActive(item.CopyOrRemove != null);
            if (item.CopyOrRemove != null)
                listeners.Listen(controls.CopyRemoveButton, () => item.CopyOrRemove.Invoke());

            controls.RenameButton.gameObject.SetActive(item.Rename != null);
            if (item.Rename != null)
                listeners.Listen(controls.RenameButton, () => item.Rename.Invoke());
        }

        private void BindShader(ShaderRowModel item, ListenerScope listeners)
        {
            var controls = _controls.Shader;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.ShaderName != item.OriginalShaderName,
                    controls.ResetButton,
                    controls.Panel);

            var selectedIndex = controls.Dropdown.OptionIndex(item.ShaderName);
            controls.Dropdown.Set(
                Mathf.Clamp(selectedIndex, 0, controls.Dropdown.options.Count - 1));
            controls.Dropdown.captionText.text = item.ShaderName;
            refresh();

            listeners.Listen(controls.Dropdown, value =>
            {
                var selected = controls.Dropdown.OptionText(value);
                if (value == 0 || selected.IsNullOrEmpty())
                    selected = item.OriginalShaderName;
                item.ShaderName = selected;

                if (item.ShaderName == item.OriginalShaderName)
                    item.ShaderNameOnReset();
                else
                    item.ShaderNameOnChange(item.ShaderName);
                MaterialEditorExtensionRegistry.RaiseRowSelection(
                    item,
                    MaterialEditorSelectionType.Shader,
                    MaterialEditorSelectionAction.Selected,
                    item.ShaderName);
                refresh();
            });
            listeners.Listen(
                controls.ResetButton,
                () => controls.Dropdown.value = controls.Dropdown.OptionIndex(item.OriginalShaderName));
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolable());

            AutoScrollToSelectionWithDropdown.Setup(controls.Dropdown);
            DropdownFilter.AddFilterUI(controls.Dropdown, "ShaderDropDown");
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.Shader,
                () => item.ShaderName);
        }

        private void BindRenderQueue(ShaderRenderQueueRowModel item, ListenerScope listeners)
        {
            var controls = _controls.ShaderRenderQueue;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.Value != item.OriginalValue,
                    controls.ResetButton,
                    controls.Panel);

            InputFieldBinding.BindInt(
                listeners,
                controls.Input,
                () => item.Value,
                value =>
                {
                    item.Value = value;
                    if (item.Value == item.OriginalValue)
                        item.ValueOnReset();
                    else
                        item.ValueOnChange(item.Value);
                    refresh();
                });
            refresh();

            listeners.Listen(controls.ResetButton, () =>
            {
                item.Value = item.OriginalValue;
                controls.Input.Set(item.Value.ToString(), false);
                item.ValueOnReset();
                refresh();
            });
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.ShaderRenderQueue,
                () => item.LabelText);
        }
    }
}
