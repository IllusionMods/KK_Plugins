#if KK || AI || HS2 || PH
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;

namespace KK_Plugins
{
    internal class StudioController : SceneCustomFunctionController
    {
        protected override void OnSceneSave()
        {
            if (!Autosave.Autosaving)
            {
                Autosave.ResetStudioCoroutine();
            }
        }

        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            if (!Autosave.Autosaving)
            {
                Autosave.ResetStudioCoroutine();
            }
        }
    }
}
#endif