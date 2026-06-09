using UnityEngine;
using UnityEngine.EventSystems;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Attach to a small corner element to allow resizing of a parent RectTransform by dragging.
    /// </summary>
    internal class ResizeHandle : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public RectTransform TargetRT;
        public Vector2 MinSize = new Vector2(100f, 100f);

        private Vector2 _startMousePos;
        private Vector2 _startSize;

        public void OnPointerDown(PointerEventData eventData)
        {
            _startMousePos = eventData.position;
            _startSize = TargetRT.sizeDelta;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (TargetRT == null) return;
            var delta = eventData.position - _startMousePos;
            float newW = Mathf.Max(MinSize.x, _startSize.x + delta.x);
            float newH = Mathf.Max(MinSize.y, _startSize.y - delta.y);
            TargetRT.sizeDelta = new Vector2(newW, newH);
        }
    }
}
