namespace MaterialEditorAPI
{
    internal enum TooltipDisplayKind
    {
        None,
        Standard,
        ShaderHint
    }

    internal static class TooltipDisplayPolicy
    {
        internal static TooltipDisplayKind Resolve(
            bool hovered,
            bool interactionSuppressed,
            bool standardTooltipsEnabled,
            bool shaderHintsEnabled,
            bool shiftPressed,
            bool hasStandardText,
            bool hasShaderHintText)
        {
            if (!hovered || interactionSuppressed)
                return TooltipDisplayKind.None;

            if (shaderHintsEnabled && shiftPressed && hasShaderHintText)
                return TooltipDisplayKind.ShaderHint;

            return standardTooltipsEnabled && hasStandardText
                ? TooltipDisplayKind.Standard
                : TooltipDisplayKind.None;
        }
    }
}
