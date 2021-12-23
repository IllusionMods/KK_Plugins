using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ListOverride : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.listoverride";
        public const string PluginName = "List Override";
        public const string PluginNameInternal = Constants.Prefix + "_ListOverride";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;
        private static readonly string ListOverrideFolder = Path.Combine(Paths.ConfigPath, PluginNameInternal);

        private void Awake()
        {
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}
