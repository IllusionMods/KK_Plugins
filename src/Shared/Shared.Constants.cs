namespace KK_Plugins
{
    internal static class Constants
    {
#if AI
        internal const string GameName = "AI Girl";
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string MainGameProcessName = "AI-Syoujyo";
#elif EC
        internal const string GameName = "Emotion Creators";
        internal const string MainGameProcessName = "EmotionCreators";
#elif HS
        internal const string GameName = "Honey Select";
        internal const string StudioProcessName = "StudioNEO_64";
        internal const string MainGameProcessName = "HoneySelect_64";
        internal const string BattleArenaProcessName = "BattleArena_64";
#elif HS2
        internal const string GameName = "Honey Select 2";
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string MainGameProcessName = "HoneySelect2";
#elif KK
        internal const string GameName = "Koikatsu";
        internal const string StudioProcessName = "CharaStudio";
        internal const string MainGameProcessName = "Koikatu";
        internal const string MainGameProcessNameSteam = "Koikatsu Party";
        internal const string VRProcessName = "KoikatuVR";
        internal const string VRProcessNameSteam = "Koikatsu Party VR";
#endif
    }
}
