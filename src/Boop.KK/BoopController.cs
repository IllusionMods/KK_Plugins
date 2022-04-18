using System;
using KKAPI;
using KKAPI.Chara;

namespace Boop
{
    public class BoopController : CharaCustomFunctionController
    {
        public BoopController()
        {
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
        }

        private new void OnDestroy()
        {
            Boop.UnregisterChar(base.ChaControl);
        }

        private new void Start()
        {
            Boop.RegisterChar(base.ChaControl);
        }
    }
}
