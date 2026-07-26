using System;

namespace MaterialEditorAPI
{
    internal sealed class MaterialRowModel : RowModel
    {
        internal MaterialRowModel()
            : base(RowItemType.Material, "Material")
        {
        }

        internal string MaterialName { get; set; }
        internal bool Collapsed { get; set; }
        internal Action<bool> CollapsedOnChange { get; set; }
        internal Action Copy { get; set; }
        internal Action Paste { get; set; }
        internal Action CopyOrRemove { get; set; }
        internal Action Rename { get; set; }
    }

    internal sealed class ShaderRowModel : RowModel
    {
        internal ShaderRowModel()
            : base(RowItemType.Shader, "Shader")
        {
        }

        internal string ShaderName { get; set; }
        internal string OriginalShaderName { get; set; }
        internal bool Collapsed { get; set; }
        internal Action<bool> CollapsedOnChange { get; set; }
        internal bool HasCategories { get; set; }
        internal bool AllCategoriesCollapsed { get; set; }
        internal Action<bool> CategoriesCollapsedOnChange { get; set; }
        internal Action SelectInterpolable { get; set; }
        internal Action<string> ShaderNameOnChange { get; set; }
        internal Action ShaderNameOnReset { get; set; }
    }

    internal sealed class ShaderRenderQueueRowModel : RowModel
    {
        internal ShaderRenderQueueRowModel()
            : base(RowItemType.ShaderRenderQueue, "Render Queue")
        {
        }

        internal int Value { get; set; }
        internal int OriginalValue { get; set; }
        internal Action<int> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
    }
}
