using UILib;
using UnityEngine;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class RendererRowViewFactory
    {
        internal static void CreateRows(Transform parent)
        {
            CreateRendererRow(parent);
            CreateBooleanRow(
                parent,
                "RendererEnabledPanel",
                "RendererEnabledLabel",
                "RendererEnabledToggle",
                "RendererEnabledResetButton",
                true,
                "Toggle the visibility of this renderer on/off",
                "Reset this property to its original value");
            CreateShadowCastingModeRow(parent);
            CreateBooleanRow(
                parent,
                "RendererReceiveShadowsPanel",
                "RendererReceiveShadowsLabel",
                "RendererReceiveShadowsToggle",
                "RendererReceiveShadowsResetButton",
                true,
                "Toggle if the renderer can have shadows cast on it on/off",
                "Reset this property to its original value");
            CreateBooleanRow(
                parent,
                "RendererUpdateWhenOffscreenPanel",
                "RendererUpdateWhenOffscreenLabel",
                "RendererUpdateWhenOffscreenToggle",
                "RendererUpdateWhenOffscreenResetButton",
                false,
                "When on, a renderer will always stay renderer, even when considered to be off-screen.\n\n This is handy for when the bounding box of an object is configured improperly and dissapears when it should still be visible",
                "Reset this property to its original value");
            CreateBooleanRow(
                parent,
                "RendererRecalculateNormalsPanel",
                "RendererRecalculateNormalsLabel",
                "RendererRecalculateNormalsToggle",
                "RendererRecalculateNormalsResetButton",
                false,
                "Recalculate the normals of this renderer based on its current shape, instead of its original shape.\n\nOnly available on skinned mesh renderers",
                "Reset this property to its original value.\n\nIn order for the reset to take effect you need to either save and re-load the scene, or copy the object and delete the old one");
        }

        private static void CreateRendererRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel("RendererPanel", parent, RendererColor);
            RowViewFactorySupport.CreateLabel(
                "RendererLabel",
                panel.transform,
                string.Empty,
                0f,
                0f);

            var rendererName = RowViewFactorySupport.CreateLabel(
                "RendererText",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);
            rendererName.gameObject.AddComponent<LabelClickTrigger>();
            TooltipManager.AddTooltip(rendererName.gameObject, "Renderer name");

            RowViewFactorySupport.CreateInterpolableButton(
                "SelectInterpolableRendererButton",
                panel.transform,
                "Select the properties (Enabled, Shadow casting mode and Receive shadows) of the currently selected renderer as interpolables in timeline");

            var exportUv = MaterialEditorControlFactory.CreateButton(
                "ExportUVButton",
                panel.transform,
                "Export UV Map");
            RowViewFactorySupport.SetWidth(exportUv, RendererButtonWidth);
            TooltipManager.AddTooltip(
                exportUv.gameObject,
                "Export the UV map of this renderer.\n\nThe UV map is the 2d projection of the renderer with which to map textures to the 3d model. You can use this UV map as a guide to drawing on textures");

            var exportObj = MaterialEditorControlFactory.CreateButton(
                "ExportObjButton",
                panel.transform,
                "Export .obj");
            RowViewFactorySupport.SetWidth(exportObj, RendererButtonWidth);
            TooltipManager.AddTooltip(
                exportObj.gameObject,
                "Export the renderer as a .obj.\n\nYou can use the <i>ExportBakedMesh</i> and <i>ExportBakedWorldPosition</i> config options to change the exporting behaviour");
        }

        private static void CreateBooleanRow(
            Transform parent,
            string panelName,
            string labelName,
            string toggleName,
            string resetName,
            bool defaultValue,
            string toggleTooltip,
            string resetTooltip)
        {
            var panel = RowViewFactorySupport.CreatePanel(panelName, parent, ItemColor);
            RowViewFactorySupport.CreateLabel(
                labelName,
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);

            var toggle = MaterialEditorControlFactory.CreateToggle(
                toggleName,
                panel.transform,
                string.Empty);
            toggle.isOn = defaultValue;
            RowViewFactorySupport.SetWidth(toggle, RendererToggleWidth);
            TooltipManager.AddTooltip(toggle.gameObject, toggleTooltip);

            var reset = MaterialEditorControlFactory.CreateButton(
                resetName,
                panel.transform,
                "Reset");
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(reset.gameObject, resetTooltip);
        }

        private static void CreateShadowCastingModeRow(Transform parent)
        {
            var panel = RowViewFactorySupport.CreatePanel(
                "RendererShadowCastingModePanel",
                parent,
                ItemColor);
            RowViewFactorySupport.CreateLabel(
                "RendererShadowCastingModeLabel",
                panel.transform,
                string.Empty,
                LabelWidth,
                1f);

            var dropdown = MaterialEditorControlFactory.CreateDropdown(
                "RendererShadowCastingModeDropdown",
                panel.transform);
            dropdown.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 100f);
            dropdown.captionText.transform.SetRect(0f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
            dropdown.captionText.alignment = TextAnchor.MiddleLeft;
            dropdown.options.Clear();
            dropdown.options.Add(new Dropdown.OptionData("Off"));
            dropdown.options.Add(new Dropdown.OptionData("On"));
            dropdown.options.Add(new Dropdown.OptionData("Two Sided"));
            dropdown.options.Add(new Dropdown.OptionData("Shadows Only"));
            dropdown.value = 0;
            dropdown.captionText.text = "Off";
            RowViewFactorySupport.SetWidth(dropdown, RendererDropdownWidth);
            TooltipManager.AddTooltip(
                dropdown.gameObject,
                @"- Off: Renderer casts no shadows
- On: Renderer casts shadows
- Two Sided: Always cast shadows from any direction, even for single sided objects
- Shadows Only: Renderer is invisible but still casts shadows");

            var reset = MaterialEditorControlFactory.CreateButton(
                "RendererShadowCastingModeResetButton",
                panel.transform,
                "Reset");
            RowViewFactorySupport.SetWidth(reset, ResetButtonWidth);
            TooltipManager.AddTooltip(
                reset.gameObject,
                "Reset this property to its original value");
        }
    }
}
