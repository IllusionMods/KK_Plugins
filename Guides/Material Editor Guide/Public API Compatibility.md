# Material Editor Public API Compatibility

This document defines the compatibility boundary for the Material Editor modernization work.

## Baseline

The initial API baseline was captured from upstream commit `2534d592b971b472a6bc44002ef6961ea0092e65`.

The authoritative machine-readable API list is:

- `src/MaterialEditor.API/PublicAPI.Shipped.txt`

It contains the 234 public symbols emitted by `src/MaterialEditor.API/API.MaterialEditor.csproj` at the baseline commit. `PublicAPI.Unshipped.txt` records reviewed additions that have not been included in a release yet.

`Microsoft.CodeAnalysis.PublicApiAnalyzers` runs when the API project is built. The build fails when a public symbol is added without being declared, when a shipped symbol is removed, or when the API files are missing or invalid.

Run the compatibility check with:

```text
dotnet build src/MaterialEditor.API/API.MaterialEditor.csproj -c Release
```

## Compatibility Scope

The frozen surface is the `MaterialEditorAPI` reference assembly produced by the API project. The same shared source is compiled into the game-specific Material Editor assemblies.

The baseline currently contains these public types:

- `MaterialEditorAPI.MaterialAPI`
- `MaterialEditorAPI.MaterialAPI.ProjectorProperties`
- `MaterialEditorAPI.MaterialAPI.RendererProperties`
- `MaterialEditorAPI.MaterialAPI.ShaderPropertyType`
- `MaterialEditorAPI.CopyContainer`
- `MaterialEditorAPI.CopyContainer.MaterialColorProperty`
- `MaterialEditorAPI.CopyContainer.MaterialFloatProperty`
- `MaterialEditorAPI.CopyContainer.MaterialKeywordProperty`
- `MaterialEditorAPI.CopyContainer.MaterialShader`
- `MaterialEditorAPI.CopyContainer.MaterialTextureProperty`
- `MaterialEditorAPI.CopyContainer.ProjectorProperty`
- `MaterialEditorAPI.MaterialEditorPluginBase`
- `MaterialEditorAPI.MaterialEditorPluginBase.ShaderData`
- `MaterialEditorAPI.MaterialEditorPluginBase.ShaderPropertyData`
- `MaterialEditorAPI.MaterialEditorUI`
- `MaterialEditorAPI.Export`
- `MaterialEditorAPI.FloatLabelDragTrigger`

Reviewed additions currently recorded in `PublicAPI.Unshipped.txt` include the semantic extension surface:

- `MaterialEditorExtensionApi` capability and version queries
- renderer, material, shader, and property selection events
- `MaterialEditorTargetContext` and `MaterialEditorPropertyContext`
- custom property descriptor providers and semantic property editor factories
- `MaterialEditorEditService`, a stable facade over repository-backed edits

These APIs deliberately do not expose the internal row model, row view, binder registry, or concrete Unity controls.

The exact constructors, methods, properties, fields, enum values, optional parameter defaults, return types, and accessibility are listed in `PublicAPI.Shipped.txt`.

## Compatibility Rules

The modernization work must preserve the following unless an explicitly approved breaking release says otherwise:

- Existing public types and members remain present with binary-compatible signatures.
- Existing enum member numeric values do not change.
- Existing optional parameter defaults do not change.
- Public types do not move to a different namespace or assembly.
- Public or protected members on `MaterialEditorPluginBase` and `MaterialEditorUI` remain available even when their implementation moves to new internal services.
- New APIs are additive and are first declared in `PublicAPI.Unshipped.txt`.
- Public API removals are not allowed as part of the UI modernization.
- Registration methods document ownership and disposal behavior; changing callback order or lifetime semantics requires compatibility review.
- Capability flags and enum numeric values are append-only.
- New property editor families are added as new semantic editor types rather than by exposing internal controls.

## Not a Public Contract

The following implementation details are not included in the frozen API surface:

- Unity UI hierarchy names, child order, layout values, and concrete row objects.
- `RowModel`, `RowView`, `RowBinder`, and their family-specific implementations.
- Internal UI types such as `ItemInfo`, `ItemTemplate`, `ListEntry`, and `VirtualList`.
- Private and internal storage implementation details.
- Maker and Studio scene lifecycle timing beyond behavior exposed through the public API.

Game-specific implementation assemblies also expose legacy public types that are not part of the standalone `MaterialEditorAPI` reference assembly. They should be reviewed before changing accessibility or signatures, but they are not declared extension points by this baseline.

## Updating the API

For an additive API change:

1. Add the public type or member.
2. Add the analyzer-provided signature to `PublicAPI.Unshipped.txt`.
3. Document the intended behavior and supported games.
4. Build the API project and all affected game targets.
5. Move reviewed entries to `PublicAPI.Shipped.txt` when preparing a release.

Extension API usage and behavioral semantics are documented in [Extension API.md](Extension%20API.md).

Do not silence compatibility diagnostics globally. Any suppression or removed API marker requires an explicit compatibility review in the pull request.
