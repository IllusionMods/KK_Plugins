using BepInEx;
using HarmonyLib;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class AnimationOverdrive : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.animationoverdrive";
        public const string PluginName = "Animation Overdrive";
        public const string PluginNameInternal = Constants.Prefix + "_AnimationOverdrive";
        public const string Version = "1.1";
        private const float AnimationSpeedMax = 1000f;

        internal void Main() => Harmony.CreateAndPatchAll(typeof(Hooks));
    }
}
