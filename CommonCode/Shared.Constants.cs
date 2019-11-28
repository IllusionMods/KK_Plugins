namespace KK_Plugins
{
    internal static class Constants
    {
#if AI
        internal const string GameName = "AI Girl";
        internal const string StudioProcessName = "StudioNEOV2";
#elif EC
        internal const string GameName = "Emotion Creators";
#elif HS
        internal const string GameName = "Honey Select";
        internal const string StudioProcessName = "StudioNEO";
#elif KK
        internal const string GameName = "Koikatsu";
        internal const string StudioProcessName = "CharaStudio";
#endif
    }
}
