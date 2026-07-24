using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class ColorPropertyRowModel : RowModel
    {
        internal ColorPropertyRowModel(string labelText)
            : base(RowItemType.ColorProperty, labelText)
        {
        }

        internal Color Value { get; set; }
        internal Color OriginalValue { get; set; }
        internal Action SelectInterpolable { get; set; }
        internal Action<Color> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
        internal Action<string, Color, Action<Color>> Edit { get; set; }
        internal Action<string, Color> SetToPalette { get; set; }
    }

    internal sealed class FloatPropertyRowModel : RowModel
    {
        internal FloatPropertyRowModel(string labelText)
            : base(RowItemType.FloatProperty, labelText)
        {
        }

        internal float Value { get; set; }
        internal float OriginalValue { get; set; }
        internal float SliderMinimum { get; set; }
        internal float SliderMaximum { get; set; } = 1f;
        internal Action SelectInterpolable { get; set; }
        internal Action<float> ValueOnChange { get; set; }
        internal Action ValueOnReset { get; set; }
    }

    internal sealed class KeywordPropertyRowModel : BooleanValueRowModel
    {
        internal KeywordPropertyRowModel(string labelText)
            : base(RowItemType.KeywordProperty, labelText)
        {
        }
    }
}
