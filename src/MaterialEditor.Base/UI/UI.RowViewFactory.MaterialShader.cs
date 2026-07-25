using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class MaterialShaderRowViewFactory
    {
        internal static void CreateRows(Transform parent)
        {
            CreateMaterialRow(parent);
            CreateShaderRow(parent);
            CreateRenderQueueRow(parent);
        }

        private static void CreateMaterialRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel("MaterialPanel", parent, MaterialColor);
            var collapse = MaterialEditorControlFactory.CreateButton(
                "MaterialCollapseButton",
                panel.transform,
                "-");
            RowViewFactorySupport.SetWidth(collapse, SmallButtonWidth);
            TooltipManager.AddTooltip(
                collapse.gameObject,
                "Expand or collapse this material section");
            RowViewFactorySupport.CreateLabel(
                "MaterialLabel",
                panel.transform,
                string.Empty,
                0f,
                0f);

            var materialName = RowViewFactorySupport.CreateLabel(
                "MaterialText",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            materialName.gameObject.AddComponent<LabelClickTrigger>();
            TooltipManager.AddTooltip(materialName.gameObject, "Material name");

            CreateMaterialButton(
                "MaterialCopy",
                panel.transform,
                "Copy Edits",
                MaterialButtonWidth,
                "Copy all the <b>edits</b> of this material");
            CreateMaterialButton(
                "MaterialPaste",
                panel.transform,
                "Paste Edits",
                MaterialButtonWidth,
                "Paste all the copied edits");
            CreateMaterialButton(
                "MaterialCopyRemove",
                panel.transform,
                "Copy Material",
                MaterialButtonWidth,
                "Make a copy of this material.\n\nUseful for overlaying different effects onto an object with different material shaders/properties");
            CreateMaterialButton(
                "MaterialRename",
                panel.transform,
                ">",
                MaterialRenameButtonWidth,
                "Rename material instances");
        }

        private static void CreateShaderRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel("ShaderPanel", parent, ItemColor);
            var collapse = MaterialEditorControlFactory.CreateButton(
                "ShaderCollapseButton",
                panel.transform,
                "-");
            RowViewFactorySupport.SetWidth(collapse, SmallButtonWidth);
            TooltipManager.AddTooltip(
                collapse.gameObject,
                "Expand or collapse this shader section");
            var label = RowViewFactorySupport.CreateLabel(
                "ShaderLabel",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            label.gameObject.AddComponent<LabelClickTrigger>();

            var categories = MaterialEditorControlFactory.CreateButton(
                "ShaderCategoriesCollapseButton",
                panel.transform,
                "--");
            RowViewFactorySupport.SetWidth(categories, SmallButtonWidth);
            TooltipManager.AddTooltip(
                categories.gameObject,
                "Expand or collapse all property categories");

            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableShaderButton",
                panel.transform,
                "Select the currently selected shader property and its render queue as interpolables in timeline");

            var dropdown = MaterialEditorControlFactory.CreateDropdown(
                "ShaderDropdown",
                panel.transform);
            dropdown.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
            dropdown.captionText.transform.SetRect(0f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
            dropdown.captionText.alignment = TextAnchor.MiddleLeft;
            dropdown.options.Clear();
            dropdown.options.Add(new Dropdown.OptionData("Reset"));
            foreach (var shader in MaterialEditorPluginBase.XMLShaderProperties)
                if (shader.Key != "default")
                    dropdown.options.Add(new Dropdown.OptionData(shader.Key));
            RowViewFactorySupport.SetWidth(dropdown, ShaderDropdownWidth);

            var reset = MaterialEditorControlFactory.CreateButton(
                "ShaderResetButton",
                panel.transform,
                "Reset");
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset this property to its original value.\n\nIf the original shader is not one known by Material Editor, it will not be able to reset the shader to its original value. In order for the reset to take effect you to either save and re-load the scene, or copy the object and delete the old one");
        }

        private static void CreateRenderQueueRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "ShaderRenderQueuePanel",
                parent,
                ItemColor);
            var label = MaterialEditorControlFactory.CreateText(
                "ShaderRenderQueueLabel",
                panel.transform,
                string.Empty);
            label.gameObject.AddComponent<LabelClickTrigger>();
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.black;

            var input = MaterialEditorControlFactory.CreateInputField(
                "ShaderRenderQueueInput",
                panel.transform);
            input.text = "0";
            TooltipManager.AddTooltip(
                input.gameObject,
                "The order in which a material is rendered. Higher render queues get rendered later");

            var reset = MaterialEditorControlFactory.CreateButton(
                "ShaderRenderQueueResetButton",
                panel.transform,
                "Reset");
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset this property to its original value");
        }

        private static void CreateMaterialButton(
            string name,
            Transform parent,
            string text,
            float width,
            string tooltip)
        {
            var button = MaterialEditorControlFactory.CreateButton(name, parent, text);
            RowViewFactorySupport.SetWidth(button, width);
            TooltipManager.AddTooltip(button.gameObject, tooltip);
        }
    }
}
