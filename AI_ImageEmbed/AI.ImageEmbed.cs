using BepInEx;
using System.Collections.Generic;

namespace KK_Plugins
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(MaterialEditor.GUID, "1.10")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ImageEmbed : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_ImageEmbed";
        private static readonly List<string> DefaultBGs = new List<string>()
        {
            "ai_mapsample00.png", "ai_mapsample01.png", "ai_mapsample02.png", "ai_mapsample03.png"
        };
        //AI doesn't come with any
        private static readonly List<string> DefaultFrames = new List<string>();
    }
}
