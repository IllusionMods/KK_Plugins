using System;

namespace MaterialEditorAPI
{
    internal sealed class RendererRowModel : RowModel
    {
        internal RendererRowModel()
            : base(RowItemType.Renderer, "Renderer")
        {
        }

        internal string RendererName { get; set; }
        internal Action SelectInterpolable { get; set; }
        internal Action ExportUv { get; set; }
        internal Action ExportObj { get; set; }
    }

    internal sealed class RendererEnabledRowModel : BooleanValueRowModel
    {
        internal RendererEnabledRowModel()
            : base(RowItemType.RendererEnabled, "Enabled")
        {
        }
    }

    internal sealed class RendererShadowCastingModeRowModel : RowModel
    {
        internal RendererShadowCastingModeRowModel()
            : base(RowItemType.RendererShadowCastingMode, "Shadow Casting Mode")
        {
        }

        internal int Value { get; set; }
        internal int OriginalValue { get; set; }
        internal Action<int> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
    }

    internal sealed class RendererReceiveShadowsRowModel : BooleanValueRowModel
    {
        internal RendererReceiveShadowsRowModel()
            : base(RowItemType.RendererReceiveShadows, "Receive Shadows")
        {
        }
    }

    internal sealed class RendererUpdateWhenOffscreenRowModel : BooleanValueRowModel
    {
        internal RendererUpdateWhenOffscreenRowModel()
            : base(RowItemType.RendererUpdateWhenOffscreen, "Update When Off-Screen")
        {
        }
    }

    internal sealed class RendererRecalculateNormalsRowModel : BooleanValueRowModel
    {
        internal RendererRecalculateNormalsRowModel()
            : base(RowItemType.RendererRecalculateNormals, "Recalculate Normals")
        {
        }
    }
}
