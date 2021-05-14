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
            ToDrag.position = CachedDragPosition + ((Vector2)Input.mousePosition - CachedMousePosition);
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
