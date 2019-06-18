using BepInEx;
using Harmony;

namespace HairAccessoryCustomizer
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HairAccessoryCustomizer : BaseUnityPlugin
    {
        private void Main() => HarmonyInstance.Create(GUID).PatchAll(typeof(Hooks));
    }
}
