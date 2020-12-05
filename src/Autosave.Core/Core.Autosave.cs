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
#elif PH
using ChaControl = Human;
#endif
#if !HS
using KKAPI;
#endif
#if !EC
using Studio;
#endif

namespace KK_Plugins
{
    /// <summary>
    /// Autosave for Studio scenes and character maker cards
    /// </summary>
#if !HS
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
#endif
    [BepInPlugin(GUID, PluginName, Version)]
    public class Autosave : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.autosave";
        public const string PluginName = "Autosave";
        public const string PluginNameInternal = Constants.Prefix + "_Autosave";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

#if !EC
        public const string AutosavePathStudio = Studio.Studio.savePath + "/_autosave";
#endif
        public const string AutosavePathMale = "chara/male/_autosave";
        public const string AutosavePathFemale = "chara/female/_autosave";
        private static GameObject AutosaveCanvas;
        private static Text AutosaveText;
#if EC
        private static readonly bool InStudio = false;
#else
        private static readonly bool InStudio = Application.productName == Constants.StudioProcessName.Replace("64bit", "").Replace("_64", "");
#endif
#if !HS
        private Coroutine MakerCoroutine;
#endif

        public static ConfigEntry<int> AutosaveIntervalStudio { get; private set; }
        public static ConfigEntry<int> AutosaveIntervalMaker { get; private set; }
        public static ConfigEntry<int> AutosaveCountdown { get; private set; }
        public static ConfigEntry<int> AutosaveFileLimit { get; private set; }

        private void Start()
        {
            Logger = base.Logger;

            AutosaveIntervalStudio = Config.Bind("Config", "Autosave Interval Studio", 10, new ConfigDescription("Minutes between autosaves in Studio", new AcceptableValueRange<int>(1, 60), new ConfigurationManagerAttributes { Order = 10 }));
            AutosaveIntervalMaker = Config.Bind("Config", "Autosave Interval Maker", 5, new ConfigDescription("Minutes between autosaves in the character maker", new AcceptableValueRange<int>(1, 60), new ConfigurationManagerAttributes { Order = 10 }));
            AutosaveCountdown = Config.Bind("Config", "Autosave Countdown", 10, new ConfigDescription("Seconds of countdown before autosaving", new AcceptableValueRange<int>(0, 60), new ConfigurationManagerAttributes { Order = 9 }));
            AutosaveFileLimit = Config.Bind("Config", "Autosave File Limit", 10, new ConfigDescription("Number of autosaves to keep, older ones will be deleted", new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = 8, ShowRangeAsPercent = false }));

            if (InStudio)
            {
#if !EC
                StartCoroutine(AutosaveStudio());
#endif
            }
            else
            {
#if !HS
                KKAPI.Maker.MakerAPI.MakerFinishedLoading += (a, b) => MakerCoroutine = StartCoroutine(AutosaveMaker());
                KKAPI.Maker.MakerAPI.MakerExiting += (a, b) => StopCoroutine(MakerCoroutine);
#endif
            }

            //Delete any leftover autosaves
            if (AutosaveFileLimit.Value == 0)
            {
#if !EC
                DeleteAutosaves(AutosavePathStudio);
#endif
                DeleteAutosaves(AutosavePathMale);
                DeleteAutosaves(AutosavePathFemale);
            }
        }

#if !HS
        private IEnumerator AutosaveMaker()
        {
            while (true)
            {
                yield return new WaitForSeconds(AutosaveIntervalMaker.Value * 60);

                if (AutosaveFileLimit.Value != 0)
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
                    yield return new WaitForEndOfFrame();

                    var charas = FindObjectsOfType<ChaControl>();
                    for (int counter = 0; counter < charas.Length; counter++)
                    {
                        DateTime now = DateTime.Now;
                        string filename = $"autosave_{now.Year}_{now.Month:00}{now.Day:00}_{now.Hour:00}{now.Minute:00}_{now.Second:00}_{now.Millisecond:000}.png";
#if PH
                        string folder = charas[counter] is Male ? AutosavePathMale : AutosavePathFemale;
#else
                        string folder = charas[counter].sex == 0 ? AutosavePathMale : AutosavePathFemale;
#endif
                        string filepath = $"{UserData.Create(folder)}{filename}";
#if AI || EC || HS2
                        Traverse.Create(charas[counter].chaFile).Method("SaveFile", filepath, 0).GetValue();
#elif KK
                        Traverse.Create(charas[counter].chaFile).Method("SaveFile", filepath).GetValue();
#elif PH
                        charas[counter].Save(filepath);
#endif
                    }

                    DeleteAutosaves(AutosavePathMale);
                    DeleteAutosaves(AutosavePathFemale);

                    SetText("Saved!");
                    yield return new WaitForSeconds(2);
                    SetText("");
                }
            }
        }
#endif

#if !EC
        private IEnumerator AutosaveStudio()
        {
            while (true)
            {
                yield return new WaitForSeconds(AutosaveIntervalStudio.Value * 60);

                //Studio not loaded yet
                if (!Studio.Studio.IsInstance())
                    continue;

                if (AutosaveFileLimit.Value != 0)
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

                    //Needed so the thumbnail is correct
                    yield return new WaitForEndOfFrame();

                    //Game runs similar code
                    foreach (KeyValuePair<int, ObjectCtrlInfo> item in Studio.Studio.Instance.dicObjectCtrl)
                        item.Value.OnSavePreprocessing();
                    Studio.CameraControl m_CameraCtrl = (Studio.CameraControl)Traverse.Create(Studio.Studio.Instance).Field("m_CameraCtrl").GetValue();
                    Studio.Studio.Instance.sceneInfo.cameraSaveData = m_CameraCtrl.Export();
                    DateTime now = DateTime.Now;
                    string str = $"autosave_{now.Year}_{now.Month:00}{now.Day:00}_{now.Hour:00}{now.Minute:00}_{now.Second:00}_{now.Millisecond:000}.png";
                    string path = $"{UserData.Create(AutosavePathStudio)}{str}";
                    Studio.Studio.Instance.sceneInfo.Save(path);

                    DeleteAutosaves(AutosavePathStudio);

                    SetText("Saved!");
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
    }
}