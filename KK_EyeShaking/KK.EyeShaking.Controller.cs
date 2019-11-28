using KKAPI;
using KKAPI.Chara;

namespace KK_Plugins
{
    public partial class EyeShaking
    {
        public class EyeShakingController : CharaCustomFunctionController
        {
            internal bool IsVirgin { get; set; } = true;
            internal bool IsVirginOrg { get; set; } = true;
            internal bool IsInit { get; set; } = false;

            protected override void OnCardBeingSaved(GameMode currentGameMode) { }
            protected override void OnReload(GameMode currentGameMode, bool maintainState) { }

            internal void HSceneStart(bool virgin)
            {
                IsVirgin = virgin;
                IsVirginOrg = virgin;
                IsInit = true;
            }

            internal void HSceneEnd()
            {
                ChaControl.ChangeEyesShaking(false);
                IsInit = false;
            }

            internal void OnInsert() => IsVirgin = false;
            internal void AddOrgasm() => IsVirginOrg = false;

            protected override void Update()
            {
                if (Enabled.Value && IsInit && (IsVirgin || IsVirginOrg))
                    ChaControl.ChangeEyesShaking(true);
            }
        }
    }
}