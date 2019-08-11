using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;

namespace InvisibleBody
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class InvisibleBody
    {
        private static ManualLogSource _logsource;
        public static ConfigWrapper<bool> HideHairAccessories { get; private set; }
        private void Start()
        {
            _logsource = Logger;
            HarmonyWrapper.PatchAll(typeof(InvisibleBody));

            HideHairAccessories = Config.Wrap("Config", "Hide built-in hair accessories", "Whether or not to hide accesories (such as scrunchies) attached to back hairs.", true);
        }

        public static void Log(LogLevel level, object text) => _logsource.Log(level, text);
        public static void Log(object text) => _logsource.Log(LogLevel.Info, text);
    }
}
