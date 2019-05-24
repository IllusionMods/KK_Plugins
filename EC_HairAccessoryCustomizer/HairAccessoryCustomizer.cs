using BepInEx;
using BepInEx.Harmony;

namespace HairAccessoryCustomizer
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HairAccessoryCustomizer : BaseUnityPlugin
    {
        private void Main() => HarmonyWrapper.PatchAll(typeof(Hooks));
    }
}
