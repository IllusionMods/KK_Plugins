using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal interface IMaterialEditorColorPalette
    {
        void Setup(string title, object data, string materialName, Color color, Action<Color> onChanged, bool useAlpha);
        void SetColor(Color value);
        void Close();
        bool IsShowing(string title, object data, string materialName);
    }
}
