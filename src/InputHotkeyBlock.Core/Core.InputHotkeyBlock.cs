using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_Plugins
{
    /// <summary>
    /// Intercepts GetKey to prevent hotkeys from mods from firing while typing in an input field
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class InputHotkeyBlock : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.inputhotkeyblock";
        public const string PluginName = "Input Hotkey Block";
        public const string PluginNameInternal = Constants.Prefix + "_InputHotkeyBlock";
        public const string Version = "1.2";

        internal void Main() => Harmony.CreateAndPatchAll(typeof(Hooks));
        /// <summary>
        /// Check if an input field is selected
        /// </summary>
        private static bool HotkeyBlock
        {
            get
            {
                if (GUIUtility.keyboardControl > 0)
                    return false; //UI elements from some mods
                if (EventSystem.current?.currentSelectedGameObject != null) //inline null checks don't work for TMP fields apparently, keep this
                {
#if !PH
                    if (EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
                        return false; //Size fields in chara maker, coordinate fields in Studio
#endif
                    if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null)
                        return false; //All other InputFields
                }
                return true;
            }
        }
    }
}
