using Cinemachine;
using HarmonyLib;

namespace KK_Plugins.StudioSceneSettings
{
    public class SceneController : SceneControllerCore
    {
        internal override float NearClipDefault => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Field("lensSettings").GetValue<LensSettings>().NearClipPlane;

        internal override void NearClipSetter(float value)
        {
            Studio.CameraControl cameraCtrl = Studio.Studio.Instance.cameraCtrl;
            var field = Traverse.Create(cameraCtrl).Field("lensSettings");
            var lensSettings = field.GetValue<LensSettings>();
            lensSettings.NearClipPlane = value;
            field.SetValue(lensSettings);
            cameraCtrl.fieldOfView = cameraCtrl.fieldOfView;
        }

        internal override float FarClipDefault() => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Field("lensSettings").GetValue<LensSettings>().FarClipPlane;

        internal override void FarClipSetter(float value)
        {
            Studio.CameraControl cameraCtrl = Studio.Studio.Instance.cameraCtrl;
            var field = Traverse.Create(cameraCtrl).Field("lensSettings");
            var lensSettings = field.GetValue<LensSettings>();
            lensSettings.FarClipPlane = value;
            field.SetValue(lensSettings);
            cameraCtrl.fieldOfView = cameraCtrl.fieldOfView;
        }
    }
}