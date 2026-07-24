using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
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

    // Center the typographic body while allowing underscores and descenders to hang
    // below the baseline instead of stretching the entire word to both row edges.
    internal sealed class RowTextVisualCenter : BaseMeshEffect
    {
        private List<GlyphBounds> _glyphBounds;
        private List<float> _bodyBottoms;

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

            var targetCenter = graphic.rectTransform.rect.center.y;
            var offset = targetCenter - (baseline + bodyTop) * 0.5f;
            if (Mathf.Approximately(offset, 0f))
                return;

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
