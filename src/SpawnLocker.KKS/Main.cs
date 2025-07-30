using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Collections;
using Illusion.Game;
using KKAPI;
using KKAPI.Utilities;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniRx;
using System.Data;
using StrayTech;
using TMPro;

namespace SpawnLocker
{
    public partial class SpawnLockMain
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionGame.PreviewClassData), nameof(ActionGame.PreviewClassData.Set))]
        static protected void PreviewClassDataSetPostfix(ActionGame.PreviewClassData __instance, SaveData.CharaData charaData)
        {
            _UpdateStatus(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionGame.PreviewClassData), nameof(ActionGame.PreviewClassData.Clear))]
        static protected void PreviewClassDataClearPostfix(ActionGame.PreviewClassData __instance)
        {
            _UpdateStatus(__instance);
        }

        static void _UpdateStatus(ActionGame.PreviewClassData __instance)
        {
            var observer = __instance.button.GetComponent<ClickObserver>();

            if (observer != null)
            {
                observer.UpdateStatus();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionGame.PreviewClassData), nameof(ActionGame.PreviewClassData.Awake))]
        static protected void PreviewClassAwakePostfix(ActionGame.PreviewClassData __instance)
        {
            var observer = __instance.button.gameObject.GetOrAddComponent<ClickObserver>();
            observer.previewData = __instance;
        }
    }

    [RequireComponent(typeof(Button))]
    public class ClickObserver : MonoBehaviour, IPointerClickHandler
    {
        public ActionGame.PreviewClassData previewData = null;

        protected UnityEngine.UI.Text m_LockText;

        void Start()
        {
            GameObject nameTextGObj = gameObject.FindChild("passport_heroine/resize/text/NameText");

            if(nameTextGObj != null)
            {
                m_LockText = GameObject.Instantiate(nameTextGObj, nameTextGObj.transform.parent).GetComponent<UnityEngine.UI.Text>();
                m_LockText.alignment = TextAnchor.MiddleRight;
                m_LockText.color = Color.black;

                Vector2 pos = m_LockText.rectTransform.anchoredPosition;
                pos.y += 155;
                m_LockText.rectTransform.anchoredPosition = pos;
            }
            
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Middle:
                    SpawnLockMain.ToggleLock(previewData?.data?.charFile);
                    UpdateStatus();
                    break;
            }
        }

        public void UpdateStatus()
        {
            if (previewData == null || m_LockText == null)
                return;

            if (SpawnLockMain.IsLocked(previewData.data?.charFile))
            {
                m_LockText.text = "LOCK";
            }
            else
            {
                m_LockText.text = "";
            }
        }
    }
}
