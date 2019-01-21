using BepInEx;
using Harmony;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_InputHotkeyBlock
{
    /// <summary>
    /// Intercepts GetKey to prevent hotkeys from mods from firing while typing in an input field
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_InputHotkeyBlock : BaseUnityPlugin
    {
        public const string PluginName = "Input Hotkey Block";
        public const string GUID = "com.deathweasel.bepinex.inputhotkeyblock";
        public const string Version = "1.1";

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_InputHotkeyBlock));
        }
        /// <summary>
        /// Check if an input field is selected
        /// </summary>
        private static bool HotkeyBlock
        {
            get
            {
                if (GUIUtility.keyboardControl > 0)
                    return false; //UI elements from some mods
                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    if (EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
                        return false; //Size fields in chara maker, coordinate fields in Studio
                    if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null)
                        return false; //All other InputFields
                }
                return true;
            }
        }
        //GetKey hooks. When HotkeyBlock returns false the GetKey functions will be prevented from running.
        [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), new[] { typeof(KeyCode) })]
        public static bool GetKeyCode(KeyCode key) => HotkeyBlock;
        [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), new[] { typeof(string) })]
        public static bool GetKeyString(string name) => HotkeyBlock;
        [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(KeyCode) })]
        public static bool GetKeyDownCode(KeyCode key) => HotkeyBlock;
        [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(string) })]
        public static bool GetKeyDownString(string name) => HotkeyBlock;
        [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), new[] { typeof(KeyCode) })]
        public static bool GetKeyUpCode(KeyCode key) => HotkeyBlock;
        [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), new[] { typeof(string) })]
        public static bool GetKeyUpString(string name) => HotkeyBlock;
    }
}
