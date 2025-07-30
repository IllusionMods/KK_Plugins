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

        protected Text m_LockText;

        void Start()
        {
            GameObject nameTextGObj = gameObject.FindChild("passport_heroine/resize/text/NameText");

            if (nameTextGObj != null)
            {
                m_LockText = Instantiate(nameTextGObj, nameTextGObj.transform.parent).GetComponent<Text>();
                m_LockText.alignment = TextAnchor.MiddleRight;
                m_LockText.color = Color.black;

                Vector2 pos = m_LockText.rectTransform.anchoredPosition;
                pos.y += 155;
                m_LockText.rectTransform.anchoredPosition = pos;
            }

        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Middle)
            {
                SpawnLockMain.ToggleLock(previewData?.data?.charFile);
                UpdateStatus();
            }
        }

        public void UpdateStatus()
        {
            if (previewData == null || m_LockText == null)
                return;

            m_LockText.text = SpawnLockMain.IsLocked(previewData.data?.charFile) ? "LOCK" : "";
        }
    }
}
