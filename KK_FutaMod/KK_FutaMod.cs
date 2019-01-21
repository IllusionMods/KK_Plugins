using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using ExtensibleSaveFormat;
using Harmony;
using MakerAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UniRx;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_FutaMod
{
    /// <summary>
    /// Futa mod. Adds dicks to girls which save and load along with the card.
    /// </summary>
    [BepInProcess("Koikatu")] //Not for Studio since you can add dicks whenever you want there
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_FutaMod : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.futamod";
        public const string PluginName = "Futa Mod";
        public const string PluginNameInternal = "KK_FutaMod";
        public const string Version = "0.3";
        private static bool ListOverride = false;
        private static bool DoingLoadFileLimited = false;
        private MakerToggle FutaToggle;
        [DisplayName("Enabled")]
        [Description("Can be used to disable the plugin without needing to uninstall it. Prevents futas from appearing outside the chara maker.")]
        public static ConfigWrapper<bool> Enabled { get; private set; }

        void Main()
        {
            Enabled = new ConfigWrapper<bool>("Enabled", PluginNameInternal, true);
            var harmony = HarmonyInstance.Create("com.deathweasel.bepinex.futamod");
            harmony.PatchAll(typeof(KK_FutaMod));
            ExtendedSave.CardBeingLoaded += ExtendedCardLoad;
            ExtendedSave.CardBeingSaved += ExtendedCardSave;
            MakerAPI.MakerAPI.Instance.RegisterCustomSubCategories += AddCategory;
        }
        /// <summary>
        /// Register the futa checkbox with MakerAPI
        /// </summary>
        private void AddCategory(object sender, RegisterSubCategoriesEvent args)
        {
            if (Singleton<CustomBase>.Instance.modeSex == 0)
                return;

            FutaToggle = args.AddControl(new MakerToggle(MakerConstants.Body.All, "ふたなり", this));
            void ToggleFuta(bool IsFuta)
            {
                Singleton<CustomBase>.Instance.chaCtrl.chaFile.status.visibleSonAlways = IsFuta;
                PluginData ExtendedData = new PluginData();
                ExtendedData.data = new Dictionary<string, object> { { "Futa", IsFuta } };
                ExtendedSave.SetExtendedDataById(Singleton<CustomBase>.Instance.chaCtrl.chaFile, "KK_FutaMod", ExtendedData);
            }
            var obs = Observer.Create<bool>(ToggleFuta);
            FutaToggle.ValueChanged.Subscribe(obs);
            FutaToggle.Value = Singleton<CustomBase>.Instance.chaCtrl.chaFile.status.visibleSonAlways;
        }
        /// <summary>
        /// Card loading
        /// </summary>
        private void ExtendedCardLoad(ChaFile file)
        {
            if (ListOverride)
                return;

            bool IsFuta = false;
            PluginData ExtendedData = ExtendedSave.GetExtendedDataById(file, "KK_FutaMod");

            if (ExtendedData != null && ExtendedData.data.ContainsKey("Futa"))
            {
                IsFuta = (bool)ExtendedData.data["Futa"];
                file.status.visibleSonAlways = IsFuta;
            }

            //Loading a card while in chara maker
            if (Singleton<CustomBase>.IsInstance() && Singleton<CustomBase>.Instance.modeSex == 1 && Singleton<CustomBase>.Instance.chaCtrl != null && DoingLoadFileLimited)
                FutaToggle.Value = IsFuta;
        }
        /// <summary>
        /// Card saving
        /// </summary>
        private void ExtendedCardSave(ChaFile file)
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
            if (_sex == 0 || _chaFile == null)
                return;

            PluginData ExtendedData = ExtendedSave.GetExtendedDataById(_chaFile, "KK_FutaMod");

            if (ExtendedData != null && ExtendedData.data.ContainsKey("Futa") && Enabled.Value)
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
    }
}