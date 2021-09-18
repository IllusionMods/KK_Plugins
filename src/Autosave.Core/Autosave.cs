﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if AI || HS2
using AIChara;
#endif
#if !HS && !PC && !SBPR
using KKAPI;
#endif
#if !EC && !PC && !SBPR
using Studio;
#endif

namespace KK_Plugins
{
    /// <summary>
    /// Autosave for Studio scenes and character maker cards
    /// </summary>
#if !HS && !PC && !SBPR
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
#endif
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Autosave : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.autosave";
        public const string PluginName = "Autosave";
        public const string PluginNameInternal = Constants.Prefix + "_Autosave";
        public const string PluginVersion = "1.1";
        internal static new ManualLogSource Logger;
        internal static Autosave Instance;

#if !EC && !PC && !SBPR
        public const string AutosavePathStudio = Studio.Studio.savePath + "/_autosave";
#endif
#if PC
        public const string AutosavePath = "chara/_autosave";
#else
        public const string AutosavePathMale = "chara/male/_autosave";
        public const string AutosavePathFemale = "chara/female/_autosave";
#endif
        private static GameObject AutosaveCanvas;
        private static Text AutosaveText;
        private static bool InStudio = false;
        public static bool Autosaving = false;

#if PC
        private static CharaCustomMode CharaCustomModeInstance;
#elif HS || SBPR
        private static CustomControl CustomControlInstance;
#endif
        private static Coroutine MakerCoroutine;
#if !EC && !PC && !SBPR && !KKS
        private static Coroutine StudioCoroutine;
#endif
        public static ConfigEntry<bool> AutosaveEnabled { get; private set; }
        public static ConfigEntry<int> AutosaveInterval { get; private set; }
        public static ConfigEntry<int> AutosaveCountdown { get; private set; }
        public static ConfigEntry<int> AutosaveFileLimit { get; private set; }

        private void Start()
        {
#if !EC && !PC && !SBPR && !KKS
            InStudio = Application.productName == Constants.StudioProcessName.Replace("64bit", "").Replace("_64", "");
#endif
            Logger = base.Logger;
            Instance = this;

            AutosaveEnabled = Config.Bind("Config", "Autosave Enabled", true, new ConfigDescription("Whether to do autosaves", null, new ConfigurationManagerAttributes { Order = 11 }));
            AutosaveInterval = Config.Bind("Config", "Autosave Interval", 15, new ConfigDescription("Minutes between autosaves", new AcceptableValueRange<int>(1, 60), new ConfigurationManagerAttributes { Order = 10 }));
            AutosaveCountdown = Config.Bind("Config", "Autosave Countdown", 10, new ConfigDescription("Seconds of countdown before autosaving", new AcceptableValueRange<int>(0, 60), new ConfigurationManagerAttributes { Order = 9 }));
            AutosaveFileLimit = Config.Bind("Config", "Autosave File Limit", 10, new ConfigDescription("Number of autosaves to keep, older ones will be deleted", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = 8, ShowRangeAsPercent = false }));

            Harmony.CreateAndPatchAll(typeof(Hooks));

            if (InStudio)
            {
#if !EC && !PC && !SBPR && !KKS
                StudioCoroutine = StartCoroutine(AutosaveStudio());
#endif
            }
            else
            {
#if !HS && !PC && !SBPR
                KKAPI.Maker.MakerAPI.MakerFinishedLoading += (a, b) => MakerCoroutine = StartCoroutine(AutosaveMaker());
                KKAPI.Maker.MakerAPI.MakerExiting += (a, b) => StopMakerCoroutine();
#endif
            }

