using BepInEx;
using BepInEx.Logging;

namespace KK_Plugins.StudioSceneSettings
{
    public abstract class StudioSceneSettingsCore : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studioscenesettings";
        public const string PluginName = "StudioSceneSettings";
        public const string Version = "1.1";
        internal static new ManualLogSource Logger;

        internal const int CameraMapMaskingLayer = 26;

        internal void Main() => Logger = base.Logger;
    }
}