#if KK || EC || AI || HS2 || PH || KKS
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;

namespace KK_Plugins
{
    public class CharaController : CharaCustomFunctionController
    {
        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            if (MakerAPI.InsideAndLoaded && !Autosave.Autosaving)
            {
                Autosave.ResetMakerCoroutine();
            }
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            if (MakerAPI.InsideAndLoaded && !Autosave.Autosaving)
            {
                Autosave.ResetMakerCoroutine();
            }
        }
    }
}
#endif
