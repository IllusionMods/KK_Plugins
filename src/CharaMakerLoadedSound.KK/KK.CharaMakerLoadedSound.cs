using BepInEx;
using Illusion.Game;
using KKAPI;

namespace KK_Plugins
{
    /// <summary>
    /// When Chara Maker starts, wait a bit for lag to stop then play a sound
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class CharaMakerLoadedSound : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.charamakerloadedsound";
        public const string PluginName = "Character Maker Loaded Sound";
        public const string PluginNameInternal = Constants.Prefix + "_CharaMakerLoadedSound";
        public const string Version = "1.0";

        internal void Main() => KKAPI.Maker.MakerAPI.MakerFinishedLoading += (s, e) => Utils.Sound.Play(SystemSE.result_single);
    }
}
