using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;

namespace UncensorSelector
{
    /// <summary>
    /// Plugin for assigning uncensors to characters individually
    /// </summary>
    [BepInDependency(Sideloader.Sideloader.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    internal partial class UncensorSelector : BaseUnityPlugin
    {
        private static ManualLogSource _logsource;

        public static ConfigWrapper<bool> GenderBender { get; private set; }
        public static ConfigWrapper<string> DefaultMaleBody { get; private set; }
        public static ConfigWrapper<string> DefaultMalePenis { get; private set; }
        public static ConfigWrapper<string> DefaultMaleBalls { get; private set; }
        public static ConfigWrapper<string> DefaultFemaleBody { get; private set; }
        public static ConfigWrapper<string> DefaultFemalePenis { get; private set; }
        public static ConfigWrapper<string> DefaultFemaleBalls { get; private set; }
        public static ConfigWrapper<bool> DefaultFemaleDisplayBalls { get; private set; }

        private void Start()
        {
            _logsource = Logger;
            HarmonyWrapper.PatchAll(typeof(Hooks));

            GenderBender = Config.Wrap("Config", "Genderbender Allowed", "Whether or not genderbender characters are allowed. " +
                "When disabled, girls will always have a female body with no penis, boys will always have a male body and a penis. " +
                "Genderbender characters will still load in Studio for scene compatibility.", true);
            DefaultMaleBody = Config.Wrap("Config", "Default Male Body", "Body to use if the character does not have one set. " +
                "The censored body will not be selected randomly if there are any alternatives.", "Random");
            DefaultMalePenis = Config.Wrap("Config", "Default Male Penis", "Penis to use if the character does not have one set. " +
                "The mosaic penis will not be selected randomly if there are any alternatives.", "Random");
            DefaultMaleBalls = Config.Wrap("Config", "Default Male Balls", "Balls to use if the character does not have one set. " +
                "The mosaic penis will not be selected randomly if there are any alternatives.", "Random");
            DefaultFemaleBody = Config.Wrap("Config", "Default Female Body", "Body to use if the character does not have one set. " +
                "The censored body will not be selected randomly if there are any alternatives.", "Random");
            DefaultFemalePenis = Config.Wrap("Config", "Default Female Penis", "Penis to use if the character does not have one set. " +
                "The mosaic penis will not be selected randomly if there are any alternatives.", "Random");
            DefaultFemaleBalls = Config.Wrap("Config", "Default Female Balls", "Balls to use if the character does not have one set. " +
                "The mosaic penis will not be selected randomly if there are any alternatives.", "Random");
            DefaultFemaleDisplayBalls = Config.Wrap("Config", "Default Female Balls Display", "Whether balls will be displayed on females if not otherwise configured.", false);
        }

        public static void Log(LogLevel level, object text) => _logsource.Log(level, text);
        public static void Log(object text) => _logsource.Log(LogLevel.Info, text);
    }
}
