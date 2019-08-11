using BepInEx;
using BepInEx.Harmony;

namespace HairAccessoryCustomizer
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HairAccessoryCustomizer : BaseUnityPlugin
    {
        private void Main() => HarmonyWrapper.PatchAll(typeof(Hooks));
    }
}
