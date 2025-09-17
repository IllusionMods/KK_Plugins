#if !HS && !PC && !SBPR
#define HasKKAPI
#endif
#if !EC && !PC && !SBPR
#define HasStudio
#endif

using BepInEx;
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
#if HasKKAPI
using KKAPI;
using KKAPI.Maker;
using ExtensibleSaveFormat;
#endif
#if PC || SBPR
// Too old Unity version, fall back to WaitForSeconds since it doesn't cause any major issues in these games
using WaitForSecondsRealtime = UnityEngine.WaitForSeconds;
#endif

namespace KK_Plugins
{
    /// <summary>
    /// Autosave for Studio scenes and character maker cards
    /// </summary>
#if HasKKAPI
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
#endif
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Autosave : BaseUnityPlugin
    {
        public const string PluginGUID = "com.deathweasel.bepinex.autosave";
        public const string PluginName = "Autosave";
        public const string PluginNameInternal = Constants.Prefix + "_Autosave";
        public const string PluginVersion = "2.0";

        internal static new ManualLogSource Logger;
        internal static Autosave Instance;

#if HasStudio
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
        private static CharaCustomMode CustomControlInstance;
#elif HS || SBPR
        private static CustomControl CustomControlInstance;
#endif
        private static Coroutine _autosaveCoroutine;
        public static ConfigEntry<bool> AutosaveEnabled { get; private set; }
        public static ConfigEntry<int> AutosaveInterval { get; private set; }
        public static ConfigEntry<int> AutosaveCountdown { get; private set; }
        public static ConfigEntry<bool> PauseInBackground { get; private set; }
        public static ConfigEntry<int> AutosaveFileLimit { get; private set; }

        private void Start()
        {
            Logger = base.Logger;
            Instance = this;

            AutosaveEnabled = Config.Bind("Config", "Autosave Enabled", true, new ConfigDescription("Whether to do autosaves", null, new ConfigurationManagerAttributes { Order = 11 }));
            AutosaveInterval = Config.Bind("Config", "Autosave Interval", 15, new ConfigDescription("Minutes between autosaves", new AcceptableValueRange<int>(1, 60), new ConfigurationManagerAttributes { Order = 10 }));
            AutosaveCountdown = Config.Bind("Config", "Autosave Countdown", 10, new ConfigDescription("Seconds of countdown before autosaving", new AcceptableValueRange<int>(0, 60), new ConfigurationManagerAttributes { Order = 9 }));
            PauseInBackground = Config.Bind("Config", "Pause In Background", true, new ConfigDescription("Do not count down when the game is not focused", null, new ConfigurationManagerAttributes { Order = 8 }));
            AutosaveFileLimit = Config.Bind("Config", "Autosave File Limit", 10, new ConfigDescription("Number of autosaves to keep, older ones will be deleted", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = 7, ShowRangeAsPercent = false }));

#if PC || HS || SBPR
            Hooks.ApplyHooks(PluginGUID);
#endif

#if HasStudio
            InStudio = Application.productName == Constants.StudioProcessName.Replace("64bit", "").Replace("_64", "");
            if (InStudio) StartAutosaveCoroutine();
#endif

            //Delete any leftover autosaves
            if (AutosaveFileLimit.Value == 0)
                DeleteAutosaves();

#if HasKKAPI
            if (!InStudio)
            {
                KKAPI.Maker.MakerAPI.MakerFinishedLoading += (a, b) => StartAutosaveCoroutine();
                KKAPI.Maker.MakerAPI.MakerExiting += (a, b) => StopAutosaveCoroutine();

                ExtendedSave.CardBeingLoaded += CharaSaveLoadHandler;
                ExtendedSave.CardBeingSaved += CharaSaveLoadHandler;
#if PH
                void CharaSaveLoadHandler(Character.CustomParameter file)
#else
                void CharaSaveLoadHandler(ChaFile file)
#endif
                {
                    if (MakerAPI.InsideAndLoaded && !Autosaving)
                    {
                        StopAutosaveCoroutine();
                        StartAutosaveCoroutine();
                    }
                }
            }
#if HasStudio
            else
            {
                ExtendedSave.SceneBeingSaved += SceneSaveLoadHandler;
                ExtendedSave.SceneBeingImported += SceneSaveLoadHandler;
                ExtendedSave.SceneBeingLoaded += SceneSaveLoadHandler;
                void SceneSaveLoadHandler(string path)
                {
                    if (!MakerAPI.InsideAndLoaded && !Autosaving)
                    {
                        StopAutosaveCoroutine();
                        StartAutosaveCoroutine();
                    }
                }
            }
#endif
#endif
        }

