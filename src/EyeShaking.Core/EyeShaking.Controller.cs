using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Studio;

namespace KK_Plugins
{
    public partial class EyeShaking
    {
        public class EyeShakingController : CharaCustomFunctionController
        {
            private bool _EyeShaking;
            public bool EyeShaking
            {
                get => _EyeShaking;
                set
                {
                    _EyeShaking = value;
                    ChaControl.ChangeEyesShaking(value);
                }
            }
            internal bool IsVirgin { get; set; } = true;
            internal bool IsVirginOrg { get; set; } = true;
            internal bool IsInit { get; set; }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                if (!StudioAPI.InsideStudio) return;

                var data = new PluginData();
                data.data.Add("EyeShaking", EyeShaking);
                SetExtendedData(data);
            }
            protected override void OnReload(GameMode currentGameMode, bool maintainState)
            {
                if (!StudioAPI.InsideStudio) return;

                var data = GetExtendedData();
                if (data != null && data.data.TryGetValue("EyeShaking", out var loadedEyeShakingState))
                    EyeShaking = (bool)loadedEyeShakingState;
                else
                    //Set EyeShaking on or off, for when characters are replaced in Studio
                    EyeShaking = _EyeShaking;
            }

            internal void HSceneStart(bool virgin)
            {
                IsVirgin = virgin;
                IsVirginOrg = virgin;
                IsInit = true;
            }

            internal void HSceneEnd()
            {
                EyeShaking = false;
                IsInit = false;
            }

            internal void OnInsert() => IsVirgin = false;
            internal void AddOrgasm() => IsVirginOrg = false;

            protected override void Update()
            {
                if (Enabled.Value && IsInit && (IsVirgin || IsVirginOrg))
                    EyeShaking = true;
            }
        }
    }
}