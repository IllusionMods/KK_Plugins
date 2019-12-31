using ExtensibleSaveFormat;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;

namespace KK_Plugins
{

    public partial class StudioSceneSettings
    {
        public class StudioSceneSettingsSceneController : SceneCustomFunctionController
        {
            protected override void OnSceneSave()
            {
                var data = new PluginData();
                if (NearClipPlane.Value == NearClipPlane.InitialValue)
                    data.data[$"NearClipPlane"] = null;
                else
                    data.data[$"NearClipPlane"] = NearClipPlane.Value;

                if (FarClipPlane.Value == FarClipPlane.InitialValue)
                    data.data[$"FarClipPlane"] = null;
                else
                    data.data[$"FarClipPlane"] = FarClipPlane.Value;

                data.data[$"MapMasking"] = MapMasking.Value;

                SetExtendedData(data);
            }

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                if (operation == SceneOperationKind.Load)
                {
                    var data = GetExtendedData();
                    if (data?.data == null)
                        ResetAll();
                    else
                    {
                        if (data.data.TryGetValue("NearClipPlane", out var nearClipPlane) && nearClipPlane != null)
                            NearClipPlane.Value = (float)nearClipPlane;
                        else
                            NearClipPlane.Reset();

                        if (data.data.TryGetValue("FarClipPlane", out var farClipPlane) && farClipPlane != null)
                            FarClipPlane.Value = (float)farClipPlane;
                        else
                            FarClipPlane.Reset();

                        if (data.data.TryGetValue("MapMasking", out var mapMasking) && mapMasking != null)
                            MapMasking.Value = (bool)mapMasking;
                        else
                            MapMasking.Reset();
                    }
                }
                else if (operation == SceneOperationKind.Clear)
                    ResetAll();
                else //Do not import saved data, keep current settings
                    return;
            }
        }
    }
}