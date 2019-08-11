using BepInEx;
using Harmony;

namespace HairAccessoryCustomizer
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HairAccessoryCustomizer : BaseUnityPlugin
    {
        private void Main() => HarmonyInstance.Create(GUID).PatchAll(typeof(Hooks));
    }
}
