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
        /// <summary>Invoked while dragging, after the target size is updated.</summary>
        public System.Action OnResize;

        private Vector2 _startMousePos;
        private Vector2 _startRenderedSize;
        private Vector2 _startSizeDelta;

        public void OnPointerDown(PointerEventData eventData)
        {
            _startMousePos = eventData.position;
            //Capture the actual rendered size and the sizeDelta separately. For a stretch-anchored rect
            //(anchorMin != anchorMax) sizeDelta is only an offset from the anchor-derived size, so clamping
            //sizeDelta directly would make the real minimum size huge. Clamp the rendered size instead.
            _startRenderedSize = TargetRT.rect.size;
            _startSizeDelta = TargetRT.sizeDelta;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (TargetRT == null) return;
            var delta = eventData.position - _startMousePos;
            float targetW = Mathf.Max(MinSize.x, _startRenderedSize.x + delta.x);
            float targetH = Mathf.Max(MinSize.y, _startRenderedSize.y - delta.y);
            //Apply the change to sizeDelta while preserving whatever anchor span the rect already has
            TargetRT.sizeDelta = new Vector2(
                _startSizeDelta.x + (targetW - _startRenderedSize.x),
                _startSizeDelta.y + (targetH - _startRenderedSize.y));
            OnResize?.Invoke();
        }
    }
}
