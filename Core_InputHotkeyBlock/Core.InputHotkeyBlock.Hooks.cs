using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class InputHotkeyBlock
    {
        internal static partial class Hooks
        {
            //GetKey hooks. When HotkeyBlock returns false the GetKey functions will be prevented from running.
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), new[] { typeof(KeyCode) })]
            public static bool GetKeyCode() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), new[] { typeof(string) })]
            public static bool GetKeyString() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(KeyCode) })]
            public static bool GetKeyDownCode() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(string) })]
            public static bool GetKeyDownString() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), new[] { typeof(KeyCode) })]
            public static bool GetKeyUpCode() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), new[] { typeof(string) })]
            public static bool GetKeyUpString() => HotkeyBlock;
        }
    }
}
