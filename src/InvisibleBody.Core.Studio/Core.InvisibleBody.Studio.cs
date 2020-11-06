using KKAPI.Studio;
using KKAPI.Studio.UI;
using UniRx;

namespace KK_Plugins
{
    public partial class InvisibleBody
    {
        internal void Main()
        {
            if (StudioAPI.InsideStudio)
                RegisterStudioControls();
        }

        private static void RegisterStudioControls()
        {
            var invisibleSwitch = new CurrentStateCategorySwitch("Invisible Body", controller => controller.charInfo.GetComponent<InvisibleBodyCharaController>().Invisible);
            invisibleSwitch.Value.Subscribe(Observer.Create((bool value) =>
            {
                bool first = true;
                foreach (var controller in StudioAPI.GetSelectedControllers<InvisibleBodyCharaController>())
                {
                    //Prevent changing other characters when the value did not actually change
                    if (first && controller.Invisible == value)
                        break;

                    first = false;
                    controller.Invisible = value;
                }
            }));

            StudioAPI.GetOrCreateCurrentStateCategory("").AddControl(invisibleSwitch);
        }
    }
}
