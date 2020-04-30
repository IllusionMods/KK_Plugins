using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    public partial class InputHotkeyBlock
    {
        internal static partial class Hooks
        {
            //GetKey hooks. When HotkeyBlock returns false the GetKey functions will be prevented from running.
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(KeyCode))]
            internal static bool GetKeyCode() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(string))]
            internal static bool GetKeyString() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(KeyCode))]
            internal static bool GetKeyDownCode() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(string))]
            internal static bool GetKeyDownString() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(KeyCode))]
            internal static bool GetKeyUpCode() => HotkeyBlock;
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(string))]
            internal static bool GetKeyUpString() => HotkeyBlock;
        }
    }
}
