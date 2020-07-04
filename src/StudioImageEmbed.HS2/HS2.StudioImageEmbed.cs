using BepInEx;
using System.Collections.Generic;

namespace KK_Plugins
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(MaterialEditor.MaterialEditorPlugin.GUID, "2.0")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ImageEmbed : BaseUnityPlugin
    {
        //HS2 doesn't come with BGs or frames
        private static readonly List<string> DefaultBGs = new List<string>();
        private static readonly List<string> DefaultFrames = new List<string>();
    }
}
