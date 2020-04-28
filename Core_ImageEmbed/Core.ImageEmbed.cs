using BepInEx;
using BepInEx.Logging;
using BepInEx.Harmony;

namespace KK_Plugins
{
    public class ImageEmbedCore : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.imageembed";
        public const string PluginName = "Image Embed";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        internal void Awake()
        {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }

    internal static class Hooks
    {

    }
}
