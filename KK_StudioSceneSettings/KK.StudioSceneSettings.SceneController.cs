using UnityEngine;

namespace KK_Plugins.StudioSceneSettings
{
    public class SceneController : SceneControllerCore
    {
        internal override float NearClipDefault => Camera.main.nearClipPlane;
        internal override void NearClipSetter(float value) => Camera.main.nearClipPlane = value;
        internal override float FarClipDefault() => Camera.main.farClipPlane;
        internal override void FarClipSetter(float value) => Camera.main.farClipPlane = value;
    }
}