using BepInEx;
using BepInEx.Configuration;
using Illusion.Game;
using KKAPI;
using KKAPI.Studio.SaveLoad;

namespace KK_Plugins
{
    /// <summary>
    /// When a Studio scene is loaded or imported, play a sound
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class StudioSceneLoadedSound : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.studiosceneloadedsound";
        public const string PluginName = "Studio Scene Loaded Sound";
        public const string PluginNameInternal = "StudioSceneLoadedSound";
        public const string Version = "1.1";

        public static ConfigEntry<bool> ImportSound { get; private set; }
        public static ConfigEntry<bool> LoadSound { get; private set; }

        internal void Main()
        {
            ImportSound = Config.Bind("Settings", "Import Sound", true, "Whether to play a sound on scene import");
            LoadSound = Config.Bind("Settings", "Load Sound", true, "Whether to play a sound on scene load");
            StudioSaveLoadApi.SceneLoad += OnSceneLoad;
        }

        private static void OnSceneLoad(object sender, SceneLoadEventArgs e)
        {
            if (e.Operation == SceneOperationKind.Import && ImportSound.Value)
                PlayAlertSound();
            else if (e.Operation == SceneOperationKind.Load && LoadSound.Value)
                PlayAlertSound();
        }

#if AI
        private static void PlayAlertSound() => Singleton<Manager.Resources>.Instance.SoundPack.Play(AIProject.SoundPack.SystemSE.OK_S);
#elif HS2
        private static void PlayAlertSound() => Utils.Sound.Play(SystemSE.ok_s);
#elif KK
        private static void PlayAlertSound() => Utils.Sound.Play(SystemSE.result_single);
#endif
    }
}