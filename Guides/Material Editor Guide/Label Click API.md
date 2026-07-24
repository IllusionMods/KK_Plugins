# Material Editor Label Click API

Material Editor exposes semantic label click notifications so companion plugins do not need to search the Unity UI hierarchy, add `Button` components, or patch private UI methods.

## Registration

Register once when the companion plugin starts and unregister when it is destroyed:

```csharp
using MaterialEditorAPI;

private void Awake()
{
    MaterialEditorUI.RegisterLabelClickHandler(OnMaterialEditorLabelClicked);
}

private void OnDestroy()
{
    MaterialEditorUI.UnregisterLabelClickHandler(OnMaterialEditorLabelClicked);
}

private static void OnMaterialEditorLabelClicked(MaterialEditorLabelClickEventArgs args)
{
    switch (args.LabelType)
    {
        case MaterialEditorLabelType.Renderer:
            SetRendererFilter(args.Name);
            break;
        case MaterialEditorLabelType.Material:
            SetMaterialFilter(args.Name);
            break;
        case MaterialEditorLabelType.Shader:
            SetShaderFilter(args.Name, args.ShiftPressed);
            break;
        case MaterialEditorLabelType.FloatProperty:
        case MaterialEditorLabelType.KeywordProperty:
        case MaterialEditorLabelType.ShaderRenderQueue:
            SetFloatProperty(args.Name);
            break;
        case MaterialEditorLabelType.ColorProperty:
            SetColorProperty(args.Name);
            break;
        case MaterialEditorLabelType.TextureProperty:
            SetTextureProperty(args.Name);
            break;
        case MaterialEditorLabelType.TextureOffsetScale:
            ReadTextureTransform(args.Material, args.Name);
            break;
    }
}
```

Registering the same delegate more than once has no effect. Exceptions from one handler are logged and do not prevent other handlers or Material Editor from processing the click.

## Event Context

`MaterialEditorLabelClickEventArgs` contains:

- `LabelType`: the semantic row or control represented by the click.
- `Name`: the current renderer, material, shader, or property name. Shader clicks report the selected shader, not the literal `Shader` caption.
- `GameObject` and `Data`: the values used to populate the current Material Editor window.
- `Renderer`, `Material`, and `Projector`: the concrete target when one applies to the clicked row.
- `PointerEventData`: the Unity pointer event.
- `ShiftPressed`, `ControlPressed`, and `AltPressed`: modifier key snapshots from the click frame.

Handlers must not retain Unity objects after the edited object or Material Editor window has been destroyed.

## Mass Shader Editor Compatibility

The API covers every Material Editor label currently hooked by Mass Shader Editor (MSE):

| Existing MSE UI hook | Label API replacement |
| --- | --- |
| `RendererText` | `Renderer` with the renderer name |
| `MaterialText` | `Material` with the material name |
| `ShaderLabel` plus a search for `ShaderDropdown` | `Shader` with the current shader name |
| `ShaderRenderQueueLabel` | `ShaderRenderQueue` with `Render Queue` |
| `TextureLabel` | `TextureProperty` with the property name |
| `OffsetScaleLabel` / `OffsetXText` plus child input-field searches | `TextureOffsetScale` with the owning `Material` and property name |
| `ColorLabel` | `ColorProperty` with the property name |
| `FloatLabel` | `FloatProperty` with the property name |
| `KeywordLabel` | `KeywordProperty` with the property name |

MSE can read texture offset and scale directly from `args.Material` and `args.Name`, so it no longer needs to inspect Material Editor input fields. Shift-click shader behavior can use `args.ShiftPressed`.

The current MSE binary still uses its existing hooks. It must be updated to register this API before those hooks can be removed from MSE itself.
