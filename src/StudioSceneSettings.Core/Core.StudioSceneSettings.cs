using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using UnityEngine;

namespace KK_Plugins.StudioSceneSettings
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class StudioSceneSettings : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studioscenesettings";
        public const string PluginName = "StudioSceneSettings";
        public const string PluginNameInternal = Constants.Prefix + "_StudioSceneSettings";
        public const string Version = "1.3";
        internal static new ManualLogSource Logger;

#if KK
        internal const int CameraMapMaskingLayer = 26;
#else
        internal const int CameraMapMaskingLayer = 22;
        private static bool DelayingLoadVanish = true;
#endif

        internal void Main()
        {
            Logger = base.Logger;
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(GUID);
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }

#if AI || HS2
    public class StudioCameraColliderController : MonoBehaviour
    {
        protected void OnTriggerEnter(Collider other) => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Method("OnTriggerEnter", other).GetValue();
        protected void OnTriggerStay(Collider other) => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Method("OnTriggerStay", other).GetValue();
        protected void OnTriggerExit(Collider other) => Traverse.Create(Studio.Studio.Instance.cameraCtrl).Method("OnTriggerExit", other).GetValue();
    }
#endif
}