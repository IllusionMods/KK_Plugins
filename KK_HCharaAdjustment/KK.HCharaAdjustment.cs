using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using UnityEngine;

namespace KK_Plugins
{
    /// <summary>
    /// Plugin for adjusting the female character in H scene independently of the male. Needs a UI.
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HCharaAdjustment : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.hcharaadjustment";
        public const string PluginName = "H Character Adjustment";
        public const string PluginNameInternal = "KK_HCharaAdjustment";
        public const string Version = "1.0.1";

        private static float AdjustmentX = 0;
        private static float AdjustmentY = 0;
        private static float AdjustmentZ = 0;

        public static ConfigEntry<KeyboardShortcut> AdjustmentXPlus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> AdjustmentXMinus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> AdjustmentXReset { get; private set; }
        public static ConfigEntry<KeyboardShortcut> AdjustmentYPlus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> AdjustmentYMinus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> AdjustmentYReset { get; private set; }
        public static ConfigEntry<KeyboardShortcut> AdjustmentZPlus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> AdjustmentZMinus { get; private set; }
        public static ConfigEntry<KeyboardShortcut> AdjustmentZReset { get; private set; }

        internal void Main()
        {
            HarmonyWrapper.PatchAll(typeof(Hooks));

            AdjustmentXPlus = Config.Bind("Keyboard Shortcuts", "Adjustment X Plus", new KeyboardShortcut(KeyCode.P), "Increase X axis adjustment");
            AdjustmentXMinus = Config.Bind("Keyboard Shortcuts", "Adjustment X Minus", new KeyboardShortcut(KeyCode.O), "Decrease X axis adjustment");
            AdjustmentXReset = Config.Bind("Keyboard Shortcuts", "Adjustment X Reset", new KeyboardShortcut(KeyCode.I), "Reset X axis adjustment");
            AdjustmentYPlus = Config.Bind("Keyboard Shortcuts", "Adjustment Y Plus", new KeyboardShortcut(KeyCode.L), "Increase Y axis adjustment");
            AdjustmentYMinus = Config.Bind("Keyboard Shortcuts", "Adjustment Y Minus", new KeyboardShortcut(KeyCode.K), "Decrease Y axis adjustment");
            AdjustmentYReset = Config.Bind("Keyboard Shortcuts", "Adjustment Y Reset", new KeyboardShortcut(KeyCode.J), "Reset Y axis adjustment");
            AdjustmentZPlus = Config.Bind("Keyboard Shortcuts", "Adjustment Z Plus", new KeyboardShortcut(KeyCode.M), "Increase Z axis adjustment");
            AdjustmentZMinus = Config.Bind("Keyboard Shortcuts", "Adjustment Z Minus", new KeyboardShortcut(KeyCode.N), "Decrease Z axis adjustment");
            AdjustmentZReset = Config.Bind("Keyboard Shortcuts", "Adjustment Z Reset", new KeyboardShortcut(KeyCode.B), "Reset Z axis adjustment");
        }
    }
}