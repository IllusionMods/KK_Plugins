using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class ShaderHintUnderline : MaskableGraphic
    {
        private const float DashWidth = 3f;
        private const float DashGap = 2f;
        private const float LineThickness = 1f;
        private const float UnderlineOffset = 0.5f;

        private Text _label;
        private string _lastText;
        private float _lastPreferredWidth = -1f;
        private float _lastPreferredHeight = -1f;
        private float _lastRectWidth = -1f;
        private float _lastRectHeight = -1f;

        internal static ShaderHintUnderline GetOrCreate(Text label)
        {
            if (label == null)
                return null;

            var existing = label.GetComponentInChildren<ShaderHintUnderline>(true);
            if (existing != null)
            {
                existing._label = label;
                return existing;
            }

            var indicatorObject = new GameObject(
                "ShaderHintUnderline",
                typeof(RectTransform),
                typeof(CanvasRenderer));
            indicatorObject.transform.SetParent(label.transform, false);

            var indicatorTransform = (RectTransform)indicatorObject.transform;
            indicatorTransform.anchorMin = Vector2.zero;
            indicatorTransform.anchorMax = Vector2.one;
            indicatorTransform.offsetMin = Vector2.zero;
            indicatorTransform.offsetMax = Vector2.zero;
            indicatorTransform.localScale = Vector3.one;

            var indicator = indicatorObject.AddComponent<ShaderHintUnderline>();
            indicator._label = label;
            indicator.color = MaterialEditorStyles.ShaderHintUnderlineColor;
            indicator.raycastTarget = false;
            indicatorObject.SetActive(false);
            return indicator;
        }

        internal void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible)
                return;

            gameObject.SetActive(visible);
            if (visible)
            {
                ResetCachedLayout();
                SetVerticesDirty();
            }
        }

        public override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (_label == null || string.IsNullOrEmpty(_label.text))
                return;

            var bounds = GetUnderlineBounds(rectTransform.rect, _label);
            if (bounds.y <= bounds.x)
                return;

            var yMin = GetTextBottom(rectTransform.rect, _label) + UnderlineOffset;
            var yMax = yMin + LineThickness;
            for (var x = bounds.x; x < bounds.y; x += DashWidth + DashGap)
                AddQuad(vertexHelper, x, Mathf.Min(x + DashWidth, bounds.y), yMin, yMax);
        }

        private void LateUpdate()
        {
            if (_label == null)
                return;

            var preferredWidth = _label.preferredWidth;
            var preferredHeight = _label.preferredHeight;
            var rectWidth = rectTransform.rect.width;
            var rectHeight = rectTransform.rect.height;
            if (_lastText == _label.text
                && Mathf.Approximately(_lastPreferredWidth, preferredWidth)
                && Mathf.Approximately(_lastPreferredHeight, preferredHeight)
                && Mathf.Approximately(_lastRectWidth, rectWidth)
                && Mathf.Approximately(_lastRectHeight, rectHeight))
            {
                return;
            }

            _lastText = _label.text;
            _lastPreferredWidth = preferredWidth;
            _lastPreferredHeight = preferredHeight;
            _lastRectWidth = rectWidth;
            _lastRectHeight = rectHeight;
            SetVerticesDirty();
        }

        private static Vector2 GetUnderlineBounds(Rect rect, Text label)
        {
            var width = Mathf.Min(rect.width, label.preferredWidth);
            switch (label.alignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    return new Vector2(rect.center.x - width * 0.5f, rect.center.x + width * 0.5f);
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return new Vector2(rect.xMax - width, rect.xMax);
                default:
                    return new Vector2(rect.xMin, rect.xMin + width);
            }
        }

        private static float GetTextBottom(Rect rect, Text label)
        {
            var textHeight = Mathf.Min(rect.height, label.preferredHeight);
            switch (label.alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    return rect.yMax - textHeight;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    return rect.yMin;
                default:
                    return rect.center.y - textHeight * 0.5f;
            }
        }

        private void AddQuad(
            VertexHelper vertexHelper,
            float xMin,
            float xMax,
            float yMin,
            float yMax)
        {
            var start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(new Vector3(xMin, yMin), color, Vector2.zero);
            vertexHelper.AddVert(new Vector3(xMin, yMax), color, Vector2.zero);
            vertexHelper.AddVert(new Vector3(xMax, yMax), color, Vector2.zero);
            vertexHelper.AddVert(new Vector3(xMax, yMin), color, Vector2.zero);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start + 2, start + 3, start);
        }

        private void ResetCachedLayout()
        {
            _lastText = null;
            _lastPreferredWidth = -1f;
            _lastPreferredHeight = -1f;
            _lastRectWidth = -1f;
            _lastRectHeight = -1f;
        }
    }
}
