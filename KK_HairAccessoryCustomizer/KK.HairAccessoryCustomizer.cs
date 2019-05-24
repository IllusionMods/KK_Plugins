using BepInEx;
using Harmony;
/// <summary>
/// Individual customization of hair accessories for adding hair gloss, color matching, etc.
/// </summary>
namespace HairAccessoryCustomizer
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HairAccessoryCustomizer : BaseUnityPlugin
    {
        private void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(Hooks));
        }
    }
}