            //Delete any leftover autosaves
            if (AutosaveFileLimit.Value == 0)
            {
#if PC
                DeleteAutosaves(AutosavePath);
#else
                DeleteAutosaves(AutosavePathMale);
                DeleteAutosaves(AutosavePathFemale);
#endif
            }

#if KK || EC || AI || HS2 || PH || KKS
            KKAPI.Chara.CharacterApi.RegisterExtraBehaviour<CharaController>(PluginGUID);
#endif
#if KK || AI || HS2 || PH
            KKAPI.Studio.SaveLoad.StudioSaveLoadApi.RegisterExtraBehaviour<StudioController>(PluginGUID);
#endif
        }

        private IEnumerator AutosaveMaker()
        {
            while (true)
            {
                yield return new WaitForSeconds(AutosaveInterval.Value * 60);
#if HS
                if (CustomControlInstance == null)
                {
                    StopMakerCoroutine();
                    break;
                }
#endif

                if (AutosaveFileLimit.Value != 0 && AutosaveEnabled.Value)
                {
                    //Display a counter before saving so that the user has a chance to stop moving the camera around
                    if (AutosaveCountdown.Value > 0)
                        for (int countdown = AutosaveCountdown.Value; countdown > 0; countdown--)
                        {
                            SetText($"Autosaving in {countdown}");
                            yield return new WaitForSeconds(1);
                        }

                    SetText("Saving...");
                    yield return new WaitForSeconds(1);

                    //Don't save if the user is in the middle of clicking and dragging
                    while (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
                        yield return new WaitForSeconds(1);

                    yield return new WaitForEndOfFrame();
                    Autosaving = true;
#if HS
                    if (CustomControlInstance == null)
                    {
                        StopMakerCoroutine();
                        break;
                    }
#endif
                    DateTime now = DateTime.Now;
                    string filename = $"autosave_{now.Year}_{now.Month:00}{now.Day:00}_{now.Hour:00}{now.Minute:00}_{now.Second:00}_{now.Millisecond:000}.png";
#if PC
                    string folder = AutosavePath;
#elif PH
                    string folder = KKAPI.Maker.MakerAPI.GetCharacterControl() is Male ? AutosavePathMale : AutosavePathFemale;
#elif HS
                    string folder = CustomControlInstance.chainfo.Sex == 0 ? AutosavePathMale : AutosavePathFemale;
#elif SBPR
                    string folder = CustomControlInstance.chabody.Sex == 0 ? AutosavePathMale : AutosavePathFemale;
#else
                    string folder = KKAPI.Maker.MakerAPI.GetCharacterControl().sex == 0 ? AutosavePathMale : AutosavePathFemale;
#endif
                    string filepath = $"{UserData.Create(folder)}{filename}";

#if KK || KKS
                    KKAPI.Maker.MakerAPI.GetCharacterControl().chaFile.SaveFile(filepath);
#elif PC
                    ((Human)Traverse.Create(CharaCustomModeInstance).Field("human").GetValue()).Custom.Save(filepath, null);
#elif PH
                    KKAPI.Maker.MakerAPI.GetCharacterControl().Save(filepath);
#elif HS
                    CustomControlInstance.CustomSaveCharaAssist(filepath);
#elif SBPR
                    CustomControlInstance.chabody.OverwriteCharaFile(filepath);
#else
                    KKAPI.Maker.MakerAPI.GetCharacterControl().chaFile.SaveFile(filepath, 0);
#endif

#if PC
                    DeleteAutosaves(AutosavePath);
#else
                    DeleteAutosaves(AutosavePathMale);
                    DeleteAutosaves(AutosavePathFemale);
#endif

                    SetText("Saved!");
                    Autosaving = false;
                    yield return new WaitForSeconds(2);
                    SetText("");
                }
            }
        }

#if !EC && !PC && !SBPR && !KKS
        private IEnumerator AutosaveStudio()
        {
            while (true)
            {
                yield return new WaitForSeconds(AutosaveInterval.Value * 60);

                //Studio not loaded yet
                if (!Studio.Studio.IsInstance())
                    continue;

                if (AutosaveFileLimit.Value != 0 && AutosaveEnabled.Value)
                {
                    //Display a counter before saving so that the user has a chance to stop moving the camera around
                    if (AutosaveCountdown.Value > 0)
                        for (int countdown = AutosaveCountdown.Value; countdown > 0; countdown--)
                        {
                            SetText($"Autosaving in {countdown}");
                            yield return new WaitForSeconds(1);
                        }

                    SetText("Saving...");
                    yield return new WaitForSeconds(1);

                    //Don't save if the user is in the middle of clicking and dragging
                    while (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
                        yield return new WaitForSeconds(1);

                    //Needed so the thumbnail is correct
                    yield return new WaitForEndOfFrame();
                    Autosaving = true;

                    //Game runs similar code
                    foreach (KeyValuePair<int, ObjectCtrlInfo> item in Studio.Studio.Instance.dicObjectCtrl)
                        item.Value.OnSavePreprocessing();
                    Studio.Studio.Instance.sceneInfo.cameraSaveData = Studio.Studio.Instance.cameraCtrl.Export();
                    DateTime now = DateTime.Now;
                    string str = $"autosave_{now.Year}_{now.Month:00}{now.Day:00}_{now.Hour:00}{now.Minute:00}_{now.Second:00}_{now.Millisecond:000}.png";
                    string path = $"{UserData.Create(AutosavePathStudio)}{str}";
                    Studio.Studio.Instance.sceneInfo.Save(path);

                    DeleteAutosaves(AutosavePathStudio);

                    SetText("Saved!");
                    Autosaving = false;
                    yield return new WaitForSeconds(2);
                    SetText("");
                }
            }
        }
#endif

        /// <summary>
        /// Delete all but the latest few autosaves
        /// </summary>
        /// <param name="folder">Autosave folder</param>
        private void DeleteAutosaves(string folder)
        {
            DirectoryInfo di = new DirectoryInfo(UserData.Create(folder));
            var files = di.GetFiles("autosave*.png").ToList();
            files.OrderBy(x => x.CreationTime);
            while (files.Count > AutosaveFileLimit.Value)
            {
                var fileToDelete = files[0];
                string filenameToDelete = files[0].Name;
                files.RemoveAt(0);
                fileToDelete.Delete();

#if PH
                //Remove any extra files starting with the same name (PH ext save data)
                var extraFiles = di.GetFiles(filenameToDelete + "*").ToList();
                foreach (var extraFile in extraFiles)
                    extraFile.Delete();
#endif
            }
        }

        private static void StopMakerCoroutine()
        {
            SetText("");
            Instance.StopCoroutine(MakerCoroutine);
        }

        /// <summary>
        /// Reset the coroutine and restart the autosave timer
        /// </summary>
        public static void ResetMakerCoroutine()
        {
            SetText("");
            Instance.StopCoroutine(MakerCoroutine);
            MakerCoroutine = Instance.StartCoroutine(Instance.AutosaveMaker());
        }

#if !EC && !PC && !SBPR && !KKS
        /// <summary>
        /// Reset the coroutine and restart the autosave timer
        /// </summary>
        public static void ResetStudioCoroutine()
        {
            SetText("");
            Instance.StopCoroutine(StudioCoroutine);
            StudioCoroutine = Instance.StartCoroutine(Instance.AutosaveStudio());
        }
#endif

        private static void InitGUI()
        {
            if (AutosaveCanvas != null)
                return;
            var align = InStudio ? TextAnchor.MiddleLeft : TextAnchor.UpperCenter;

            AutosaveCanvas = new GameObject("AutosaveCanvas");

            var cscl = AutosaveCanvas.GetOrAddComponent<CanvasScaler>();
            cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cscl.referenceResolution = new Vector2(Screen.width, Screen.height);

            var canvas = AutosaveCanvas.GetOrAddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            AutosaveCanvas.GetOrAddComponent<CanvasGroup>().blocksRaycasts = false;

            var vlg = AutosaveCanvas.GetOrAddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childAlignment = align;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            int fsize = 16;

            GameObject autosaveTextObject = new GameObject("AutosaveText");
            autosaveTextObject.transform.SetParent(AutosaveCanvas.transform);

            var rect = autosaveTextObject.GetOrAddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(Screen.width * 0.990f, fsize + (fsize * 0.05f));

            AutosaveText = autosaveTextObject.GetOrAddComponent<Text>();
            AutosaveText.font = fontFace;
            AutosaveText.fontSize = fsize;
            AutosaveText.fontStyle = UnityEngine.FontStyle.Normal;
            AutosaveText.alignment = align;
            AutosaveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            AutosaveText.verticalOverflow = VerticalWrapMode.Overflow;
            AutosaveText.color = Color.red;

            var autosaveTextOutline = autosaveTextObject.GetOrAddComponent<Outline>();
            autosaveTextOutline.effectColor = Color.black;
            autosaveTextOutline.effectDistance = new Vector2(1, 1);
        }

        private static void SetText(string text)
        {
            InitGUI();
            AutosaveText.text = text;
        }

        private static class Hooks
        {
#if PC
            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustomMode), nameof(CharaCustomMode.Start))]
            private static void CharaCustomMode_Start(CharaCustomMode __instance)
            {
                CharaCustomModeInstance = __instance;
                MakerCoroutine = Instance.StartCoroutine(Instance.AutosaveMaker());
            }

            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustomMode), nameof(CharaCustomMode.End))]
            private static void CharaCustomMode_End() => StopMakerCoroutine();
#elif HS

            [HarmonyPostfix, HarmonyPatch(typeof(CustomControl), "Start")]
            private static void CustomControl_Start(CustomControl __instance)
            {
                CustomControlInstance = __instance;
                MakerCoroutine = Instance.StartCoroutine(Instance.AutosaveMaker());
            }
#elif SBPR
            [HarmonyPostfix, HarmonyPatch(typeof(CustomScene), "Start")]
            private static void CustomScene_Start(CustomScene __instance)
            {
                CustomControlInstance = __instance.customControl;
                MakerCoroutine = Instance.StartCoroutine(Instance.AutosaveMaker());
            }
#endif
        }
    }
}