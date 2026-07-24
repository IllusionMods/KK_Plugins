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
            ShaderPropertyData definition)
        {
            GameObject = gameObject;
            Data = data;
            Material = material;
            Projector = projector;
            MaterialName = materialName;
            Definition = definition;
        }

        internal GameObject GameObject { get; }
        internal object Data { get; }
        internal Material Material { get; }
        internal Projector Projector { get; }
        internal string MaterialName { get; }
        internal ShaderPropertyData Definition { get; }
        internal string Name => Definition.Name;
        internal ShaderPropertyType Type => Definition.Type;
        internal float? MinValue => Definition.MinValue;
        internal float? MaxValue => Definition.MaxValue;
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

        private IEnumerable<RowModel> CreateTextureRows(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var projector = descriptor.Projector;
            var propertyName = descriptor.Name;

            var textureItem = new RowModel(RowModel.RowItemType.TextureProperty, propertyName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                PropertyName = propertyName,
                TextureChanged = !_editService.GetMaterialTextureValueOriginal(data, material, propertyName, gameObject),
                TextureExists = material.GetTexture($"_{propertyName}") != null,
                TextureOnExport = () => _actions.ExportTexture(material, propertyName),
                SelectInterpolableButtonTextureOnClick = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.TextureProperty,
                        descriptor.MaterialName,
                        propertyName,
                        string.Empty)
            };
            textureItem.TextureOnImport = () =>
                _actions.ImportTexture(textureItem, gameObject, data, material, propertyName);
            textureItem.TextureOnReset = () =>
                _editService.RemoveMaterialTexture(data, material, propertyName, gameObject);

            var textureOffset = material.GetTextureOffset($"_{propertyName}");
            var textureOffsetOriginal =
                _editService.GetMaterialTextureOffsetOriginal(data, material, propertyName, gameObject)
                ?? textureOffset;
            var textureScale = material.GetTextureScale($"_{propertyName}");
            var textureScaleOriginal =
                _editService.GetMaterialTextureScaleOriginal(data, material, propertyName, gameObject)
                ?? textureScale;

            var textureOffsetScaleItem = new RowModel(RowModel.RowItemType.TextureOffsetScale)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = projector,
                PropertyName = propertyName,
                Offset = textureOffset,
                OffsetOriginal = textureOffsetOriginal,
                OffsetOnChange = value =>
                    _editService.SetMaterialTextureOffset(data, material, propertyName, value, gameObject),
                OffsetOnReset = () =>
                    _editService.RemoveMaterialTextureOffset(data, material, propertyName, gameObject),
                Scale = textureScale,
                ScaleOriginal = textureScaleOriginal,
                ScaleOnChange = value =>
                    _editService.SetMaterialTextureScale(data, material, propertyName, value, gameObject),
                ScaleOnReset = () =>
                    _editService.RemoveMaterialTextureScale(data, material, propertyName, gameObject)
            };

            return new[] { textureItem, textureOffsetScaleItem };
        }

        private RowModel CreateColorRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var value = material.GetColor($"_{propertyName}");
            var original =
                _editService.GetMaterialColorPropertyValueOriginal(data, material, propertyName, gameObject)
                ?? value;

            return new ColorPropertyRowModel(propertyName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = descriptor.Projector,
                PropertyName = propertyName,
                ColorValue = value,
                ColorValueOriginal = original,
                ColorValueOnChange = newValue =>
                    _editService.SetMaterialColorProperty(data, material, propertyName, newValue, gameObject),
                ColorValueOnReset = () =>
                    _editService.RemoveMaterialColorProperty(data, material, propertyName, gameObject),
                ColorValueOnEdit = (title, currentValue, onChanged) =>
                    _actions.EditColor(data, material, $"Material Editor - {title}", currentValue, onChanged),
                ColorValueSetToPalette = (title, currentValue) =>
                    _actions.SetColorToPalette(data, material, $"Material Editor - {title}", currentValue),
                SelectInterpolableButtonColorOnClick = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.ColorProperty,
                        descriptor.MaterialName,
                        propertyName,
                        string.Empty)
            };
        }

        private RowModel CreateFloatRow(PropertyDescriptor descriptor)
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

        internal static RowModel CreateFloatRow(
            PropertyDescriptor descriptor,
            float value,
            float original,
            float? minValue,
            float? maxValue,
            System.Action selectInterpolable,
            System.Action<float> changeValue,
            System.Action resetValue)
        {
            var item = new FloatPropertyRowModel(descriptor.Name)
            {
                GameObject = descriptor.GameObject,
                Data = descriptor.Data,
                Material = descriptor.Material,
                Projector = descriptor.Projector,
                PropertyName = descriptor.Name,
                FloatValue = value,
                FloatValueOriginal = original,
                SelectInterpolableButtonFloatOnClick = selectInterpolable,
                FloatValueOnChange = changeValue,
                FloatValueOnReset = resetValue
            };
            if (minValue != null)
                item.FloatValueSliderMin = minValue.Value;
            if (maxValue != null)
                item.FloatValueSliderMax = maxValue.Value;
            return item;
        }

        private RowModel CreateKeywordRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var value = material.IsKeywordEnabled($"_{propertyName}");
            var original =
                _editService.GetMaterialKeywordPropertyValueOriginal(data, material, propertyName, gameObject)
                ?? value;

            return new RowModel(RowModel.RowItemType.KeywordProperty, propertyName)
            {
                GameObject = gameObject,
                Data = data,
                Material = material,
                Projector = descriptor.Projector,
                PropertyName = propertyName,
                KeywordValue = value,
                KeywordValueOriginal = original,
                KeywordValueOnChange = newValue =>
                    _editService.SetMaterialKeywordProperty(data, material, propertyName, newValue, gameObject),
                KeywordValueOnReset = () =>
                    _editService.RemoveMaterialKeywordProperty(data, material, propertyName, gameObject)
            };
        }
    }
}
