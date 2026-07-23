using System.Collections.Generic;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialEditorAPI
{
    // Centralized layout and typography for virtualized Material Editor rows.
    internal static class RowStyle
    {
        internal static void Apply(GameObject rowTemplate)
        {
            foreach (var layout in rowTemplate.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = true;

                var panelRect = layout.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                panelRect.localScale = Vector3.one;

                foreach (var text in layout.GetComponentsInChildren<Text>(true))
                {
                    text.alignment = WithMiddleVerticalAlignment(text.alignment);
                    text.fontSize = Mathf.Min(text.fontSize, UIUtility.defaultFontSize);
                    if (text.resizeTextForBestFit)
                        text.resizeTextMaxSize = Mathf.Min(text.resizeTextMaxSize, UIUtility.defaultFontSize);

                    if (text.GetComponent<RowTextVisualCenter>() == null)
                        text.gameObject.AddComponent<RowTextVisualCenter>();
                    text.SetVerticesDirty();
                }
            }
        }

        private static TextAnchor WithMiddleVerticalAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                    return TextAnchor.MiddleCenter;
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return TextAnchor.MiddleRight;
                default:
                    return TextAnchor.MiddleLeft;
            }
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
