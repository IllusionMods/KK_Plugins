using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
using Studio;
using UnityEngine;
using UnityEngine.SceneManagement;
#if AI || HS2
using Cinemachine;
using HarmonyLib;
#endif

namespace KK_Plugins.StudioSceneSettings
{
    public class SceneController : SceneCustomFunctionController
    {
        public static SceneEffectsToggleSet MapMasking;
        public static SceneEffectsSliderSet NearClipPlane;
        public static SceneEffectsSliderSet FarClipPlane;

        internal void Start() => SceneManager.sceneLoaded += InitStudioUI;

        /// <summary>
        /// Save the modified values to the scene extended data
        /// </summary>
        protected override void OnSceneSave()
        {

            var data = new PluginData();
#if KK
            data.data[$"MapMasking"] = MapMasking.Value;
#endif

            if (NearClipPlane.Value == NearClipPlane.InitialValue)
                data.data[$"NearClipPlane"] = null;
            else
                data.data[$"NearClipPlane"] = NearClipPlane.Value;

            if (FarClipPlane.Value == FarClipPlane.InitialValue)
                data.data[$"FarClipPlane"] = null;
            else
                data.data[$"FarClipPlane"] = FarClipPlane.Value;

            SetExtendedData(data);
        }

        /// <summary>
        /// Read the extended save data and set the values of SliderSet or ToggleSet which will set the UI elements and trigger the setter method
        /// </summary>
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            if (operation == SceneOperationKind.Load)
            {
                var data = GetExtendedData();
                if (data?.data == null)
                    ResetAll();
                else
                {
#if KK
                    if (data.data.TryGetValue("MapMasking", out var mapMasking) && mapMasking != null)
                        MapMasking.Value = (bool)mapMasking;
                    else
                        MapMasking.Reset();
#endif

                    if (data.data.TryGetValue("NearClipPlane", out var nearClipPlane) && nearClipPlane != null && (float)nearClipPlane != 0f)
                        NearClipPlane.Value = (float)nearClipPlane;
                    else
                        NearClipPlane.Reset();

                    if (data.data.TryGetValue("FarClipPlane", out var farClipPlane) && farClipPlane != null && (float)farClipPlane != 0f)
                        FarClipPlane.Value = (float)farClipPlane;
                    else
                        FarClipPlane.Reset();
                }
            }
            else if (operation == SceneOperationKind.Clear)
                ResetAll();
            else //Do not import saved data, keep current settings
                return;
        }

        /// <summary>
        /// Reset all the things to their default values
        /// </summary>
        public void ResetAll()
        {
            MapMasking?.Reset();
            NearClipPlane.Reset();
            FarClipPlane.Reset();
        }

        /// <summary>
        /// Initialize the SliderSets and ToggleSets which create and manage the UI
        /// </summary>
        private void InitStudioUI(Scene s, LoadSceneMode lsm)
        {
            if (s.name != "Studio") return;
            SceneManager.sceneLoaded -= InitStudioUI;

            var CameraLayerDefault = Camera.main.gameObject.layer;

            var menu = new SceneEffectsCategory(StudioSceneSettings.PluginNameInternal);
#if KK
            MapMasking = menu.AddToggleSet("Map Masking", value => Camera.main.gameObject.layer = value ? StudioSceneSettings.CameraMapMaskingLayer : CameraLayerDefault, false);
#endif
            NearClipPlane = menu.AddSliderSet("Near Clip Plane", value => NearClipSetter(value), NearClipDefault, 0.01f, 10f);
            FarClipPlane = menu.AddSliderSet("Far Clip Plane", value => FarClipSetter(value), FarClipDefault, 1f, 10000f);

            NearClipPlane.EnforceSliderMaximum = false;
            FarClipPlane.EnforceSliderMaximum = false;
        }


#if KK
        internal float NearClipDefault => Camera.main.nearClipPlane;
        internal void NearClipSetter(float value) => Camera.main.nearClipPlane = value;
        internal float FarClipDefault => Camera.main.farClipPlane;
        internal void FarClipSetter(float value) => Camera.main.farClipPlane = value;
#else
        internal float NearClipDefault => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Field("lensSettings").GetValue<LensSettings>().NearClipPlane;

        internal void NearClipSetter(float value)
        {
            Studio.CameraControl cameraCtrl = Studio.Studio.Instance.cameraCtrl;
            var field = Traverse.Create(cameraCtrl).Field("lensSettings");
            var lensSettings = field.GetValue<LensSettings>();
            lensSettings.NearClipPlane = value;
            field.SetValue(lensSettings);
            cameraCtrl.fieldOfView = cameraCtrl.fieldOfView;
        }

        internal float FarClipDefault => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Field("lensSettings").GetValue<LensSettings>().FarClipPlane;

        internal void FarClipSetter(float value)
        {
            Studio.CameraControl cameraCtrl = Studio.Studio.Instance.cameraCtrl;
            var field = Traverse.Create(cameraCtrl).Field("lensSettings");
            var lensSettings = field.GetValue<LensSettings>();
            lensSettings.FarClipPlane = value;
            field.SetValue(lensSettings);
            cameraCtrl.fieldOfView = cameraCtrl.fieldOfView;
        }
#endif
    }
}