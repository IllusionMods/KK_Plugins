namespace KK_Plugins
{
    internal static class Constants
    {
#if AI
        internal const string Prefix = "AI";
        internal const string GameName = "AI Girl";
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string MainGameProcessName = "AI-Syoujyo";
#elif EC
        internal const string Prefix = "EC";
        internal const string GameName = "Emotion Creators";
        internal const string MainGameProcessName = "EmotionCreators";
#elif HS
        internal const string Prefix = "HS";
        internal const string GameName = "Honey Select";
        internal const string StudioProcessName = "StudioNEO_64";
        internal const string MainGameProcessName = "HoneySelect_64";
        internal const string BattleArenaProcessName = "BattleArena_64";
#elif HS2
        internal const string Prefix = "HS2";
        internal const string GameName = "Honey Select 2";
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string MainGameProcessName = "HoneySelect2";
        internal const string VRProcessName = "HoneySelect2VR";
#elif KK
        internal const string Prefix = "KK";
        internal const string GameName = "Koikatsu";
        internal const string StudioProcessName = "CharaStudio";
        internal const string MainGameProcessName = "Koikatu";
        internal const string MainGameProcessNameSteam = "Koikatsu Party";
        internal const string VRProcessName = "KoikatuVR";
        internal const string VRProcessNameSteam = "Koikatsu Party VR";
#elif KKS
        internal const string Prefix = "KKS";
        internal const string GameName = "Koikatsu Sunshine";
        internal const string MainGameProcessName = "KoikatsuSunshine";
        internal const string StudioProcessName = "CharaStudio";
        internal const string VRProcessName = "KoikatsuSunshine_VR";
#elif PH
        internal const string Prefix = "PH";
        internal const string GameName = "Play Home";
        internal const string StudioProcessName = "PlayHomeStudio64bit";
        internal const string MainGameProcessName = "PlayHome64bit";
#elif PC
        internal const string Prefix = "PC";
        internal const string GameName = "Play Club";
        internal const string MainGameProcessName = "PlayClub";
#elif SBPR
        internal const string Prefix = "SBPR";
        internal const string GameName = "Sexy Beach Premium Resort";
        internal const string MainGameProcessName = "SexyBeachPR_64";
#endif
    }
}
