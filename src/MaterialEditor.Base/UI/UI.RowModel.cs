using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    // Common identity and click-event context shared by every virtualized row.
    internal abstract class RowModel
    {
        protected RowModel(RowItemType itemType, string labelText)
        {
            ItemType = itemType;
            LabelText = labelText ?? string.Empty;
        }

        internal RowItemType ItemType { get; }
        internal string LabelText { get; set; }
        internal string TooltipText { get; set; }
        internal GameObject GameObject { get; set; }
        internal object Data { get; set; }
        internal Renderer Renderer { get; set; }
        internal Material Material { get; set; }
        internal Projector Projector { get; set; }
        internal string PropertyName { get; set; }
        internal MaterialEditorPropertyDescriptor PublicDescriptor { get; set; }

        internal enum RowItemType
        {
            Renderer,
            RendererEnabled,
            RendererShadowCastingMode,
            RendererReceiveShadows,
            RendererUpdateWhenOffscreen,
            RendererRecalculateNormals,
            Material,
            Shader,
            ShaderRenderQueue,
            PropertyCategory,
            TextureProperty,
            TextureOffsetScale,
            ColorProperty,
            FloatProperty,
            KeywordProperty
        }
    }

    internal abstract class BooleanValueRowModel : RowModel
    {
        protected BooleanValueRowModel(RowItemType itemType, string labelText)
            : base(itemType, labelText)
        {
        }

        internal bool Value { get; set; }
        internal bool OriginalValue { get; set; }
        internal Action<bool> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
    }

}
