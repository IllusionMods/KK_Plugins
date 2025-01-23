using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal sealed class ItemInfo
    {
        public RowItemType ItemType;
        public string LabelText { get; set; }

        public string RendererName { get; set; }
        public Action SelectInterpolableButtonRendererOnClick { get; set; }
        public Action ExportUVOnClick { get; set; }
        public Action ExportObjOnClick { get; set; }

        public bool RendererEnabled { get; set; }
        public bool RendererEnabledOriginal { get; set; }
        public Action<bool> RendererEnabledOnChange { get; set; }
        public Action RendererEnabledOnReset { get; set; }

        public int RendererShadowCastingMode { get; set; }
        public int RendererShadowCastingModeOriginal { get; set; }
        public Action<int> RendererShadowCastingModeOnChange { get; set; }
        public Action RendererShadowCastingModeOnReset { get; set; }

        public bool RendererReceiveShadows { get; set; }
        public bool RendererReceiveShadowsOriginal { get; set; }
        public Action<bool> RendererReceiveShadowsOnChange { get; set; }
        public Action RendererReceiveShadowsOnReset { get; set; }

        public bool RendererUpdateWhenOffscreen { get; set; }
        public bool RendererUpdateWhenOffscreenOriginal { get; set; }
        public Action<bool> RendererUpdateWhenOffscreenOnChange { get; set; }
        public Action RendererUpdateWhenOffscreenOnReset { get; set; }

        public bool RendererRecalculateNormals { get; set; }
        public bool RendererRecalculateNormalsOriginal { get; set; }
        public Action<bool> RendererRecalculateNormalsOnChange { get; set; }
        public Action RendererRecalculateNormalsOnReset { get; set; }

        public string MaterialName { get; set; }
        public Action MaterialOnCopy { get; set; }
        public Action MaterialOnPaste { get; set; }
        public Action MaterialOnCopyRemove { get; set; }
        public Action MaterialOnRename { get; set; }

        public string ShaderName { get; set; }
        public string ShaderNameOriginal { get; set; }
        public Action SelectInterpolableButtonShaderOnClick { get; set; }
        public Action<string> ShaderNameOnChange { get; set; }
        public Action ShaderNameOnReset { get; set; }

        public int ShaderRenderQueue { get; set; }
        public int ShaderRenderQueueOriginal { get; set; }
        public Action<int> ShaderRenderQueueOnChange { get; set; }
        public Action ShaderRenderQueueOnReset { get; set; }

        public bool TextureChanged { get; set; }
        public bool TextureExists { get; set; }
        public Action SelectInterpolableButtonTextureOnClick { get; set; }
        public Action TextureOnExport { get; set; }
        public Action TextureOnImport { get; set; }
        public Action TextureOnReset { get; set; }

        public Vector2 Offset { get; set; }
        public Vector2 OffsetOriginal { get; set; }
        public Action<Vector2> OffsetOnChange { get; set; }
        public Action OffsetOnReset { get; set; }

        public Vector2 Scale { get; set; }
        public Vector2 ScaleOriginal { get; set; }
        public Action<Vector2> ScaleOnChange { get; set; }
        public Action ScaleOnReset { get; set; }

        public Color ColorValue { get; set; }
        public Color ColorValueOriginal { get; set; }
        public Action SelectInterpolableButtonColorOnClick { get; set; }
        public Action<Color> ColorValueOnChange { get; set; }
        public Action ColorValueOnReset { get; set; }
        public Action<string, Color, Action<Color>> ColorValueOnEdit { get; set; }
        public Action<string, Color> ColorValueSetToPalette { get; set; }

        public float FloatValue { get; set; }
        public float FloatValueOriginal { get; set; }
        public float FloatValueSliderMin { get; set; } = 0;
        public float FloatValueSliderMax { get; set; } = 1;
        public Action SelectInterpolableButtonFloatOnClick { get; set; }
        public Action<float> FloatValueOnChange { get; set; }
        public Action FloatValueOnReset { get; set; }

        public bool KeywordValue { get; set; }
        public bool KeywordValueOriginal { get; set; }
        public Action<bool> KeywordValueOnChange { get; set; }
        public Action KeywordValueOnReset { get; set; }

        public ItemInfo(RowItemType itemType, string labelText = "")
        {
            ItemType = itemType;
            LabelText = labelText;
        }

        public enum RowItemType { Renderer, RendererEnabled, RendererShadowCastingMode, RendererReceiveShadows, RendererUpdateWhenOffscreen, RendererRecalculateNormals, Material, Shader, ShaderRenderQueue, TextureProperty, TextureOffsetScale, ColorProperty, FloatProperty, KeywordProperty }
    }
}
