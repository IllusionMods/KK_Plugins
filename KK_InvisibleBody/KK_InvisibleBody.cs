using BepInEx;
using BepInEx.Logging;
using Logger = BepInEx.Logger;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;
using Harmony;
using Studio;
using ExtensibleSaveFormat;
/// <summary>
/// Sets the selected characters invisible in Studio. Invisible state saves and loads with the scene.
/// Also sets female characters invisible in H scenes.
/// </summary>
namespace KK_InvisibleBody
{
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInPlugin("com.deathweasel.bepinex.invisiblebody", "Invisible Body", "1.0")]
    public class KK_InvisibleBody : BaseUnityPlugin
    {
        private static bool LoadOrImportClicked = false;
        private static bool ChangeCharaVisibleState = true;
        [DisplayName("Invisibility Hotkey")]
        [Description("Invisibility hotkey.\n" +
                     "Toggles invisibility of the selected character in studio.\n" +
                     "Sets the female character invisible in H scenes.")]
        public static SavedKeyboardShortcut InvisibilityHotkey { get; private set; }
        [Category("Settings")]
        [DisplayName("Hide built-in hair accessories")]
        [Description("Whether or not to hide accesories (such as scrunchies) attached to back hairs.")]
        public static ConfigWrapper<bool> HideHairAccessories { get; private set; }

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.invisiblebody");
            harmony.PatchAll(typeof(KK_InvisibleBody));
            SceneManager.sceneLoaded += SceneLoaded;
            InvisibilityHotkey = new SavedKeyboardShortcut("InvisibilityHotkey", "KK_InvisibleBody", new KeyboardShortcut(KeyCode.KeypadPlus));
            HideHairAccessories = new ConfigWrapper<bool>("HideHairAccessories", "KK_InvisibleBody", true);
        }

        void Update()
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
                                SetVisibleState(ociChar.charInfo, true);
                            }
                        }
                    }
                }
                //In H scene set female characters invisible
                if (Singleton<Manager.Scene>.Instance.NowSceneNames.Any(sceneName => sceneName == "H"))
                {
                    List<ChaControl> CharaList = (from m in Resources.FindObjectsOfTypeAll<ChaControl>()
                                                  where m.sex == 1
                                                  select m).ToList();

                    foreach (ChaControl chara in CharaList)
                    {
                        //Set invisible
                        SetVisibleState(chara, forceVisible: true, forceVisibleState: false, saveVisibleState: false);
                    }
                }
            }
        }
        /// <summary>
        /// Sets the visibility state of a character. If no optional parameters are set the character's visiblity state will be read from the character file and set from that.
        /// </summary>
        /// <param name="chaControl">Character for which to set visible state.</param>
        /// <param name="toggleVisible">Toggles the character from visible to invisible and vice versa. Not used if forceVisible is set.</param>
        /// <param name="forceVisible">Forces the character to the state set in forceVisibleState. Overrides default visibility state and toggleVisible.</param>
        /// <param name="forceVisibleState">The visibility state to set a character. Only used if forceVisible is set.</param>
        /// <param name="saveVisibleState">Whether or not the visible state should be saved to the card.</param>
        private static void SetVisibleState(ChaControl chaControl, bool toggleVisible = false, bool forceVisible = false, bool forceVisibleState = false, bool saveVisibleState = true)
        {
            bool Visible;
            PluginData ExtendedData = ExtendedSave.GetExtendedDataById(chaControl.chaFile, "KK_InvisibleBody");
            GameObject CharacterObject = GameObject.Find(chaControl.name);

            if (ExtendedData == null)
            {
                Logger.Log(LogLevel.Debug, "No KK_InvisibleBody marker found");
                Visible = true;
                //character has no extended data, create some so it will save and load with the scene
                ExtendedData = new PluginData();
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add("Visible", Visible);
                ExtendedData.data = dic;
            }
            else
            {
                Logger.Log(LogLevel.Debug, $"KK_InvisibleBody marker found, Visible was {ExtendedData.data["Visible"]}");
                Visible = (bool)ExtendedData.data["Visible"];
            }

            if (forceVisible)
                Visible = forceVisibleState;
            else if (toggleVisible)
                Visible = !Visible;
            if (saveVisibleState)
            {
                ExtendedData.data["Visible"] = Visible;
                ExtendedSave.SetExtendedDataById(chaControl.chaFile, "KK_InvisibleBody", ExtendedData);
            }

            Transform cf_j_root = CharacterObject.transform.Find("BodyTop/p_cf_body_bone/cf_j_root");
            if (cf_j_root != null)
                IterateVisible(cf_j_root.gameObject, Visible);

            //female
            Transform cf_o_rootf = CharacterObject.transform.Find("BodyTop/p_cf_body_00/cf_o_root/");
            if (cf_o_rootf != null)
                IterateVisible(cf_o_rootf.gameObject, Visible);

            //male
            Transform cf_o_rootm = CharacterObject.transform.Find("BodyTop/p_cm_body_00/cf_o_root/");
            if (cf_o_rootm != null)
                IterateVisible(cf_o_rootm.gameObject, Visible);
        }
        /// <summary>
        /// Sets the visible state of the game object and all it's children.
        /// </summary>
        private static void IterateVisible(GameObject go, bool Visible)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                //Logger.Log(LogLevel.Info, $"Game Object:{DebugFullObjectPath(go)}");

                if (Visible)
                    IterateVisible(go.transform.GetChild(i).gameObject, Visible); //always set everything visible
                else if (HideHairAccessories.Value && go.name.StartsWith("a_n_") && go.transform.parent.gameObject.name == "ct_hairB")
                    IterateVisible(go.transform.GetChild(i).gameObject, Visible); //hide hair accessory
                else if (go.name.StartsWith("a_n_"))
                    Logger.Log(LogLevel.None, $"not hiding attached items for {go.name}"); //keep accessories and studio items visible
                else
                    IterateVisible(go.transform.GetChild(i).gameObject, Visible); //set everything else invisible
            }

            if (go.GetComponent<Renderer>())
                go.GetComponent<Renderer>().enabled = Visible;
        }
        /// <summary>
        /// Recursively finds the parents of a game object and builds a string of the full path. Only used for debug purposes.
        /// </summary>
        private static string DebugFullObjectPath(GameObject go)
        {
            if (go.transform.parent == null)
                return go.name;
            else
                return DebugFullObjectPath(go.transform.parent.gameObject) + "/" + go.name;
        }
        /// <summary>
        /// Scene has fully loaded and all the characters exist in game. Set the visiblity state of each character.
        /// Note that because this relies on the notification to determine whether the scene has loaded this will not work with Drag and Drop.
        /// Need to figure out a better solution for that.
        /// </summary>
        private void SceneLoaded(Scene s, LoadSceneMode lsm)
        {
            if (s.name == "StudioNotification" && LoadOrImportClicked)
            {
                LoadOrImportClicked = false;
                foreach (var chara in Resources.FindObjectsOfTypeAll<ChaControl>())
                    SetVisibleState(chara);
            }
        }
        /// <summary>
        /// Scene load clicked hook.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        public static void OnClickLoadPrefix()
        {
            LoadOrImportClicked = true;
        }
        /// <summary>
        /// Scene import clicked hook.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickImport")]
        public static void OnClickImportPrefix()
        {
            LoadOrImportClicked = true;
        }
        /// <summary>
        /// Change chara hook. Occurs when replacing a character in a scene. See if the character is invisible and set a flag to be used in the postfix.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ChangeChara))]
        public static void ChangeCharaPrefix(OCIChar __instance)
        {
            ChangeCharaVisibleState = true;
            PluginData ExtendedData = ExtendedSave.GetExtendedDataById(__instance.charInfo.chaFile, "KK_InvisibleBody");
            if (ExtendedData != null && (bool)ExtendedData.data["Visible"] == false)
            {
                //character is invisible, set it back to visible before the character is changed out
                ChangeCharaVisibleState = false;
                SetVisibleState(__instance.charInfo, forceVisible: true, forceVisibleState: true, saveVisibleState: false);
            }
        }
        /// <summary>
        /// Change chara postfix. Set the new character invisible if the old one was invisible.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ChangeChara))]
        public static void ChangeCharaPostfix(OCIChar __instance)
        {
            if (ChangeCharaVisibleState == false)
            {
                //character was invisible before the change, set it back to invisible
                SetVisibleState(__instance.charInfo, forceVisible: true, forceVisibleState: false);
                ChangeCharaVisibleState = true;
            }
        }
    }
}
