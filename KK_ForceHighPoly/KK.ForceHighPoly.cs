using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using System.Collections;

namespace KK_Plugins
{
    /// <summary>
    /// Replaces all _low assets with normal assets, forcing everything to load as high poly
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ForceHighPoly : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.forcehighpoly";
        public const string PluginName = "Force High Poly";
        public const string PluginNameInternal = "KK_ForceHighPoly";
        public const string Version = "1.2";

        public static ConfigEntry<bool> Enabled { get; private set; }

        internal void Main()
        {
            Enabled = Config.Bind("Config", "High poly mode", true, "Whether or not to load high poly assets. May require exiting to main menu to take effect.");

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        private static IEnumerator ChangeHairAsyncPostfix(ChaControl instance, int kind)
        {
            var hairObject = instance.objHair[kind];
            if (hairObject != null)
                foreach (var dynamicBone in hairObject.GetComponentsInChildren<DynamicBone>(true))
                    dynamicBone.enabled = true;

            yield break;
        }
    }
}
