using MaterialEditorAPI;

internal static class Program
{
    private static int Main()
    {
        try
        {
            SharedDefaultsAndShaderOverridesMerge();
            ReferencesAndWhitespaceAreResolved();
            InvalidCatalogsAreRejected();
            ShaderHintDisplayPolicyIsIndependentOfStandardTooltips();
            Console.WriteLine("Material Editor metadata regression tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void SharedDefaultsAndShaderOverridesMerge()
    {
        var shared = ShaderTooltipCatalogParser.Parse(
            @"<MaterialEditorTooltips SchemaVersion=""1"">
                <TooltipSet Id=""common"">
                  <Category Name=""Lighting"">Common lighting</Category>
                  <Property Name=""MainColor"">Common color</Property>
                </TooltipSet>
                <UseTooltipSet Ref=""common"" />
              </MaterialEditorTooltips>");
        var shaderSpecific = ShaderTooltipCatalogParser.Parse(
            @"<MaterialEditorTooltips SchemaVersion=""1"">
                <Shader Name=""xukmi/HairPlus"">
                  <Tooltip>Hair shader</Tooltip>
                  <Property Name=""MainColor"">Hair color</Property>
                </Shader>
              </MaterialEditorTooltips>");

        shared.Merge(shaderSpecific);
        var resolved = shared.ResolveShader("xukmi/HairPlus");

        Equal("Hair shader", resolved.TooltipText, "shader tooltip");
        Equal(
            "Common lighting",
            resolved.CategoryTooltips["Lighting"],
            "shared category tooltip");
        Equal(
            "Hair color",
            resolved.PropertyTooltips["MainColor"],
            "shader property override");
    }

    private static void ReferencesAndWhitespaceAreResolved()
    {
        var warnings = new List<string>();
        var catalog = ShaderTooltipCatalogParser.Parse(
            @"<MaterialEditorTooltips SchemaVersion=""1"">
                <TooltipSet Id=""common"">
                  <Property Name=""RimStrength"">
                    Controls
                    rim strength
                  </Property>
                </TooltipSet>
                <Shader Name=""Test/Shader"">
                  <UseTooltipSet Ref=""common"" />
                  <Property Name=""RimPower"" Ref=""RimStrength"" />
                </Shader>
              </MaterialEditorTooltips>",
            warnings.Add);

        var resolved = catalog.ResolveShader("Test/Shader");
        Equal(
            "Controls\nrim strength",
            resolved.PropertyTooltips["RimPower"],
            "property reference");
        Equal(0, warnings.Count, "warning count");
    }

    private static void InvalidCatalogsAreRejected()
    {
        Throws<FormatException>(
            () => ShaderTooltipCatalogParser.Parse(
                "<WrongRoot SchemaVersion=\"1\" />"),
            "invalid root");
        Throws<FormatException>(
            () => ShaderTooltipCatalogParser.Parse(
                "<MaterialEditorTooltips SchemaVersion=\"2\" />"),
            "invalid schema version");
        Throws<ArgumentException>(
            () => ShaderTooltipCatalogParser.Parse(" "),
            "empty catalog");
    }

    private static void ShaderHintDisplayPolicyIsIndependentOfStandardTooltips()
    {
        Equal(
            TooltipDisplayKind.ShaderHint,
            ResolveTooltip(
                standardTooltipsEnabled: false,
                shaderHintsEnabled: true,
                shiftPressed: true,
                hasStandardText: true,
                hasShaderHintText: true),
            "Shift hint with standard tooltips disabled");
        Equal(
            TooltipDisplayKind.Standard,
            ResolveTooltip(
                standardTooltipsEnabled: true,
                shaderHintsEnabled: true,
                shiftPressed: false,
                hasStandardText: true,
                hasShaderHintText: true),
            "standard tooltip without Shift");
        Equal(
            TooltipDisplayKind.None,
            ResolveTooltip(
                standardTooltipsEnabled: false,
                shaderHintsEnabled: true,
                shiftPressed: false,
                hasStandardText: true,
                hasShaderHintText: true),
            "disabled standard tooltip without Shift");
        Equal(
            TooltipDisplayKind.Standard,
            ResolveTooltip(
                standardTooltipsEnabled: true,
                shaderHintsEnabled: false,
                shiftPressed: true,
                hasStandardText: true,
                hasShaderHintText: true),
            "disabled shader hints fall back to standard tooltip");
        Equal(
            TooltipDisplayKind.None,
            TooltipDisplayPolicy.Resolve(
                hovered: true,
                interactionSuppressed: true,
                standardTooltipsEnabled: true,
                shaderHintsEnabled: true,
                shiftPressed: true,
                hasStandardText: true,
                hasShaderHintText: true),
            "dragging suppresses all tooltips");
        Equal(
            TooltipDisplayKind.None,
            TooltipDisplayPolicy.Resolve(
                hovered: false,
                interactionSuppressed: false,
                standardTooltipsEnabled: true,
                shaderHintsEnabled: true,
                shiftPressed: true,
                hasStandardText: true,
                hasShaderHintText: true),
            "non-hovered target");
    }

    private static TooltipDisplayKind ResolveTooltip(
        bool standardTooltipsEnabled,
        bool shaderHintsEnabled,
        bool shiftPressed,
        bool hasStandardText,
        bool hasShaderHintText)
    {
        return TooltipDisplayPolicy.Resolve(
            hovered: true,
            interactionSuppressed: false,
            standardTooltipsEnabled,
            shaderHintsEnabled,
            shiftPressed,
            hasStandardText,
            hasShaderHintText);
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                $"{name}: expected '{expected}', got '{actual}'.");
    }

    private static void Throws<TException>(Action action, string name)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}.");
    }
}
