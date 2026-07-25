# Shader Tooltip Catalog Specification and Authoring Guide

Material Editor tooltip catalogs let shader authors describe shaders, property
categories, and individual properties without putting large blocks of text in
the Sideloader manifest. A catalog is an XML `TextAsset` stored in an
AssetBundle. It does not require a `MonoBehaviour` or a prefab component.

This document defines schema version 1. Tooltip catalogs require a Material
Editor build that reports the `PropertyTooltips` extension capability. Older
Material Editor versions ignore the unknown `TooltipCatalog` manifest element;
the shader still loads, but the catalog tooltips are not displayed.

## Quick Start

1. Copy [shader_tooltip_catalog_template.xml](shader_tooltip_catalog_template.xml)
   into the Unity shader project, for example:

   ```text
   Assets/
     MaterialEditor/
       Tooltips/
         material_editor_tooltips.xml
   ```

2. Let Unity import the XML file as a `TextAsset`.
3. Assign the XML asset to an AssetBundle. It may use the same AssetBundle as
   the shader prefab.
4. Build the AssetBundle and include it in the zipmod.
5. Add a `TooltipCatalog` reference inside the manifest's `MaterialEditor`
   element.
6. Check the BepInEx log after loading the mod. Catalog errors are reported as
   warnings and never block the shader itself.

## Manifest Reference

```xml
<MaterialEditor>
  <TooltipCatalog
      AssetBundle="my_shaders/shader_assets.unity3d"
      Asset="material_editor_tooltips"/>

  <Shader
      Name="Family/Standard"
      AssetBundle="my_shaders/shader_assets.unity3d"
      Asset="family_standard">
    ...
  </Shader>
</MaterialEditor>
```

`AssetBundle` follows the same Sideloader AssetBundle path rules as the
corresponding `Shader` declaration. `Asset` is the exact Unity asset name of
the XML `TextAsset`, normally without the `.xml` extension.

A manifest may declare more than one `TooltipCatalog`. Catalogs are merged in
manifest order; metadata in a later catalog overrides matching metadata loaded
from an earlier catalog.

## Schema

The root element must be `MaterialEditorTooltips` and must declare
`SchemaVersion="1"`.

```xml
<?xml version="1.0" encoding="utf-8"?>
<MaterialEditorTooltips SchemaVersion="1">
  <TooltipSet Id="family.common">
    <Property Name="MainTex">Base color texture.</Property>
    <Property Name="Alpha">
      Opacity: 0 is transparent and 1 is opaque.
    </Property>
    <Category Name="Surface">
      Surface color, texture, and opacity controls.
    </Category>
  </TooltipSet>

  <Shader Name="Family/Standard">
    <UseTooltipSet Ref="family.common"/>
    <Tooltip>Standard shader for the Family series.</Tooltip>
  </Shader>

  <Shader Name="Family/ReversedAlpha">
    <UseTooltipSet Ref="family.common"/>
    <Property Name="Alpha">
      Reversed opacity: 0 is opaque and 1 is transparent.
    </Property>
  </Shader>
</MaterialEditorTooltips>
```

Supported elements:

| Element | Required attributes | Purpose |
|---|---|---|
| `TooltipSet` | `Id` | Defines reusable property and category text for a shader family. |
| `Shader` | `Name` | Defines metadata for one exact shader name. |
| `UseTooltipSet` | `Ref` | Applies a reusable set to the containing shader. |
| `Tooltip` | None | Defines the tooltip shown on the Shader row. |
| `Property` | `Name` | Defines the tooltip shown on a property label. |
| `Category` | `Name` | Defines the tooltip shown on a category header and navigator entry. |

Unknown elements are ignored. A missing `Id`, `Name`, or `Ref` does not define
usable metadata.

## Name Matching

Names are ordinal and case-sensitive.

- `Shader Name` must match the `Name` used by the Material Editor `Shader`
  manifest declaration.
- `Property Name` must match the Material Editor property name, not
  necessarily the raw Unity property spelling. For the common manifest entry
  `<Property Name="MainTex" Type="Texture"/>`, use `MainTex`, not `_MainTex`.
- `Category Name` must match the category displayed by Material Editor.
  Material Editor capitalizes the first character of manifest category names,
  so `Category="surface"` is displayed and matched as `Surface`.

If a name does not match exactly, the entry is simply unused.

## Reusable Sets and Override Order

A shader may apply multiple sets:

```xml
<Shader Name="Family/Advanced">
  <UseTooltipSet Ref="family.common"/>
  <UseTooltipSet Ref="family.advanced"/>
  <Property Name="Alpha">Advanced shader opacity.</Property>
</Shader>
```

Resolution order, from lowest to highest priority:

1. The first referenced `TooltipSet`.
2. Each later referenced `TooltipSet`.
3. Entries declared directly inside the `Shader`.
4. Matching metadata from a later `TooltipCatalog` in the manifest.

An override replaces the complete tooltip. Text is never concatenated. This is
important when two shaders use the same property name with opposite meanings.

## Aliases with `Ref`

A property or category can reuse already resolved text under another name:

```xml
<Shader Name="Family/Alternative">
  <UseTooltipSet Ref="family.common"/>
  <Property Name="Opacity" Ref="Alpha"/>
</Shader>
```

`Ref` is resolved against metadata already available at that point: referenced
sets and earlier entries in the same scope. Put the source entry before an
alias. If the reference cannot be resolved, Material Editor logs a warning and
skips that alias.

Explicit text takes priority over `Ref`:

```xml
<Property Name="Opacity" Ref="Alpha">
  Reversed opacity: 0 is opaque and 1 is transparent.
</Property>
```

## Text Rules

- Catalog text is English-only. Localization remains the responsibility of
  BepInEx translation plugins.
- Tooltips are plain text. Rich Text tags are not interpreted.
- Line breaks are supported. Leading and trailing whitespace on each line is
  removed while loading.
- Keep wording stable so translation plugins can use exact or regular
  expression matching.
- Do not insert runtime values, material instance names, character names, or
  other changing text.
- Keep descriptions concise. The tooltip panel is 280 UI units wide and is
  limited to 360 UI units in height; longer text is truncated.

Recommended property wording explains meaning, direction, units, and unusual
limits:

```xml
<Property Name="OutlineWidth">
  Outline width in model-space units. Higher values produce a thicker outline.
</Property>
```

Avoid repeating the visible label without adding information:

```xml
<!-- Avoid -->
<Property Name="OutlineWidth">Outline width.</Property>
```

## Failure Behaviour

The following conditions generate BepInEx warnings but do not prevent the
shader or Material Editor from loading:

- the AssetBundle or TextAsset cannot be found;
- the XML is malformed;
- the root element or schema version is invalid;
- a `TooltipSet` or alias reference cannot be resolved.

Authors should test at least one shader that uses only a shared set, one shader
with a direct override, and one alias before releasing a catalog.

## Release Checklist

- The XML file is imported by Unity as a `TextAsset`.
- The TextAsset has the expected AssetBundle name.
- The manifest `AssetBundle` path and `Asset` name match the built bundle.
- Shader, property, and category names match Material Editor exactly.
- Common text lives in `TooltipSet` rather than being copied between shaders.
- Shader-specific semantic differences use complete overrides.
- Text is concise, stable, English-only, and contains no Rich Text.
- The game log contains no tooltip catalog warnings.

See [shader_tooltip_catalog_template.xml](shader_tooltip_catalog_template.xml)
for a ready-to-copy starting file.
