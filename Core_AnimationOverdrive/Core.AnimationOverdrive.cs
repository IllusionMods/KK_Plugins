using BepInEx.Harmony;

namespace KK_Plugins
{
    public partial class AnimationOverdrive
    {
        public const string GUID = "com.deathweasel.bepinex.animationoverdrive";
        public const string PluginName = "Animation Overdrive";
        public const string Version = "1.1";
        private const float AnimationSpeedMax = 1000f;

        internal void Main() => HarmonyWrapper.PatchAll(typeof(Hooks));
    }
}