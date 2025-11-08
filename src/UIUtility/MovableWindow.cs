using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UILib
{
    internal class MovableWindow : UIBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Vector2 CachedDragPosition;
        private Vector2 CachedMousePosition;
        private bool PointerDownCalled;

        public event Action<PointerEventData> OnPointerDownEvent;
        public event Action<PointerEventData> OnDragEvent;
        public event Action<PointerEventData> OnPointerUpEvent;

        public RectTransform ToDrag;
        public bool PreventDragout;

        public void OnPointerDown(PointerEventData eventData)
        {
            PointerDownCalled = true;
            CachedDragPosition = ToDrag.position;
            CachedMousePosition = Input.mousePosition;
            OnPointerDownEvent?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (PointerDownCalled == false)
                return;
            Vector3 newPos = CachedDragPosition + ((Vector2)Input.mousePosition - CachedMousePosition);
            if (PreventDragout)
            {
                float height = Screen.height * (ToDrag.anchorMax.y - ToDrag.anchorMin.y) + ToDrag.sizeDelta.y;
                float newX = Mathf.Clamp(newPos.x, 0, Screen.width);
                float newY = Mathf.Clamp(newPos.y, height / 2, Screen.height - height / 2);
                newPos = new Vector3(newX, newY, newPos.z);
            }
            ToDrag.position = newPos;
            OnDragEvent?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (PointerDownCalled == false)
                return;
            PointerDownCalled = false;
            OnPointerUpEvent?.Invoke(eventData);
        }
    }
}
