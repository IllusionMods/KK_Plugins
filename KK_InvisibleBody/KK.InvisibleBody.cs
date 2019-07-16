using BepInEx;
using BepInEx.Logging;
using Harmony;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using System.ComponentModel;
using UniRx;
using Logger = BepInEx.Logger;
/// <summary>
/// Sets the selected characters invisible in Studio. Invisible state saves and loads with the scene.
/// Also sets female characters invisible in H scenes.
/// </summary>
namespace InvisibleBody
{
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class InvisibleBody : BaseUnityPlugin
    {
        [Category("Settings")]
        [DisplayName("Hide built-in hair accessories")]
        [Description("Whether or not to hide accesories (such as scrunchies) attached to back hairs.")]
        public static ConfigWrapper<bool> HideHairAccessories { get; private set; }

        private void Start()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(InvisibleBody));

            HideHairAccessories = new ConfigWrapper<bool>("HideHairAccessories", PluginNameInternal, true);

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

        public static void Log(LogLevel level, object text) => Logger.Log(level, text);
        public static void Log(object text) => Logger.Log(LogLevel.Info, text);
    }
}
