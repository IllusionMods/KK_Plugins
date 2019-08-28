using BepInEx;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using UniRx;
/// <summary>
/// Sets the selected characters invisible in Studio. Invisible state saves and loads with the scene.
/// Also sets female characters invisible in H scenes.
/// </summary>
namespace KK_Plugins
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInDependency(ExtensibleSaveFormat.ExtendedSave.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class InvisibleBody : BaseUnityPlugin
    {
        private void Main()
        {
            if (StudioAPI.InsideStudio)
                RegisterStudioControls();
        }

        private static void RegisterStudioControls()
        {
            var invisibleSwitch = new CurrentStateCategorySwitch("Invisible Body", controller => controller.charInfo.GetComponent<InvisibleBodyCharaController>().Invisible);
            invisibleSwitch.Value.Subscribe(Observer.Create((bool value) =>
            {
                var controller = GetSelectedStudioController();
                if (controller != null)
                    controller.Invisible = value;
            }));

            StudioAPI.GetOrCreateCurrentStateCategory("").AddControl(invisibleSwitch);
        }

        private static InvisibleBodyCharaController GetSelectedStudioController() => FindObjectOfType<MPCharCtrl>()?.ociChar?.charInfo?.GetComponent<InvisibleBodyCharaController>();
    }
}
