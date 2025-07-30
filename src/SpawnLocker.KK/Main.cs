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
        [HarmonyPatch(typeof(ActionGame.PreviewClassData), MethodType.Constructor, new System.Type[] { typeof(GameObject) })]
        static protected void PreviewClassDataConstructorPostfix(ActionGame.PreviewClassData __instance)
        {
            var observer = __instance.button.gameObject.GetOrAddComponent<ClickObserver>();
            observer.previewData = __instance;
        }
    }

    [RequireComponent(typeof(Button))]
    public class ClickObserver : MonoBehaviour, IPointerClickHandler
    {
        public ActionGame.PreviewClassData previewData = null;

        protected Image m_Desk;
        protected RectTransform m_DeskRect;
        protected Vector2 m_SizeDelta;
        protected Vector2 m_OffsetMin;
        protected Vector2 m_OffsetMax;

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Middle:
                    SpawnLockMain.ToggleLock(previewData?.data?.charFile);
                    break;
            }

            UpdateStatus();
        }

        public void UpdateStatus()
        {
            if (previewData == null)
                return;

            if(m_DeskRect == null)
            {
                var desk = gameObject.FindChild("desk").GetComponent<Image>();
                
                if (desk == null)
                    return;

                m_Desk = desk;
                m_DeskRect = desk.rectTransform;
                m_SizeDelta = m_DeskRect.sizeDelta;
                m_OffsetMin = m_DeskRect.offsetMin;
                m_OffsetMax = m_DeskRect.offsetMax;
            }

            m_DeskRect.gameObject.SetActive(true);

            if (SpawnLockMain.IsLocked(previewData.data?.charFile))
            {
                float mergin = 16;
                m_Desk.color = Color.black;
                m_DeskRect.sizeDelta = m_SizeDelta + new Vector2(mergin, mergin);
                m_DeskRect.offsetMin = m_OffsetMin - new Vector2(mergin / 2, mergin / 2);
                m_DeskRect.offsetMax = m_OffsetMax + new Vector2(mergin / 2, mergin / 2);
            }
            else
            {
                m_Desk.color = Color.white;
                m_DeskRect.sizeDelta = m_SizeDelta;
                m_DeskRect.offsetMin = m_OffsetMin;
                m_DeskRect.offsetMax = m_OffsetMax;
            }
        }
    }
}
