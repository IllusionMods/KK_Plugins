using KKAPI;
using KKAPI.Chara;

namespace KK_Plugins
{
    public class BoopController : CharaCustomFunctionController
    {
        protected override void OnCardBeingSaved(GameMode currentGameMode) { }

        private new void OnDestroy()
        {
            Boop.UnregisterChar(ChaControl);
        }

        private new void Start()
        {
            Boop.RegisterChar(ChaControl);
        }
    }
}
