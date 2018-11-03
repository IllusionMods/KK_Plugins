using BepInEx;
using Harmony;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// Intercepts GetKey to prevent hotkeys from mods from firing while typing in an input field
/// </summary>
namespace KK_InputHotkeyLock
{
    [BepInPlugin("com.deathweasel.bepinex.inputhotkeylock", "Input Hotkey Lock", "1.0")]
    public class KK_InputHotkeyLock : BaseUnityPlugin
    {
        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.inputhotkeylock");
            harmony.PatchAll(typeof(KK_InputHotkeyLock));
        }
        /// <summary>
        /// Check if an input field is selected
        /// </summary>
        private static bool InputLock
        {
            get
            {
                if (GUIUtility.keyboardControl > 0)
                    return false; //UI elements from some mods
                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    if (EventSystem.current.currentSelectedGameObject.name == "InputField")
                        return false; //Size fields in chara maker which don't have InputField component for some reason
                    if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null)
                        return false; //All other InputFields
                }
                return true;
            }
        }
        /// <summary>
        /// GetKey hooks. When InputLock returns false the GetKey function will be prevented from running.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKey), new[] { typeof(KeyCode) })]
        public static bool GetKeyCode(KeyCode key)
        {
            return InputLock;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKey), new[] { typeof(string) })]
        public static bool GetKeyString(string name)
        {
            return InputLock;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(KeyCode) })]
        public static bool GetKeyDownCode(KeyCode key)
        {
            return InputLock;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(string) })]
        public static bool GetKeyDownString(string name)
        {
            return InputLock;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), new[] { typeof(KeyCode) })]
        public static bool GetKeyUpCode(KeyCode key)
        {
            return InputLock;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), new[] { typeof(string) })]
        public static bool GetKeyUpString(string name)
        {
            return InputLock;
        }
    }
}
