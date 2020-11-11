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
        public static SceneEffectsSliderSet ShadowDistance;
        private int CameraLayerDefault;

#if AI || HS2
        private GameObject studioCameraColliderControllerGO;
#endif

        private void Start() => SceneManager.sceneLoaded += InitStudioUI;

        /// <summary>
        /// Save the modified values to the scene extended data
        /// </summary>
        protected override void OnSceneSave()
        {

            var data = new PluginData();
            data.data["MapMasking"] = MapMasking.Value;

            if (NearClipPlane.Value == NearClipPlane.InitialValue)
                data.data["NearClipPlane"] = null;
            else
                data.data["NearClipPlane"] = NearClipPlane.Value;

            if (FarClipPlane.Value == FarClipPlane.InitialValue)
                data.data["FarClipPlane"] = null;
            else
                data.data["FarClipPlane"] = FarClipPlane.Value;

            if (ShadowDistance.Value == ShadowDistance.InitialValue)
                data.data["ShadowDistance"] = null;
            else
                data.data["ShadowDistance"] = ShadowDistance.Value;

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
                    if (data.data.TryGetValue("MapMasking", out var mapMasking) && mapMasking != null)
                        MapMasking.Value = (bool)mapMasking;
                    else
                        MapMasking.Reset();

                    if (data.data.TryGetValue("NearClipPlane", out var nearClipPlane) && nearClipPlane != null && (float)nearClipPlane != 0f)
                        NearClipPlane.Value = (float)nearClipPlane;
                    else
                        NearClipPlane.Reset();

                    if (data.data.TryGetValue("FarClipPlane", out var farClipPlane) && farClipPlane != null && (float)farClipPlane != 0f)
                        FarClipPlane.Value = (float)farClipPlane;
                    else
                        FarClipPlane.Reset();

                    if (data.data.TryGetValue("ShadowDistance", out var shadowDistance) && shadowDistance != null && (float)shadowDistance != 0f)
                        ShadowDistance.Value = (float)shadowDistance;
                    else
                        ShadowDistance.Reset();
                }
            }
            else if (operation == SceneOperationKind.Clear)
                ResetAll();
            //Do not import saved data, keep current settings
        }

        /// <summary>
        /// Reset all the things to their default values
        /// </summary>
        public void ResetAll()
        {
            MapMasking?.Reset();
            NearClipPlane.Reset();
            FarClipPlane.Reset();
            ShadowDistance.Reset();
        }

        /// <summary>
        /// Initialize the SliderSets and ToggleSets which create and manage the UI
        /// </summary>
        private void InitStudioUI(Scene s, LoadSceneMode lsm)
        {
            if (s.name != "Studio") return;
            SceneManager.sceneLoaded -= InitStudioUI;

#if KK
            CameraLayerDefault = Camera.main.gameObject.layer;
#else 
            CameraLayerDefault = 1;
#endif

#if AI || HS2
            //Add a custom collider controller to the camera
            var mainCamera = GameObject.Find("StudioScene/Camera/CameraSet/MainCamera");

            studioCameraColliderControllerGO = new GameObject();
            studioCameraColliderControllerGO.name = "StudioCameraColliderController";
            studioCameraColliderControllerGO.AddComponent<StudioCameraColliderController>();
            var rigidbody = studioCameraColliderControllerGO.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            var collider = studioCameraColliderControllerGO.GetOrAddComponent<CapsuleCollider>();
            collider.radius = 0.05f;
            collider.isTrigger = true;
            collider.direction = 2;

            var cameraControllerGO = GameObject.Find("StudioScene/Camera/CameraSet/CameraController");
            var cameraController = cameraControllerGO.GetComponent<Studio.CameraControl>();
            Traverse.Create(cameraController).Field("viewCollider").SetValue(collider);
            studioCameraColliderControllerGO.transform.SetParent(mainCamera.transform);
            studioCameraColliderControllerGO.transform.localPosition = new Vector3(0f, 0f, 0f);
            studioCameraColliderControllerGO.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            studioCameraColliderControllerGO.layer = CameraLayerDefault;
#endif

            var menu = new SceneEffectsCategory(StudioSceneSettings.PluginNameInternal);
            MapMasking = menu.AddToggleSet("Map Masking", MapMaskingSetter, false);
            NearClipPlane = menu.AddSliderSet("Near Clip Plane", NearClipSetter, NearClipDefault, 0.01f, 10f);
            FarClipPlane = menu.AddSliderSet("Far Clip Plane", FarClipSetter, FarClipDefault, 1f, 10000f);
            ShadowDistance = menu.AddSliderSet("Shadow Distance", ShadowDistanceSetter, ShadowDistanceDefault, 1f, 1000f);

            NearClipPlane.EnforceSliderMaximum = false;
            FarClipPlane.EnforceSliderMaximum = false;
            ShadowDistance.EnforceSliderMaximum = false;
        }

#if KK
        internal float NearClipDefault => Camera.main.nearClipPlane;
        internal void NearClipSetter(float value) => Camera.main.nearClipPlane = value;
        internal float FarClipDefault => Camera.main.farClipPlane;
        internal void FarClipSetter(float value) => Camera.main.farClipPlane = value;
        internal void MapMaskingSetter(bool value) => Camera.main.gameObject.layer = value ? StudioSceneSettings.CameraMapMaskingLayer : CameraLayerDefault;
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

        internal void MapMaskingSetter(bool value) => studioCameraColliderControllerGO.layer = value ? StudioSceneSettings.CameraMapMaskingLayer : CameraLayerDefault;

#endif
        internal float ShadowDistanceDefault => QualitySettings.shadowDistance;
        internal void ShadowDistanceSetter(float value) => QualitySettings.shadowDistance = value;
    }
}