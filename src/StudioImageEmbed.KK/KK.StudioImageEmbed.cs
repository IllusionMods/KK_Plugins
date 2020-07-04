using BepInEx;
using System.Collections.Generic;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(MaterialEditor.MEStudio.GUID, "2.0")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ImageEmbed : BaseUnityPlugin
    {
        private static readonly List<string> DefaultBGs = new List<string>()
        {
            "bg_01.png", "bg_02.png", "bg_03.png", "bg_04.png", "bg_05.png", "bg_06.png", "bg_07.png", "bg_08.png", "bg_09.png", "bg_10.png",
            "チャペル_夕.png", "チャペル_昼.png", "公園_夕.png", "公園_夜.png", "公園_昼.png", "夜景.png", "宇宙空間01.png", "宇宙空間02.png", "宇宙空間03.png", "宇宙空間04.png",
            "遊園地_夕.png", "遊園地_夜.png", "遊園地_昼.png", "駅前_夕.png", "駅前_夜.png", "駅前_昼.png"
        };
        private static readonly List<string> DefaultFrames = new List<string>()
        {
            "koi_studio_frame_00.png", "koi_studio_frame_01.png", "koi_studio_frame_02.png", "koi_studio_frame_03.png", "koi_studio_frame_04.png", "koi_studio_frame_05.png", "koi_studio_frame_06.png"
        };
    }
}
