using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class PropertyDescriptor
    {
        internal PropertyDescriptor(
            GameObject gameObject,
            object data,
            Material material,
            Projector projector,
            string materialName,
            ShaderPropertyData definition,
            string category)
        {
            GameObject = gameObject;
            Data = data;
            Material = material;
            Projector = projector;
            MaterialName = materialName;
            Name = definition.Name;
            DisplayName = definition.Name;
            Type = definition.Type;
            MinValue = definition.MinValue;
            MaxValue = definition.MaxValue;
            PublicDescriptor = new MaterialEditorPropertyDescriptor(
                definition.Name,
                definition.Name,
                GetEditorId(definition.Type))
            {
                PropertyName = definition.Name,
                Category = category ?? string.Empty,
                Minimum = definition.MinValue,
                Maximum = definition.MaxValue
            };
        }

        internal PropertyDescriptor(
            GameObject gameObject,
            object data,
            Material material,
            Projector projector,
            string materialName,
            MaterialEditorPropertyDescriptor descriptor,
            ShaderPropertyType type)
        {
            GameObject = gameObject;
            Data = data;
            Material = material;
            Projector = projector;
            MaterialName = materialName;
            Name = string.IsNullOrEmpty(descriptor.PropertyName)
                ? descriptor.Id
                : descriptor.PropertyName;
            DisplayName = string.IsNullOrEmpty(descriptor.DisplayName)
                ? descriptor.Id
                : descriptor.DisplayName;
            Type = type;
            MinValue = descriptor.Minimum;
            MaxValue = descriptor.Maximum;
            PublicDescriptor = descriptor;
        }

        internal GameObject GameObject { get; }
        internal object Data { get; }
        internal Material Material { get; }
        internal Projector Projector { get; }
        internal string MaterialName { get; }
        internal string Name { get; }
        internal string DisplayName { get; }
        internal ShaderPropertyType Type { get; }
        internal float? MinValue { get; }
        internal float? MaxValue { get; }
        internal MaterialEditorPropertyDescriptor PublicDescriptor { get; }

        private static string GetEditorId(ShaderPropertyType type)
        {
            switch (type)
            {
                case ShaderPropertyType.Texture:
                    return MaterialEditorPropertyEditorIds.Texture;
                case ShaderPropertyType.Color:
                    return MaterialEditorPropertyEditorIds.Color;
                case ShaderPropertyType.Float:
                    return MaterialEditorPropertyEditorIds.Float;
                case ShaderPropertyType.Keyword:
                    return MaterialEditorPropertyEditorIds.Boolean;
                default:
                    return string.Empty;
            }
        }
    }

    internal sealed class PropertyRowModelFactory
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorPresentationActions _actions;

        internal PropertyRowModelFactory(
            MaterialEditService editService,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _actions = actions;
        }

        internal IEnumerable<RowModel> Create(PropertyDescriptor descriptor)
        {
            switch (descriptor.Type)
            {
                case ShaderPropertyType.Texture:
                    return CreateTextureRows(descriptor);
                case ShaderPropertyType.Color:
                    return new[] { CreateColorRow(descriptor) };
                case ShaderPropertyType.Float:
                    return new[] { CreateFloatRow(descriptor) };
                case ShaderPropertyType.Keyword:
                    return new[] { CreateKeywordRow(descriptor) };
                default:
                    return new RowModel[0];
            }
        }

        internal IEnumerable<RowModel> CreateExtension(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor)
        {
            var editor = MaterialEditorExtensionRegistry.CreatePropertyEditor(
                context,
                descriptor);
            if (editor == null)
                return new RowModel[0];

            var floatEditor = editor as MaterialEditorFloatPropertyEditor;
            if (floatEditor != null)
                return new[] { CreateExtensionFloatRow(context, descriptor, floatEditor) };

            var colorEditor = editor as MaterialEditorColorPropertyEditor;
            if (colorEditor != null)
                return new[] { CreateExtensionColorRow(context, descriptor, colorEditor) };

            var booleanEditor = editor as MaterialEditorBooleanPropertyEditor;
            if (booleanEditor != null)
                return new[] { CreateExtensionBooleanRow(context, descriptor, booleanEditor) };

            var textureEditor = editor as MaterialEditorTexturePropertyEditor;
            if (textureEditor != null)
                return CreateExtensionTextureRows(context, descriptor, textureEditor);

            MaterialEditorPluginBase.Logger?.LogWarning(
                $"Property editor '{descriptor.EditorId}' returned an unsupported editor type.");
            return new RowModel[0];
        }

        private IEnumerable<RowModel> CreateTextureRows(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var projector = descriptor.Projector;
            var propertyName = descriptor.Name;

            var textureItem = new TexturePropertyRowModel(descriptor.DisplayName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                PropertyName = propertyName,
                PublicDescriptor = descriptor.PublicDescriptor,
                Changed = !_editService.GetMaterialTextureValueOriginal(data, material, propertyName, gameObject),
                Exists = material.GetTexture($"_{propertyName}") != null,
                Export = () => _actions.ExportTexture(material, propertyName),
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.TextureProperty,
                        descriptor.MaterialName,
                        propertyName,
                        string.Empty)
            };
            textureItem.Import = () =>
                _actions.ImportTexture(textureItem, gameObject, data, material, propertyName);
            textureItem.Reset = () =>
                _editService.RemoveMaterialTexture(data, material, propertyName, gameObject);

            var textureOffset = material.GetTextureOffset($"_{propertyName}");
            var textureOffsetOriginal =
                _editService.GetMaterialTextureOffsetOriginal(data, material, propertyName, gameObject)
                ?? textureOffset;
            var textureScale = material.GetTextureScale($"_{propertyName}");
            var textureScaleOriginal =
                _editService.GetMaterialTextureScaleOriginal(data, material, propertyName, gameObject)
                ?? textureScale;

            var textureOffsetScaleItem = new TextureOffsetScaleRowModel()
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                PropertyName = propertyName,
                PublicDescriptor = descriptor.PublicDescriptor,
                Offset = textureOffset,
                OriginalOffset = textureOffsetOriginal,
                OffsetOnChange = value =>
                    _editService.SetMaterialTextureOffset(data, material, propertyName, value, gameObject),
                OffsetOnReset = () =>
                    _editService.RemoveMaterialTextureOffset(data, material, propertyName, gameObject),
                Scale = textureScale,
                OriginalScale = textureScaleOriginal,
                ScaleOnChange = value =>
                    _editService.SetMaterialTextureScale(data, material, propertyName, value, gameObject),
                ScaleOnReset = () =>
                    _editService.RemoveMaterialTextureScale(data, material, propertyName, gameObject)
            };

            return new RowModel[] { textureItem, textureOffsetScaleItem };
        }

        private ColorPropertyRowModel CreateColorRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var value = material.GetColor($"_{propertyName}");
            var original =
                _editService.GetMaterialColorPropertyValueOriginal(data, material, propertyName, gameObject)
                ?? value;

            return new ColorPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = descriptor.Projector,
                PropertyName = propertyName,
                PublicDescriptor = descriptor.PublicDescriptor,
                Value = value,
                OriginalValue = original,
                ValueOnChange = newValue =>
                    _editService.SetMaterialColorProperty(data, material, propertyName, newValue, gameObject),
                ValueOnReset = () =>
                    _editService.RemoveMaterialColorProperty(data, material, propertyName, gameObject),
                Edit = (title, currentValue, onChanged) =>
                    _actions.EditColor(data, material, $"Material Editor - {title}", currentValue, onChanged),
                SetToPalette = (title, currentValue) =>
                    _actions.SetColorToPalette(data, material, $"Material Editor - {title}", currentValue),
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.ColorProperty,
                        descriptor.MaterialName,
                        propertyName,
                        string.Empty)
            };
        }

        private FloatPropertyRowModel CreateFloatRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var value = material.GetFloat($"_{propertyName}");
            var original =
                _editService.GetMaterialFloatPropertyValueOriginal(data, material, propertyName, gameObject)
                ?? value;

            return CreateFloatRow(
                descriptor,
                value,
                original,
                descriptor.MinValue,
                descriptor.MaxValue,
                () => _actions.SelectInterpolable(
                    gameObject,
                    RowModel.RowItemType.FloatProperty,
                    descriptor.MaterialName,
                    propertyName,
                    string.Empty),
                newValue =>
                    _editService.SetMaterialFloatProperty(data, material, propertyName, newValue, gameObject),
                () => _editService.RemoveMaterialFloatProperty(data, material, propertyName, gameObject));
        }

        internal static FloatPropertyRowModel CreateFloatRow(
            PropertyDescriptor descriptor,
            float value,
            float original,
            float? minValue,
            float? maxValue,
            System.Action selectInterpolable,
            System.Action<float> changeValue,
            System.Action resetValue)
        {
            var item = new FloatPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = descriptor.GameObject,
                Data = descriptor.Data,
                Material = descriptor.Material,
                Projector = descriptor.Projector,
                PropertyName = descriptor.Name,
                PublicDescriptor = descriptor.PublicDescriptor,
                Value = value,
                OriginalValue = original,
                SelectInterpolable = selectInterpolable,
                ValueOnChange = changeValue,
                ValueOnReset = resetValue
            };
            if (minValue != null)
                item.SliderMinimum = minValue.Value;
            if (maxValue != null)
                item.SliderMaximum = maxValue.Value;
            return item;
        }

        private KeywordPropertyRowModel CreateKeywordRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var value = material.IsKeywordEnabled($"_{propertyName}");
            var original =
                _editService.GetMaterialKeywordPropertyValueOriginal(data, material, propertyName, gameObject)
                ?? value;

            return new KeywordPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = descriptor.Projector,
                PropertyName = propertyName,
                PublicDescriptor = descriptor.PublicDescriptor,
                Value = value,
                OriginalValue = original,
                ValueOnChange = newValue =>
                    _editService.SetMaterialKeywordProperty(data, material, propertyName, newValue, gameObject),
                ValueOnReset = () =>
                    _editService.RemoveMaterialKeywordProperty(data, material, propertyName, gameObject)
            };
        }

        private static FloatPropertyRowModel CreateExtensionFloatRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorFloatPropertyEditor editor)
        {
            return new FloatPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                SliderMinimum = editor.Minimum,
                SliderMaximum = editor.Maximum,
                SelectInterpolable = editor.SelectInterpolable ?? (() => { }),
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            };
        }

        private ColorPropertyRowModel CreateExtensionColorRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorColorPropertyEditor editor)
        {
            return new ColorPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                SelectInterpolable = editor.SelectInterpolable ?? (() => { }),
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset,
                Edit = (title, value, changed) =>
                    _actions.EditColor(
                        context.Target.Data,
                        context.Target.Material,
                        $"Material Editor - {title}",
                        value,
                        changed),
                SetToPalette = (title, value) =>
                    _actions.SetColorToPalette(
                        context.Target.Data,
                        context.Target.Material,
                        $"Material Editor - {title}",
                        value)
            };
        }

        private static KeywordPropertyRowModel CreateExtensionBooleanRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorBooleanPropertyEditor editor)
        {
            return new KeywordPropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            };
        }

        private static IEnumerable<RowModel> CreateExtensionTextureRows(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorTexturePropertyEditor editor)
        {
            var select = editor.SelectInterpolable ?? (() => { });
            var texture = new TexturePropertyRowModel(descriptor.DisplayName)
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Changed = editor.Changed,
                Exists = editor.Exists,
                SelectInterpolable = select,
                Export = editor.Export ?? (() => { }),
                Import = editor.Import ?? (() => { }),
                Reset = editor.Reset ?? (() => { })
            };
            var transform = new TextureOffsetScaleRowModel
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Offset = editor.Offset,
                OriginalOffset = editor.OriginalOffset,
                OffsetOnChange = editor.OffsetChanged ?? (_ => { }),
                OffsetOnReset = editor.ResetOffset ?? (() => { }),
                Scale = editor.Scale,
                OriginalScale = editor.OriginalScale,
                ScaleOnChange = editor.ScaleChanged ?? (_ => { }),
                ScaleOnReset = editor.ResetScale ?? (() => { })
            };
            return new RowModel[] { texture, transform };
        }
    }
}
