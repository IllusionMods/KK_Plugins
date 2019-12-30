using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class StudioSceneSettings
    {
        private void InitStudioUI(string sceneName)
        {
            if (sceneName != "Studio") return;

            var CameraNearClipPlaneDefault = Camera.main.nearClipPlane;
            var CameraFarClipPlaneDefault = Camera.main.farClipPlane;
            var CameraLayerDefault = Camera.main.gameObject.layer;

            var header = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/Image Depth of Field");
            var headerClone = Instantiate(header);
            headerClone.name = $"Image {PluginNameInternal}";
            headerClone.transform.SetParent(header.transform.parent);
            headerClone.transform.localScale = new Vector3(1f, 1f, 1f);

            var label = headerClone.GetComponentInChildren<TextMeshProUGUI>();
            label.text = PluginNameInternal;

            var content = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/Depth of Field");
            var contentClone = Instantiate(content);
            contentClone.name = "KK_StudioSceneSettings";
            contentClone.transform.SetParent(content.transform.parent);
            contentClone.transform.localScale = new Vector3(1f, 1f, 1f);

            var maskLabel = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/TextMeshPro Draw").GetComponent<TextMeshProUGUI>();
            var maskToggle = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/Toggle Draw").GetComponent<Toggle>();
            MapMasking = new ToggleSet(maskLabel, maskToggle, "Map Masking", value => Camera.main.gameObject.layer = value ? CameraMapMaskingLayer : CameraLayerDefault, false);

            var nearClipLabel = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/TextMeshPro Focal Size").GetComponent<TextMeshProUGUI>();
            var nearClipSlider = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/Slider Focal Size").GetComponent<Slider>();
            var nearClipInput = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/InputField Focal Size").GetComponent<InputField>();
            var nearClipButton = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/Button Focal Size Default").GetComponent<Button>();
            NearClipPlane = new SliderSet(nearClipLabel, nearClipSlider, nearClipInput, nearClipButton, "Near Clip Plane", value => Camera.main.nearClipPlane = value, CameraNearClipPlaneDefault, 0.01f, 10f);
            NearClipPlane.EnforceSliderMaximum = false;

            var farClipLabel = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/TextMeshPro Aperture").GetComponent<TextMeshProUGUI>();
            var farClipSlider = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/Slider Aperture").GetComponent<Slider>();
            var farClipInput = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/InputField Aperture").GetComponent<InputField>();
            var farClipButton = GameObject.Find("StudioScene/Canvas Main Menu/04_System/01_Screen Effect/Screen Effect/Viewport/Content/KK_StudioSceneSettings/Button Aperture Default").GetComponent<Button>();
            FarClipPlane = new SliderSet(farClipLabel, farClipSlider, farClipInput, farClipButton, "Far Clip Plane", value => Camera.main.farClipPlane = value, CameraFarClipPlaneDefault, 0.1f, 10000f);
            FarClipPlane.EnforceSliderMaximum = false;
        }
    }
}