using BepInEx;
using BepInEx.Logging;
using Harmony;
using Studio;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logger;
/// <summary>
/// Sets the selected characters invisible in Studio. Invisible state saves and loads with the scene.
/// Also sets female characters invisible in H scenes.
/// </summary>
namespace InvisibleBody
{
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class InvisibleBody : BaseUnityPlugin
    {
        [DisplayName("Invisibility Hotkey")]
        [Description("Invisibility hotkey.\n" +
             "Toggles invisibility of the selected character in studio.\n" +
             "Sets the female character invisible in H scenes.")]
        public static SavedKeyboardShortcut InvisibilityHotkey { get; private set; }
        [Category("Settings")]
        [DisplayName("Hide built-in hair accessories")]
        [Description("Whether or not to hide accesories (such as scrunchies) attached to back hairs.")]
        public static ConfigWrapper<bool> HideHairAccessories { get; private set; }

        private void Start()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(InvisibleBody));

            InvisibilityHotkey = new SavedKeyboardShortcut("InvisibilityHotkey", PluginNameInternal, new KeyboardShortcut(KeyCode.KeypadPlus));
            HideHairAccessories = new ConfigWrapper<bool>("HideHairAccessories", PluginNameInternal, true);
        }

        private void Update()
        {
            if (InvisibilityHotkey.IsDown())
            {
                //In studio, toggle visibility state of any characters selected in the workspace
                if (Singleton<Manager.Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "Studio"))
                {
                    TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
                    for (int i = 0; i < selectNodes.Length; i++)
                    {
                        if (Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                        {
                            if (objectCtrlInfo is OCIChar ociChar) //selected item is a character
                            {
                                var controller = GetController(ociChar.charInfo);
                                controller.Visible = !controller.Visible;
                            }
                        }
                    }
                }
            }
        }

        public static void Log(LogLevel level, object text) => Logger.Log(level, text);
        public static void Log(object text) => Logger.Log(LogLevel.Info, text);
    }
}
