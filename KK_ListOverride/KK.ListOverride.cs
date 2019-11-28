using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using System.IO;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ListOverride : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.listoverride";
        public const string PluginName = "List Override";
        public const string PluginNameInternal = "KK_ListOverride";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;
        private static readonly string ListOverrideFolder = Path.Combine(Paths.ConfigPath, PluginNameInternal);

        internal void Awake()
        {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
