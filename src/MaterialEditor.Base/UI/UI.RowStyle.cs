using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    internal static class FoldGlyphs
    {
        internal const string Collapsed = "∨";
        internal const string Expanded = "∧";
        internal const string AllCollapsed = "∨∨";
        internal const string AllExpanded = "∧∧";
    }

    // Compatibility adapter retained for the existing row construction code.
    internal static class RowStyle
    {
        internal static void Apply(GameObject rowTemplate)
        {
            MaterialEditorStyles.ApplyRow(rowTemplate);
        }

        internal static void ApplyTypography(GameObject root)
        {
            MaterialEditorStyles.ApplyTypography(root);
        }
    }

    internal enum TextVisualCenterMode
    {
        TypographicBody,
        VisibleBounds
    }

    // Row and input text is centered by its typographic body so underscores and
    // descenders can hang naturally. Multiline tooltip text is centered by the
    // bounds of the complete generated text block.
    internal sealed class RowTextVisualCenter : BaseMeshEffect
    {
        [SerializeField] private TextVisualCenterMode _mode;
        private List<GlyphBounds> _glyphBounds;
        private List<float> _bodyBottoms;

        internal void SetMode(TextVisualCenterMode mode)
        {
            _mode = mode;
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper.currentVertCount == 0)
                return;

            const int verticesPerGlyph = 4;
            var glyphCapacity = vertexHelper.currentVertCount / verticesPerGlyph;
            if (glyphCapacity == 0)
                return;

            var vertex = new UIVertex();
            var glyphBounds = _glyphBounds ?? (_glyphBounds = new List<GlyphBounds>());
            glyphBounds.Clear();
            var maxGlyphHeight = 0f;

            for (var glyphIndex = 0; glyphIndex < glyphCapacity; glyphIndex++)
            {
                var bottom = float.PositiveInfinity;
                var top = float.NegativeInfinity;

                for (var vertexIndex = 0; vertexIndex < verticesPerGlyph; vertexIndex++)
                {
                    vertexHelper.PopulateUIVertex(ref vertex, glyphIndex * verticesPerGlyph + vertexIndex);
                    bottom = Mathf.Min(bottom, vertex.position.y);
                    top = Mathf.Max(top, vertex.position.y);
                }

                var height = top - bottom;
                if (height <= 0.01f)
                    continue;

                glyphBounds.Add(new GlyphBounds(bottom, top));
                maxGlyphHeight = Mathf.Max(maxGlyphHeight, height);
            }

            if (glyphBounds.Count == 0)
                return;

            if (_mode == TextVisualCenterMode.VisibleBounds)
            {
                var blockBottom = float.PositiveInfinity;
                var blockTop = float.NegativeInfinity;
                for (var i = 0; i < glyphBounds.Count; i++)
                {
                    blockBottom = Mathf.Min(blockBottom, glyphBounds[i].Bottom);
                    blockTop = Mathf.Max(blockTop, glyphBounds[i].Top);
                }

                CenterVertices(vertexHelper, blockBottom, blockTop);
                return;
            }

            var bodyBottoms = _bodyBottoms ?? (_bodyBottoms = new List<float>());
            bodyBottoms.Clear();
            var bodyTop = float.NegativeInfinity;
            var minimumBodyHeight = maxGlyphHeight * 0.5f;

            for (var i = 0; i < glyphBounds.Count; i++)
            {
                var bounds = glyphBounds[i];
                if (bounds.Height < minimumBodyHeight)
                    continue;

                bodyBottoms.Add(bounds.Bottom);
                bodyTop = Mathf.Max(bodyTop, bounds.Top);
            }

            if (bodyBottoms.Count == 0)
                return;

            bodyBottoms.Sort();
            var middle = bodyBottoms.Count / 2;
            var baseline = bodyBottoms.Count % 2 == 0
                ? (bodyBottoms[middle - 1] + bodyBottoms[middle]) * 0.5f
                : bodyBottoms[middle];

            CenterVertices(vertexHelper, baseline, bodyTop);
        }

        private void CenterVertices(
            VertexHelper vertexHelper,
            float visualBottom,
            float visualTop)
        {
            var targetCenter = graphic.rectTransform.rect.center.y;
            var offset = targetCenter - (visualBottom + visualTop) * 0.5f;
            if (Mathf.Approximately(offset, 0f))
                return;

            var vertex = new UIVertex();
            for (var i = 0; i < vertexHelper.currentVertCount; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                vertex.position.y += offset;
                vertexHelper.SetUIVertex(vertex, i);
            }
        }

        private struct GlyphBounds
        {
            internal readonly float Bottom;
            internal readonly float Top;

            internal float Height => Top - Bottom;

            internal GlyphBounds(float bottom, float top)
            {
                Bottom = bottom;
                Top = top;
            }
        }
    }
}
