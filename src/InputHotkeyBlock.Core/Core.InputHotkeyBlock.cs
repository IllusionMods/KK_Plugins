using BepInEx.Harmony;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_Plugins
{
    /// <summary>
    /// Intercepts GetKey to prevent hotkeys from mods from firing while typing in an input field
    /// </summary>
    public partial class InputHotkeyBlock
    {
        public const string PluginName = "Input Hotkey Block";
        public const string GUID = "com.deathweasel.bepinex.inputhotkeyblock";
        public const string Version = "1.2";

        internal void Main() => HarmonyWrapper.PatchAll(typeof(Hooks));
        /// <summary>
        /// Check if an input field is selected
        /// </summary>
        private static bool HotkeyBlock
        {
            get
            {
                if (GUIUtility.keyboardControl > 0)
                    return false; //UI elements from some mods
                if (EventSystem.current?.currentSelectedGameObject != null)
                {
                    if (EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
                        return false; //Size fields in chara maker, coordinate fields in Studio
                    if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null)
                        return false; //All other InputFields
                }
                return true;
            }
        }
    }
}