        private bool _hasFocus = true;
        private float _startTime;
        private float _unfocusTime;
        private void OnApplicationFocus(bool hasFocus)
        {
            if (_hasFocus == hasFocus) return;
            _hasFocus = hasFocus;

            if (PauseInBackground.Value)
            {
                if (hasFocus)
                {
                    // Once focus is regained, adjust the start time to account for time spent unfocused
                    var timePaused = Time.realtimeSinceStartup - _unfocusTime;
                    _startTime += timePaused;

                    // Ensure at least 10 seconds remain on the timer when focus is regained
                    if (Time.realtimeSinceStartup - _startTime > AutosaveInterval.Value * 60f - 10)
                        _startTime = Time.realtimeSinceStartup - (AutosaveInterval.Value * 60f - 10);
                }
                else
                {
                    _unfocusTime = Time.realtimeSinceStartup;
                }
            }
            //Console.WriteLine($"FOCUS: {_hasFocus} -> {hasFocus}   starttime: {old} -> {_startTime}");
        }

        private IEnumerator AutosaveCoroutine()
        {
#if HasStudio
            if (InStudio)
            {
                //Studio not loaded yet
                while (!Studio.Studio.IsInstance())
                    yield return null;
            }
#endif
            while (true)
            {
                while (!Input.anyKey)
                    yield return null;

                _startTime = Time.realtimeSinceStartup;
                // Stop the countdown if user alt-tabs out of the game
                while (!_hasFocus || Time.realtimeSinceStartup - _startTime < AutosaveInterval.Value * 60f)
                    yield return null;

                if (!InStudio && !MakerIsAlive())
                {
                    StopAutosaveCoroutine();
                    break;
                }

                if (AutosaveFileLimit.Value == 0 || !AutosaveEnabled.Value)
                    continue;

                //Display a counter before saving so that the user has a chance to stop moving the camera around
                if (AutosaveCountdown.Value > 0)
                {
                    for (int countdown = AutosaveCountdown.Value; countdown > 0; countdown--)
                    {
                        SetText($"Autosaving in {countdown}");
                        yield return new WaitForSecondsRealtime(1);
                    }
                }

                SetText("Saving...");

                //Don't save if the user is in the middle of clicking and dragging
                do yield return new WaitForSecondsRealtime(1);
                while (Input.anyKey);

                //Needed so the thumbnail is correct
                yield return new WaitForEndOfFrame();

                if (!InStudio &&!MakerIsAlive())
                {
                    StopAutosaveCoroutine();
                    break;
                }

                Autosaving = true;
                MakeSave();
                DeleteAutosaves();
                SetText("Saved!");
                Autosaving = false;

                yield return new WaitForSecondsRealtime(2);
                SetText("");
            }
        }

        private static bool MakerIsAlive()
        {
#if PC || HS || SBPR
            if (CustomControlInstance == null) return false;
#elif HasKKAPI
            if (!MakerAPI.InsideMaker) return false;
#endif
            return true;
        }

