using System.Collections.Generic;

namespace KK_Plugins
{
    internal class ImageEmbedConstants
    {
#if AI
        internal static readonly List<string> DefaultBGs = new List<string>
        {
            "ai_mapsample00.png", "ai_mapsample01.png", "ai_mapsample02.png", "ai_mapsample03.png"
        };
        //AI doesn't come with any
        internal static readonly List<string> DefaultFrames = new List<string>();
#elif HS2
        //HS2 doesn't come with BGs or frames
        internal static readonly List<string> DefaultBGs = new List<string>();
        internal static readonly List<string> DefaultFrames = new List<string>();
#elif KK
        internal static readonly List<string> DefaultBGs = new List<string>
        {
            "bg_01.png", "bg_02.png", "bg_03.png", "bg_04.png", "bg_05.png", "bg_06.png", "bg_07.png", "bg_08.png", "bg_09.png", "bg_10.png",
            "チャペル_夕.png", "チャペル_昼.png", "公園_夕.png", "公園_夜.png", "公園_昼.png", "夜景.png", "宇宙空間01.png", "宇宙空間02.png", "宇宙空間03.png", "宇宙空間04.png",
            "遊園地_夕.png", "遊園地_夜.png", "遊園地_昼.png", "駅前_夕.png", "駅前_夜.png", "駅前_昼.png"
        };
        internal static readonly List<string> DefaultFrames = new List<string>
        {
            "koi_studio_frame_00.png", "koi_studio_frame_01.png", "koi_studio_frame_02.png", "koi_studio_frame_03.png", "koi_studio_frame_04.png", "koi_studio_frame_05.png", "koi_studio_frame_06.png"
        };
#elif KKS
        internal static readonly List<string> DefaultBGs = new List<string>
        {
            "bg_01.png", "bg_02.png", "bg_03.png", "bg_04.png", "bg_05.png", "bg_06.png", "bg_07.png", "bg_08.png", "bg_09.png", "bg_10.png", "bg_21.png", "bg_22.png", 
            "bg_23.png", "bg_24.png", "bg_25.png", "bg_26.png", "bg_27.png", "bg_28.png", "bg_29.png", "bg_30.png", "bg_31.png", 
            "宇宙空間01.png", "宇宙空間02.png", "宇宙空間03.png", "宇宙空間04.png",
        };
        internal static readonly List<string> DefaultFrames = new List<string>
        {
            "koi_studio_frame_00.png", "koi_studio_frame_01.png", "koi_studio_frame_02.png", "koi_studio_frame_03.png", "koi_studio_frame_04.png", "koi_studio_frame_05.png", "koi_studio_frame_06.png"
        };
#endif
    }
}
