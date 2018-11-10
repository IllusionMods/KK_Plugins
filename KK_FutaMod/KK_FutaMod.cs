using BepInEx;
using BepInEx.Logging;
using Logger = BepInEx.Logger;
using Harmony;
using UnityEngine;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using ExtensibleSaveFormat;
using ChaCustom;
/// <summary>
/// Futa mod. Adds dicks to girls which save and load along with the card.
/// </summary>
namespace KK_FutaMod
{
    [BepInProcess("Koikatu")] //Not for Studio since you can add dicks whenever you want there
    [BepInPlugin("com.deathweasel.bepinex.futamod", "Futa Mod", "0.1")]
    public class KK_FutaMod : BaseUnityPlugin
    {
        [DisplayName("Futa Hotkey")]
        [Description("Futa hotkey")]
        public static SavedKeyboardShortcut FutaHotkey { get; private set; }
        private static bool ListOverride = false;
        private static bool DoingLoadFileLimited = false;

        void Main()
        {
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.futamod");
            harmony.PatchAll(typeof(KK_FutaMod));
            FutaHotkey = new SavedKeyboardShortcut("FutaHotkey", "KK_FutaMod", new KeyboardShortcut(KeyCode.KeypadMinus));
            ExtendedSave.CardBeingLoaded += ExtendedCardLoad;
            ExtendedSave.CardBeingSaved += ExtendedCardSave;
        }
        /// <summary>
        /// Replace this with a GUI
        /// </summary>
        void Update()
        {
            if (FutaHotkey.IsDown() && Singleton<CustomBase>.IsInstance() && Singleton<CustomBase>.Instance.chaCtrl != null)
            {
                bool IsFuta = !Singleton<CustomBase>.Instance.chaCtrl.chaFile.status.visibleSonAlways;
                Singleton<CustomBase>.Instance.chaCtrl.chaFile.status.visibleSonAlways = IsFuta;
                PluginData ExtendedData = new PluginData();
                ExtendedData.data = new Dictionary<string, object> { { "Futa", IsFuta } };
                ExtendedSave.SetExtendedDataById(Singleton<CustomBase>.Instance.chaCtrl.chaFile, "KK_FutaMod", ExtendedData);
            }
        }
        /// <summary>
        /// Card loading
        /// </summary>
        private static void ExtendedCardLoad(ChaFile file)
        {
            if (ListOverride) return;

            bool IsFuta = false;
            PluginData ExtendedData = ExtendedSave.GetExtendedDataById(file, "KK_FutaMod");

            if (ExtendedData != null && ExtendedData.data.ContainsKey("Futa"))
            {
                IsFuta = (bool)ExtendedData.data["Futa"];
                file.status.visibleSonAlways = IsFuta;
            }

            //Loading a card while in chara maker
            if (Singleton<CustomBase>.IsInstance() && Singleton<CustomBase>.Instance.chaCtrl != null && DoingLoadFileLimited)
            {
                ExtendedData = new PluginData();
                ExtendedData.data = new Dictionary<string, object> { { "Futa", IsFuta } };
                ExtendedSave.SetExtendedDataById(Singleton<CustomBase>.Instance.chaCtrl.chaFile, "KK_FutaMod", ExtendedData);
                Singleton<CustomBase>.Instance.chaCtrl.chaFile.status.visibleSonAlways = IsFuta;
            }
        }
        /// <summary>
        /// Card saving
        /// </summary>
        private static void ExtendedCardSave(ChaFile file)
        {
            PluginData ExtendedData = ExtendedSave.GetExtendedDataById(file, "KK_FutaMod");

            if (ExtendedData != null && ExtendedData.data.ContainsKey("Futa"))
            {
                if (Singleton<CustomBase>.IsInstance() && Singleton<CustomBase>.Instance.chaCtrl != null)
                {
                    //Saving card from chara maker, get the status from the character
                    ExtendedData.data["Futa"] = file.status.visibleSonAlways;
                    ExtendedSave.SetExtendedDataById(file, "KK_FutaMod", ExtendedData);
                }
                else
                {
                    //Not in chara maker, keep the existing extended data
                    ExtendedSave.SetExtendedDataById(file, "KK_FutaMod", ExtendedData);
                }
            }
            else
            {
                if (Singleton<CustomBase>.IsInstance() && Singleton<CustomBase>.Instance.chaCtrl != null)
                {
                    //Saving a character in chara maker that doesn't have extended data
                    ExtendedData = new PluginData();
                    ExtendedData.data = new Dictionary<string, object> { { "Futa", file.status.visibleSonAlways } };
                    ExtendedSave.SetExtendedDataById(file, "KK_FutaMod", ExtendedData);
                }
            }
        }
        /// <summary>
        /// When one ChaFile is copied to another, copy over the extended data too
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), nameof(ChaFile.CopyChaFile))]
        public static void CopyChaFile(ChaFile dst, ChaFile src)
        {
            PluginData ExtendedData = ExtendedSave.GetExtendedDataById(src, "KK_FutaMod");

            if (ExtendedData != null && ExtendedData.data.ContainsKey("Futa"))
                ExtendedSave.SetExtendedDataById(dst, "KK_FutaMod", ExtendedData);
        }
        /// <summary>
        /// When a female is created enable the dick
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Character), nameof(Manager.Character.CreateChara))]
        public static void CreateChara(ChaControl __result, ChaFileControl _chaFile, byte _sex)
        {
            if (_sex == 0 || _chaFile == null) return;

            PluginData ExtendedData = ExtendedSave.GetExtendedDataById(_chaFile, "KK_FutaMod");

            if (ExtendedData != null && ExtendedData.data.ContainsKey("Futa"))
                __result.chaFile.status.visibleSonAlways = (bool)ExtendedData.data["Futa"];
        }

        //Allow changing futa state in chara maker only when LoadFileLimited has been called
        [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFileLimited), new[] { typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        public static void LoadFileLimitedPrefix() => DoingLoadFileLimited = true;
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFileLimited), new[] { typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) })]
        public static void LoadFileLimitedPostfix() => DoingLoadFileLimited = false;

        //Prevent changing futa state when loading the list of characters
        [HarmonyPrefix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static void CustomScenePrefix() => ListOverride = true;
        [HarmonyPostfix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static void CustomScenePostfix() => ListOverride = false;

        ///// <summary>
        ///// Normal asset loading. Replace the male body name with the female one.
        ///// </summary>
        //[HarmonyPrefix]
        //[HarmonyBefore(new string[] { "com.bepis.bepinex.resourceredirector" })]
        //[HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), new[] { typeof(string), typeof(string), typeof(Type), typeof(string) })]
        //public static void LoadAssetPrefix(ref string assetName)
        //{
        //    if (assetName == "p_cm_body_00_low")
        //        assetName = "p_cf_body_00_low";
        //    else if (assetName == "p_cm_body_00")
        //        assetName = "p_cf_body_00";
        //}
        ///// <summary>
        ///// Async asset loading. Probably only used in the intro sequence.
        ///// </summary>
        //[HarmonyPrefix]
        //[HarmonyBefore(new string[] { "com.bepis.bepinex.resourceredirector" })]
        //[HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetAsync), new[] { typeof(string), typeof(string), typeof(Type), typeof(string) })]
        //public static void LoadAssetAsyncPrefix(ref string assetName)
        //{
        //    if (assetName == "p_cm_body_00_low")
        //        assetName = "p_cf_body_00_low";
        //    else if (assetName == "p_cm_body_00")
        //        assetName = "p_cf_body_00";
        //}
    }
}