        private static void MakeSave()
        {
#if HasStudio
            if (InStudio)
            {
                //Game runs similar code
                foreach (KeyValuePair<int, Studio.ObjectCtrlInfo> item in Studio.Studio.Instance.dicObjectCtrl)
                    item.Value.OnSavePreprocessing();
                Studio.Studio.Instance.sceneInfo.cameraSaveData = Studio.Studio.Instance.cameraCtrl.Export();
                DateTime now = DateTime.Now;
                string str = $"autosave_{now.Year}_{now.Month:00}{now.Day:00}_{now.Hour:00}{now.Minute:00}_{now.Second:00}_{now.Millisecond:000}.png";
                string path = $"{UserData.Create(AutosavePathStudio)}{str}";

                Studio.Studio.Instance.sceneInfo.Save(path);
            }
            else
#endif
            {
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
                ((Human)Traverse.Create(CustomControlInstance).Field("human").GetValue()).Custom.Save(filepath, null);
#elif PH
                KKAPI.Maker.MakerAPI.GetCharacterControl().Save(filepath);
#elif HS
                CustomControlInstance.CustomSaveCharaAssist(filepath);
#elif SBPR
                CustomControlInstance.chabody.OverwriteCharaFile(filepath);
#else
                KKAPI.Maker.MakerAPI.GetCharacterControl().chaFile.SaveFile(filepath, 0);
#endif
            }
        }
        private void DeleteAutosaves()
        {
#if HasStudio
            if (InStudio)
            {
                DeleteAutosaves(AutosavePathStudio);
            }
            else
#endif
            {
#if PC
                DeleteAutosaves(AutosavePath);
#else
                DeleteAutosaves(AutosavePathMale);
                DeleteAutosaves(AutosavePathFemale);
#endif
            }
        }

        /// <summary>
        /// Delete all but the latest few autosaves
        /// </summary>
        /// <param name="folder">Autosave folder</param>
        private void DeleteAutosaves(string folder)
        {
            DirectoryInfo di = new DirectoryInfo(UserData.Create(folder));
            var files = di.GetFiles("autosave*.png").ToList();
            files.Sort((x, y) => x.CreationTimeUtc.CompareTo(y.CreationTimeUtc));
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

        private static void StopAutosaveCoroutine()
        {
            SetText("");
            Instance.StopCoroutine(_autosaveCoroutine);
        }

        private static void StartAutosaveCoroutine()
        {
            _autosaveCoroutine = Instance.StartCoroutine(Instance.AutosaveCoroutine());
        }

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

#if PC || HS || SBPR
        private static class Hooks
        {
            public static void ApplyHooks(string guid) => Harmony.CreateAndPatchAll(typeof(Hooks), guid);
#if PC
            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustomMode), nameof(CharaCustomMode.Start))]
            private static void CharaCustomMode_Start(CharaCustomMode __instance)
            {
                CustomControlInstance = __instance;
                StartAutosaveCoroutine();
            }

            [HarmonyPostfix, HarmonyPatch(typeof(CharaCustomMode), nameof(CharaCustomMode.End))]
            private static void CharaCustomMode_End() => StopAutosaveCoroutine();
#elif HS
            [HarmonyPostfix, HarmonyPatch(typeof(CustomControl), "Start")]
            private static void CustomControl_Start(CustomControl __instance)
            {
                CustomControlInstance = __instance;
                StartAutosaveCoroutine();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CustomControl), nameof(CustomControl.EndCustomScene))]
            private static void CustomControl_End(CustomScene __instance) => StopAutosaveCoroutine();
#elif SBPR
            [HarmonyPostfix, HarmonyPatch(typeof(CustomScene), nameof(CustomScene.Start))]
            private static void CustomScene_Start(CustomScene __instance)
            {
                CustomControlInstance = __instance.customControl;
                StartAutosaveCoroutine();
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CustomScene), nameof(CustomScene.Destroy))]
            private static void CustomScene_End(CustomScene __instance) => StopAutosaveCoroutine();
#endif
        }
#endif
    }
}
