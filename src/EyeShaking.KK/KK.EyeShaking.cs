using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;

namespace KK_Plugins
{
    /// <summary>
    /// Adds shaking to a character's eye highlights when she is a virgin in an H scene
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class EyeShaking : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.eyeshaking";
        public const string PluginName = "Eye Shaking";
        public const string PluginNameInternal = Constants.Prefix + "_EyeShaking";
        public const string Version = "1.0";

        public static ConfigEntry<bool> Enabled { get; private set; }

        internal void Main()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
            CharacterApi.RegisterExtraBehaviour<EyeShakingController>(GUID);

            Enabled = Config.Bind("Config", "Enabled", true, "When enabled, virgins in H scenes will appear to have shaking eye highlights");
        }

        private static EyeShakingController GetController(ChaControl character) => character?.gameObject?.GetComponent<EyeShakingController>();
    }
}