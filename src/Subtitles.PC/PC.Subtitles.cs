namespace KK_Plugins
{
    public partial class Subtitles
    {
        private int previousLevel = -1;

        internal void OnLevelWasLoaded(int level)
        {
            if (level == previousLevel)
                return;
            previousLevel = level;
            Caption.UpdateScene();
        }
    }
}
