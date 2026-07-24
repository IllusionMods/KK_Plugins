# Material Editor Extension API

Material Editor exposes a semantic extension API for companion plugins. The API describes targets, properties, and edit operations without exposing `RowModel`, `RowView`, binders, Unity UI controls, or hierarchy names.

## Version and Capabilities

Check capabilities before using an optional extension point:

```csharp
using MaterialEditorAPI;

if (!MaterialEditorExtensionApi.Supports(
        MaterialEditorApiCapability.SelectionEvents |
        MaterialEditorApiCapability.EditServiceFacade))
{
    return;
}

Version apiVersion = MaterialEditorExtensionApi.ApiVersion;
```

The capability check is preferred over comparing versions when a plugin needs a specific feature.

## Selection Events

Selection notifications cover renderers, materials, shaders, and properties:

```csharp
private IDisposable _selectionRegistration;

private void Awake()
{
    _selectionRegistration =
        MaterialEditorExtensionApi.RegisterSelectionHandler(OnSelection);
}

private void OnDestroy()
{
    _selectionRegistration?.Dispose();
}

private static void OnSelection(MaterialEditorSelectionEventArgs args)
{
    switch (args.Action)
    {
        case MaterialEditorSelectionAction.Selected:
            // Added to a persistent renderer/material selection.
            break;
        case MaterialEditorSelectionAction.Deselected:
            // Removed from a persistent renderer/material selection.
            break;
        case MaterialEditorSelectionAction.Activated:
            // A semantic row label was activated.
            break;
    }

    Material material = args.Context.Material;
    MaterialEditorPropertyDescriptor property = args.Property;
}
```

Renderer and material side-list toggles emit `Selected` or `Deselected`. Shader dropdown changes emit `Selected`. Semantic label clicks emit `Activated`; property activations include a descriptor when one is available.

Handlers run on the Unity main thread. Exceptions are logged and isolated so one extension cannot prevent other handlers from running.

## Edit Service Facade

`MaterialEditorEditService` routes edits through Material Editor's active storage repository. This keeps character, coordinate, and scene persistence behavior consistent with edits made by the built-in UI.

An event or property-provider context already contains a bound service:

```csharp
private static void SetOutlineWidth(MaterialEditorSelectionEventArgs args)
{
    Material material = args.Context.Material;
    if (material == null)
        return;

    args.Context.EditService.SetFloat(material, "LineWidthS", 0.5f);
}
```

Outside an event, request a facade after Material Editor initializes:

```csharp
MaterialEditorEditService edits =
    MaterialEditorExtensionApi.GetEditService(gameObject, data);

if (edits != null)
    edits.ResetShader(material);
```

The facade is bound to the supplied root `GameObject` and opaque data context. Do not reuse it for another character, coordinate, or scene object.

## Custom Property Descriptors

Descriptor providers add shader-backed properties without modifying Material Editor's XML manifests:

```csharp
private IDisposable _descriptorRegistration;

private void Awake()
{
    _descriptorRegistration =
        MaterialEditorExtensionApi.RegisterPropertyDescriptorProvider(
            Info.Metadata.GUID,
            GetProperties,
            priority: 0);
}

private static IEnumerable<MaterialEditorPropertyDescriptor> GetProperties(
    MaterialEditorPropertyContext context)
{
    if (context.ShaderName != "MyShader")
        yield break;

    yield return new MaterialEditorPropertyDescriptor(
        "RimPower",
        "Rim Power",
        MaterialEditorPropertyEditorIds.Float)
    {
        PropertyName = "RimPower",
        Category = "Extension",
        Minimum = 0f,
        Maximum = 8f
    };
}
```

Built-in editor IDs are:

- `MaterialEditorPropertyEditorIds.Float`
- `MaterialEditorPropertyEditorIds.Color`
- `MaterialEditorPropertyEditorIds.Boolean`
- `MaterialEditorPropertyEditorIds.Texture`

Built-in editors use the descriptor's `PropertyName` and the Material Editor edit-service facade, so values are persisted through the normal repository. Non-keyword shader properties are shown only when the material reports that the backing property exists.

Providers are evaluated when the Material Editor row list is rebuilt. They must be deterministic, avoid long-running work, and return new descriptors or treat returned descriptors as immutable after the call.

## Custom Property Editors

Plugins can register a reusable semantic editor without creating or locating Unity controls:

```csharp
private IDisposable _editorRegistration;
private IDisposable _descriptorRegistration;

private void Awake()
{
    _editorRegistration = MaterialEditorExtensionApi.RegisterPropertyEditor(
        Info.Metadata.GUID,
        "myplugin.opacity",
        CreateOpacityEditor);

    _descriptorRegistration =
        MaterialEditorExtensionApi.RegisterPropertyDescriptorProvider(
            Info.Metadata.GUID,
            context => new[]
            {
                new MaterialEditorPropertyDescriptor(
                    "Opacity",
                    "Plugin Opacity",
                    "myplugin.opacity")
                {
                    Category = "Extension"
                }
            });
}

private static MaterialEditorPropertyEditor CreateOpacityEditor(
    MaterialEditorPropertyContext context,
    MaterialEditorPropertyDescriptor descriptor)
{
    float value = ReadOpacity(context.Target);
    float original = ReadOriginalOpacity(context.Target);

    return new MaterialEditorFloatPropertyEditor(
        value,
        original,
        newValue => SaveOpacity(context.Target, newValue),
        () => ResetOpacity(context.Target))
    {
        Minimum = 0f,
        Maximum = 1f
    };
}
```

Factories may return `MaterialEditorFloatPropertyEditor`, `MaterialEditorColorPropertyEditor`, `MaterialEditorBooleanPropertyEditor`, or `MaterialEditorTexturePropertyEditor`. Material Editor adapts these semantic definitions to its current internal rows and controls.

Editor IDs are global and must be namespaced with the provider plugin ID. Registering an existing ID throws. Built-in IDs cannot be replaced.

Dispose editor registrations after descriptor registrations so no active descriptor refers to a removed editor.

## Compatibility Contract

The semantic contracts in this document are public API. The following remain internal implementation details and must not be patched or retained by extension plugins:

- `RowModel` and its subclasses
- `RowView`, `RowBinder`, and row handler registries
- Unity `Button`, `InputField`, `Dropdown`, and row panel instances
- UI hierarchy names, child order, and layout values

New editor families and capabilities may be added in compatible releases. Existing enum values, registrations, descriptors, event contexts, and facade method signatures follow the policy in [Public API Compatibility.md](Public%20API%20Compatibility.md).
