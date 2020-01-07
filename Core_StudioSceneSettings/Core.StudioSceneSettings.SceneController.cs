using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_Plugins.StudioSceneSettings
{
    public abstract class SceneControllerCore : SceneCustomFunctionController
    {
        public static ToggleSet MapMasking;
        public static SliderSet NearClipPlane;
        public static SliderSet FarClipPlane;

        internal void Start() => SceneManager.sceneLoaded += InitStudioUI;

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

                    if (data.data.TryGetValue("NearClipPlane", out var nearClipPlane) && nearClipPlane != null)
                        NearClipPlane.Value = (float)nearClipPlane;
                    else
                        NearClipPlane.Reset();

                    if (data.data.TryGetValue("FarClipPlane", out var farClipPlane) && farClipPlane != null)
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

        private void InitStudioUI(Scene s, LoadSceneMode lsm)
        {
            if (s.name != "Studio") return;
            SceneManager.sceneLoaded -= InitStudioUI;

            var CameraLayerDefault = Camera.main.gameObject.layer;

            var menu = new ScreenEffectMenu(StudioSceneSettingsPlugin.PluginNameInternal);
#if KK
            MapMasking = menu.AddToggleSet("Map Masking", value => Camera.main.gameObject.layer = value ? StudioSceneSettingsCore.CameraMapMaskingLayer : CameraLayerDefault, false);
#endif
            NearClipPlane = menu.AddSliderSet("Near Clip Plane", value => NearClipSetter(value), NearClipDefault, 0.01f, 10f);
            FarClipPlane = menu.AddSliderSet("Far Clip Plane", value => FarClipSetter(value), FarClipDefault, 1f, 10000f);

            NearClipPlane.EnforceSliderMaximum = false;
            FarClipPlane.EnforceSliderMaximum = false;
        }

        internal abstract float NearClipDefault { get; }
        internal abstract void NearClipSetter(float value);
        internal abstract float FarClipDefault { get; }
        internal abstract void FarClipSetter(float value);
    }
}