using BepInEx;
using KKAPI;

namespace KK_Plugins
{
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MaleJuice : BaseUnityPlugin { }
}
