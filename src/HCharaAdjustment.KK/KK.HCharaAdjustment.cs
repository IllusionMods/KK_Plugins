using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;
using UnityEngine;

namespace KK_Plugins
{
    /// <summary>
    /// Plugin for adjusting the female character in H scene independently of the male
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class HCharaAdjustment : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.hcharaadjustment";
        public const string PluginName = "H Character Adjustment";
        public const string PluginNameInternal = Constants.Prefix + "_HCharaAdjustment";
        public const string Version = "2.0";
        internal static new ManualLogSource Logger;

        public static ConfigEntry<KeyboardShortcut> Female1GuideObject { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Female2GuideObject { get; private set; }
        public static ConfigEntry<KeyboardShortcut> MaleGuideObject { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Female1GuideObjectReset { get; private set; }
        public static ConfigEntry<KeyboardShortcut> Female2GuideObjectReset { get; private set; }
        public static ConfigEntry<KeyboardShortcut> MaleGuideObjectReset { get; private set; }

        internal void Main()
        {
            Logger = base.Logger;

            Female1GuideObject = Config.Bind("Keyboard Shortcuts", "Show Female 1 Guide Object", new KeyboardShortcut(KeyCode.O), new ConfigDescription("Show the guide object for adjusting girl 1 position", null, new ConfigurationManagerAttributes { Order = 6 }));
            Female2GuideObject = Config.Bind("Keyboard Shortcuts", "Show Female 2 Guide Object", new KeyboardShortcut(KeyCode.P), new ConfigDescription("Show the guide object for adjusting girl 2 position", null, new ConfigurationManagerAttributes { Order = 5 }));
            MaleGuideObject = Config.Bind("Keyboard Shortcuts", "Show Male Guide Object", new KeyboardShortcut(KeyCode.I), new ConfigDescription("Show the guide object for adjusting the boy's position", null, new ConfigurationManagerAttributes { Order = 4 }));
            Female1GuideObjectReset = Config.Bind("Keyboard Shortcuts", "Reset Female 1 Position", new KeyboardShortcut(KeyCode.O, KeyCode.RightControl), new ConfigDescription("Reset adjustments for girl 1 position", null, new ConfigurationManagerAttributes { Order = 3 }));
            Female2GuideObjectReset = Config.Bind("Keyboard Shortcuts", "Reset Female 2 Position", new KeyboardShortcut(KeyCode.P, KeyCode.RightControl), new ConfigDescription("Reset adjustments for girl 2 position", null, new ConfigurationManagerAttributes { Order = 2 }));
            MaleGuideObjectReset = Config.Bind("Keyboard Shortcuts", "Reset Male Position", new KeyboardShortcut(KeyCode.I, KeyCode.RightControl), new ConfigDescription("Reset adjustments for girl 2 position", null, new ConfigurationManagerAttributes { Order = 1 }));

            Harmony.CreateAndPatchAll(typeof(Hooks));
            CharacterApi.RegisterExtraBehaviour<HCharaAdjustmentController>(GUID);
        }

        public static HCharaAdjustmentController GetController(ChaControl chaControl) => chaControl.GetComponent<HCharaAdjustmentController>();
    }
}