using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class InputHotkeyBlock
    {
        internal static partial class Hooks
        {
            /// <summary>
            /// Click was handled by a GUI element. Don't advance text.
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ADV.TextScenario), "MessageWindowProc")]
            public static void MessageWindowProcPreHook(object nextInfo)
            {
                if (Event.current.type == EventType.Used)
                    Traverse.Create(nextInfo).Field("isNext").SetValue(false);
            }
        }
    }
}
