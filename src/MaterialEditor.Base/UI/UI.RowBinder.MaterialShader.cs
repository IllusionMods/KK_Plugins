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
                    BindMaterial(item, listeners);
                    break;
                case RowModel.RowItemType.Shader:
                    BindShader(item, listeners);
                    break;
                case RowModel.RowItemType.ShaderRenderQueue:
                    BindRenderQueue(item, listeners);
                    break;
            }
        }

        private void BindMaterial(RowModel item, ListenerScope listeners)
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

            listeners.Listen(controls.CopyButton, () => item.MaterialOnCopy.Invoke());
            listeners.Listen(controls.PasteButton, () => item.MaterialOnPaste.Invoke());

            var pasteText = controls.PasteButton.GetComponentInChildren<Text>();
            controls.PasteButton.enabled = !MaterialEditorPluginBase.CopyData.IsEmpty;
            pasteText.color = controls.PasteButton.enabled ? Color.black : Color.gray;

            controls.CopyRemoveButton.GetComponentInChildren<Text>().text =
                item.MaterialName.Contains(MaterialAPI.MaterialCopyPostfix)
                    ? "Remove Material"
                    : "Copy Material";
            controls.CopyRemoveButton.gameObject.SetActive(item.MaterialOnCopyRemove != null);
            if (item.MaterialOnCopyRemove != null)
                listeners.Listen(controls.CopyRemoveButton, () => item.MaterialOnCopyRemove.Invoke());

            controls.RenameButton.gameObject.SetActive(item.MaterialOnRename != null);
            if (item.MaterialOnRename != null)
                listeners.Listen(controls.RenameButton, () => item.MaterialOnRename.Invoke());
        }

        private void BindShader(RowModel item, ListenerScope listeners)
        {
            var controls = _controls.Shader;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.ShaderName != item.ShaderNameOriginal,
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
                    selected = item.ShaderNameOriginal;
                item.ShaderName = selected;

                if (item.ShaderName == item.ShaderNameOriginal)
                    item.ShaderNameOnReset();
                else
                    item.ShaderNameOnChange(item.ShaderName);
                refresh();
            });
            listeners.Listen(
                controls.ResetButton,
                () => controls.Dropdown.value = controls.Dropdown.OptionIndex(item.ShaderNameOriginal));
            listeners.Listen(
                controls.SelectInterpolableButton,
                () => item.SelectInterpolableButtonShaderOnClick());

            AutoScrollToSelectionWithDropdown.Setup(controls.Dropdown);
            DropdownFilter.AddFilterUI(controls.Dropdown, "ShaderDropDown");
            LabelClickBinding.Bind(
                listeners,
                controls.LabelClickTrigger,
                item,
                MaterialEditorLabelType.Shader,
                () => item.ShaderName);
        }

        private void BindRenderQueue(RowModel item, ListenerScope listeners)
        {
            var controls = _controls.ShaderRenderQueue;
            controls.SetVisible(true);

            System.Action refresh = () =>
                ChangedStateBinding.Apply(
                    controls.Label,
                    item.LabelText,
                    item.ShaderRenderQueue != item.ShaderRenderQueueOriginal,
                    controls.ResetButton,
                    controls.Panel);

            InputFieldBinding.BindInt(
                listeners,
                controls.Input,
                () => item.ShaderRenderQueue,
                value =>
                {
                    item.ShaderRenderQueue = value;
                    if (item.ShaderRenderQueue == item.ShaderRenderQueueOriginal)
                        item.ShaderRenderQueueOnReset();
                    else
                        item.ShaderRenderQueueOnChange(item.ShaderRenderQueue);
                    refresh();
                });
            refresh();

            listeners.Listen(controls.ResetButton, () =>
            {
                item.ShaderRenderQueue = item.ShaderRenderQueueOriginal;
                controls.Input.Set(item.ShaderRenderQueue.ToString(), false);
                item.ShaderRenderQueueOnReset();
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
