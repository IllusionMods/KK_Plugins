using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class ShaderHintUnderline : MaskableGraphic
    {
        private const float DashWidth = 3f;
        private const float DashGap = 2f;
        private const float LineThicknessPixels = 2f;
        private const float UnderlineRise = 2f;
        private const float MinimumSegmentSize = 0.01f;
        private const float MinimumGlyphSize = 0.01f;
        private const float MinimumBodyHeightRatio = 0.5f;

        private Text _label;
        private ShaderHintTextMeshCapture _meshCapture;
        private string _lastText;
        private float _lastRectWidth = -1f;
        private float _lastRectHeight = -1f;
        private int _lastFontSize = -1;
        private int _lastBestFitFontSize = -1;
        private int _lastCaptureVersion = -1;
        private bool _retryMeshRead;

        internal static ShaderHintUnderline GetOrCreate(Text label)
        {
            if (label == null)
                return null;

            var existing =
                label.GetComponentInChildren<ShaderHintUnderline>(true);
            if (existing != null)
            {
                existing.Attach(label);
                return existing;
            }

            var indicatorObject = new GameObject(
                "ShaderHintUnderline",
                typeof(RectTransform),
                typeof(CanvasRenderer));
            indicatorObject.transform.SetParent(label.transform, false);

            var indicatorTransform =
                (RectTransform)indicatorObject.transform;
            indicatorTransform.anchorMin = Vector2.zero;
            indicatorTransform.anchorMax = Vector2.one;
            indicatorTransform.offsetMin = Vector2.zero;
            indicatorTransform.offsetMax = Vector2.zero;
            indicatorTransform.pivot = label.rectTransform.pivot;
            indicatorTransform.localScale = Vector3.one;

            var indicator =
                indicatorObject.AddComponent<ShaderHintUnderline>();
            indicator.Attach(label);
            indicator.color =
                MaterialEditorStyles.ShaderHintUnderlineColor;
            indicator.raycastTarget = false;
            indicator.SetVisible(false);
            return indicator;
        }

        internal void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible)
            {
                if (_meshCapture != null)
                    _meshCapture.enabled = visible;
                return;
            }

            if (visible)
            {
                if (_meshCapture != null)
                    _meshCapture.enabled = true;
                ResetCachedLayout();
                if (_label != null)
                    _label.SetVerticesDirty();
            }
            else if (_meshCapture != null)
            {
                _meshCapture.enabled = false;
            }

            gameObject.SetActive(visible);
            if (visible)
                SetVerticesDirty();
        }

        private void Attach(Text label)
        {
            if (_label == label)
                return;

            _label = label;
            _meshCapture =
                label.GetComponent<ShaderHintTextMeshCapture>();
            if (_meshCapture == null)
            {
                _meshCapture =
                    label.gameObject.AddComponent<ShaderHintTextMeshCapture>();
            }

            _meshCapture.enabled = gameObject.activeSelf;
            ResetCachedLayout();
        }

        private void LateUpdate()
        {
            if (_label == null)
                return;

            var rect = _label.rectTransform.rect;
            var bestFitFontSize =
                _label.cachedTextGenerator.fontSizeUsedForBestFit;
            if (!_retryMeshRead
                && _lastText == _label.text
                && Mathf.Approximately(_lastRectWidth, rect.width)
                && Mathf.Approximately(_lastRectHeight, rect.height)
                && _lastFontSize == _label.fontSize
                && _lastBestFitFontSize == bestFitFontSize
                && _lastCaptureVersion == _meshCapture.Version
                && _meshCapture.Matches(_label))
            {
                return;
            }

            _retryMeshRead = false;
            _lastText = _label.text;
            _lastRectWidth = rect.width;
            _lastRectHeight = rect.height;
            _lastFontSize = _label.fontSize;
            _lastBestFitFontSize = bestFitFontSize;

            // Register the label before this child graphic so the text mesh is
            // current even when a virtualized row is rebound while scrolling.
            _label.SetVerticesDirty();
            SetVerticesDirty();
        }

        public override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (_label == null || string.IsNullOrEmpty(_label.text))
                return;

            // Rebuild synchronously so the capture effect holds the final text
            // vertices before this independent Graphic draws its underline.
            _label.Rebuild(CanvasUpdate.PreRender);
            if (!_meshCapture.Matches(_label))
            {
                _retryMeshRead = true;
                return;
            }

            _lastCaptureVersion = _meshCapture.Version;

            float left;
            float right;
            float bodyBottom;
            if (!TryGetVisibleTextBounds(
                    _meshCapture.Vertices,
                    out left,
                    out right,
                    out bodyBottom))
            {
                return;
            }

            var yMax = bodyBottom + UnderlineRise;
            var yMin = yMax - GetLineThickness();
            AddDashes(vertexHelper, left, right, yMin, yMax);
        }

        private float GetLineThickness()
        {
            var scaleFactor =
                canvas != null ? canvas.scaleFactor : 1f;
            if (scaleFactor <= 0f)
                scaleFactor = 1f;

            return LineThicknessPixels / scaleFactor;
        }

        private static bool TryGetVisibleTextBounds(
            IList<Vector3> vertices,
            out float left,
            out float right,
            out float bodyBottom)
        {
            left = float.PositiveInfinity;
            right = float.NegativeInfinity;
            bodyBottom = float.NegativeInfinity;

            const int verticesPerGlyph = 4;
            var glyphCount = vertices.Count / verticesPerGlyph;
            var maximumGlyphHeight = 0f;
            for (var glyphIndex = 0;
                 glyphIndex < glyphCount;
                 glyphIndex++)
            {
                GlyphBounds bounds;
                if (!TryGetGlyphBounds(
                        vertices,
                        glyphIndex,
                        verticesPerGlyph,
                        out bounds))
                    continue;

                left = Mathf.Min(left, bounds.Left);
                right = Mathf.Max(right, bounds.Right);
                maximumGlyphHeight =
                    Mathf.Max(maximumGlyphHeight, bounds.Height);
            }

            if (float.IsInfinity(left)
                || float.IsInfinity(right)
                || right <= left)
                return false;

            var minimumBodyHeight =
                maximumGlyphHeight * MinimumBodyHeightRatio;
            for (var glyphIndex = 0;
                 glyphIndex < glyphCount;
                 glyphIndex++)
            {
                GlyphBounds bounds;
                if (!TryGetGlyphBounds(
                        vertices,
                        glyphIndex,
                        verticesPerGlyph,
                        out bounds)
                    || bounds.Height < minimumBodyHeight)
                    continue;

                bodyBottom = Mathf.Max(bodyBottom, bounds.Bottom);
            }

            return !float.IsInfinity(bodyBottom);
        }

        private static bool TryGetGlyphBounds(
            IList<Vector3> vertices,
            int glyphIndex,
            int verticesPerGlyph,
            out GlyphBounds bounds)
        {
            var left = float.PositiveInfinity;
            var right = float.NegativeInfinity;
            var bottom = float.PositiveInfinity;
            var top = float.NegativeInfinity;

            for (var vertexIndex = 0;
                 vertexIndex < verticesPerGlyph;
                 vertexIndex++)
            {
                var position =
                    vertices[glyphIndex * verticesPerGlyph + vertexIndex];
                left = Mathf.Min(left, position.x);
                right = Mathf.Max(right, position.x);
                bottom = Mathf.Min(bottom, position.y);
                top = Mathf.Max(top, position.y);
            }

            bounds = new GlyphBounds(left, right, bottom, top);
            return bounds.Width > MinimumGlyphSize
                   && bounds.Height > MinimumGlyphSize;
        }

        private void AddDashes(
            VertexHelper vertexHelper,
            float left,
            float right,
            float yMin,
            float yMax)
        {
            var lastDashEnd = left;
            for (var x = left; x < right; x += DashWidth + DashGap)
            {
                lastDashEnd = Mathf.Min(x + DashWidth, right);
                AddQuad(
                    vertexHelper,
                    x,
                    lastDashEnd,
                    yMin,
                    yMax);
            }

            if (right - lastDashEnd > MinimumSegmentSize)
            {
                AddQuad(
                    vertexHelper,
                    Mathf.Max(left, right - DashWidth),
                    right,
                    yMin,
                    yMax);
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
            vertexHelper.AddVert(
                new Vector3(xMin, yMin),
                color,
                Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(xMin, yMax),
                color,
                Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(xMax, yMax),
                color,
                Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(xMax, yMin),
                color,
                Vector2.zero);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start + 2, start + 3, start);
        }

        private void ResetCachedLayout()
        {
            _lastText = null;
            _lastRectWidth = -1f;
            _lastRectHeight = -1f;
            _lastFontSize = -1;
            _lastBestFitFontSize = -1;
            _lastCaptureVersion = -1;
            _retryMeshRead = true;
        }

        private struct GlyphBounds
        {
            internal readonly float Left;
            internal readonly float Right;
            internal readonly float Bottom;
            internal readonly float Top;

            internal float Width => Right - Left;
            internal float Height => Top - Bottom;

            internal GlyphBounds(
                float left,
                float right,
                float bottom,
                float top)
            {
                Left = left;
                Right = right;
                Bottom = bottom;
                Top = top;
            }
        }
    }

    [RequireComponent(typeof(Text))]
    internal sealed class ShaderHintTextMeshCapture : BaseMeshEffect
    {
        private readonly List<Vector3> _vertices =
            new List<Vector3>();

        private string _capturedText;
        private float _capturedRectWidth = -1f;
        private float _capturedRectHeight = -1f;
        private int _capturedFontSize = -1;
        private int _capturedBestFitFontSize = -1;

        internal IList<Vector3> Vertices => _vertices;
        internal int Version { get; private set; }

        internal bool Matches(Text label)
        {
            if (label == null)
                return false;

            var rect = label.rectTransform.rect;
            return _capturedText == label.text
                   && Mathf.Approximately(
                       _capturedRectWidth,
                       rect.width)
                   && Mathf.Approximately(
                       _capturedRectHeight,
                       rect.height)
                   && _capturedFontSize == label.fontSize
                   && _capturedBestFitFontSize
                   == label.cachedTextGenerator.fontSizeUsedForBestFit;
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive())
                return;

            _vertices.Clear();
            var vertex = new UIVertex();
            for (var index = 0;
                 index < vertexHelper.currentVertCount;
                 index++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, index);
                _vertices.Add(vertex.position);
            }

            var label = graphic as Text;
            if (label != null)
            {
                var rect = label.rectTransform.rect;
                _capturedText = label.text;
                _capturedRectWidth = rect.width;
                _capturedRectHeight = rect.height;
                _capturedFontSize = label.fontSize;
                _capturedBestFitFontSize =
                    label.cachedTextGenerator.fontSizeUsedForBestFit;
            }

            Version++;
        }
    }
}
