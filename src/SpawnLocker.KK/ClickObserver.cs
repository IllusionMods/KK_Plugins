using StrayTech;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpawnLocker
{
    [RequireComponent(typeof(Button))]
    public class ClickObserver : MonoBehaviour, IPointerClickHandler
    {
        public ActionGame.PreviewClassData previewData;

        protected Image m_Desk;
        protected RectTransform m_DeskRect;
        protected Vector2 m_SizeDelta;
        protected Vector2 m_OffsetMin;
        protected Vector2 m_OffsetMax;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Middle)
            {
                SpawnLockMain.ToggleLock(previewData?.data?.charFile);
            }
            // BUG: Should be in the block above like for KKS?
            UpdateStatus();
        }

        public void UpdateStatus()
        {
            if (previewData == null)
                return;

            if (m_DeskRect == null)
            {
                var desk = gameObject.FindChild("desk")?.GetComponent<Image>();

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
