#if !PH
using KKAPI.Utilities;
using Studio;
using System;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorUI;

namespace MaterialEditorAPI
{
    internal static class TimelineCompatibilityHelper
    {
        internal static void PopulateTimeline()
        {
            if (!TimelineCompatibility.IsTimelineAvailable()) return;

            //Renderer Enabled
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "targetEnabled",
                   name: "Enabled",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => parameter.enabled = leftValue,
                   interpolateAfter: null,
                   getValue: (oci, parameter) => GetRenderer(oci, parameter.NameFormatted()).enabled,
                   readValueFromXml: (parameter, node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString(value)),
                   getParameter: (oci) => GetRenderer(oci, selectedInterpolable.RendererName),
                   readParameterFromXml: (oci, node) => GetRenderer(oci, node.Attributes["parameter"].Value),
                   writeParameterToXml: (oci, writer, parameter) => writer.WriteAttributeString("parameter", parameter.NameFormatted()),
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is Renderer && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.NameFormatted()}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Renderer)
               );

            //Renderer ShadowCastingMode
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "shadowCastingMode",
                   name: "Shadow Casting Mode",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => parameter.shadowCastingMode = leftValue,
                   interpolateAfter: null,
                   getValue: (oci, parameter) => GetRenderer(oci, parameter.NameFormatted()).shadowCastingMode,
                   readValueFromXml: (parameter, node) => (ShadowCastingMode)XmlConvert.ToInt32(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", ((int)value).ToString()),
                   getParameter: (oci) => GetRenderer(oci, selectedInterpolable.RendererName),
                   readParameterFromXml: (oci, node) => GetRenderer(oci, node.Attributes["parameter"].Value),
                   writeParameterToXml: (oci, writer, parameter) => writer.WriteAttributeString("parameter", parameter.NameFormatted()),
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is Renderer && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.NameFormatted()}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Renderer)
               );

            //Renderer ReceiveShadows
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "receiveShadows",
                   name: "Receive Shadows",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => parameter.receiveShadows = leftValue,
                   interpolateAfter: null,
                   getValue: (oci, parameter) => GetRenderer(oci, parameter.NameFormatted()).receiveShadows,
                   readValueFromXml: (parameter, node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString((bool)value)),
                   getParameter: (oci) => GetRenderer(oci, selectedInterpolable.RendererName),
                   readParameterFromXml: (oci, node) => GetRenderer(oci, node.Attributes["parameter"].Value),
                   writeParameterToXml: (oci, writer, parameter) => writer.WriteAttributeString("parameter", parameter.NameFormatted()),
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is Renderer && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.NameFormatted()}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Renderer)
               );

            //Shader
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "shader",
                   name: "Shader",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetShader(parameter.go, parameter.materialName, leftValue),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial().shader.NameFormatted(),
                   readValueFromXml: (parameter, node) => node.Attributes["value"].Value,
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value),
                   getParameter: GetMaterialInfoParameter,
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Shader)
               );

            //Shader RenderQueue
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "renderQueue",
                   name: "Render Queue",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetRenderQueue(parameter.go, parameter.materialName, leftValue),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial().renderQueue,
                   readValueFromXml: (parameter, node) => XmlConvert.ToInt32(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value.ToString()),
                   getParameter: GetMaterialInfoParameter,
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Shader)
               );

            //Texture value
            //Using this interpolable explodes scene filesize due to needing to convert an image to base64 to save it
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "textureProperty",
                   name: "Texture Property (DON'T USE UNLESS YOU REALLY NEED TO!!!!)",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetTexture(parameter.go, parameter.materialName, parameter.propertyName, leftValue),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial().GetTexture($"_{parameter.propertyName}"),
                   readValueFromXml: (parameter, node) =>
                   {
                       var texture = new Texture2D(1, 1);
                       texture.LoadImage(Convert.FromBase64String(node.Attributes["X"].Value));
                       return texture;
                   },
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", Convert.ToBase64String(value.ToTexture2D().EncodeToPNG())),
                   getParameter: GetMaterialInfoParameter,
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"Texture Property: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.TextureProperty)
               );

            //Texture scale value
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "textureScaleProperty",
                   name: "Texture Scale Property",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetTextureScale(parameter.go, parameter.materialName, parameter.propertyName, Vector2.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial().GetTextureScale($"_{parameter.propertyName}"),
                   readValueFromXml: (parameter, node) =>
                   {
                       return new Vector2(
                           XmlConvert.ToSingle(node.Attributes["X"].Value),
                           XmlConvert.ToSingle(node.Attributes["Y"].Value)
                       );
                   },
                   writeValueToXml: (parameter, writer, value) =>
                   {
                       writer.WriteAttributeString("X", XmlConvert.ToString(value.x));
                       writer.WriteAttributeString("Y", XmlConvert.ToString(value.y));
                   },
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   getParameter: GetMaterialInfoParameter,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.TextureProperty)
               );

            //Texture offset value
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "textureOffsetProperty",
                   name: "Texture Offset Property",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetTextureOffset(parameter.go, parameter.materialName, parameter.propertyName, Vector2.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial().GetTextureOffset($"_{parameter.propertyName}"),
                   readValueFromXml: (parameter, node) =>
                   {
                       return new Vector2(
                           XmlConvert.ToSingle(node.Attributes["X"].Value),
                           XmlConvert.ToSingle(node.Attributes["Y"].Value)
                       );
                   },
                   writeValueToXml: (parameter, writer, value) =>
                   {
                       writer.WriteAttributeString("X", XmlConvert.ToString(value.x));
                       writer.WriteAttributeString("Y", XmlConvert.ToString(value.y));
                   },
                   getParameter: GetMaterialInfoParameter,
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.TextureProperty)
               );

            //Color value
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "colorProperty",
                   name: "Color Property",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetColor(parameter.go, parameter.materialName, parameter.propertyName, Color.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial().GetColor($"_{parameter.propertyName}"),
                   readValueFromXml: (parameter, node) =>
                   {
                       return new Color(
                           XmlConvert.ToSingle(node.Attributes["R"].Value),
                           XmlConvert.ToSingle(node.Attributes["G"].Value),
                           XmlConvert.ToSingle(node.Attributes["B"].Value),
                           XmlConvert.ToSingle(node.Attributes["A"].Value)
                       );
                   },
                   writeValueToXml: (parameter, writer, value) =>
                   {
                       writer.WriteAttributeString("R", XmlConvert.ToString(value.r));
                       writer.WriteAttributeString("G", XmlConvert.ToString(value.g));
                       writer.WriteAttributeString("B", XmlConvert.ToString(value.b));
                       writer.WriteAttributeString("A", XmlConvert.ToString(value.a));
                   },
                   getParameter: GetMaterialInfoParameter,
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.ColorProperty)
               );

            //Float value
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "floatProperty",
                   name: "Float Property",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetFloat(parameter.go, parameter.materialName, parameter.propertyName, Mathf.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial().GetFloat($"_{parameter.propertyName}"),
                   readValueFromXml: (parameter, node) => XmlConvert.ToSingle(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value.ToString()),
                   getParameter: GetMaterialInfoParameter,
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.FloatProperty)
               );
        }

        private static GameObject GetGameObjectFromOci(ObjectCtrlInfo oci)
        {
            if (oci is OCIItem ociItem)
                return ociItem.objectItem;
            else if (oci is OCIChar ociChar)
                return ociChar.charInfo.gameObject;
            return null;
        }

        private static Renderer GetRenderer(ObjectCtrlInfo oci, string name)
        {
            GameObject _go = GetGameObjectFromOci(oci);
            return GetRendererList(_go).First(x => x.NameFormatted() == name);
        }

        private static MaterialInfo GetMaterialInfoParameter(ObjectCtrlInfo oci)
        {
            return new MaterialInfo(oci, selectedInterpolable.MaterialName, selectedInterpolable.PropertyName);
        }

        private static void WriteMaterialInfoXml(ObjectCtrlInfo oci, XmlTextWriter writer, MaterialInfo parameter)
        {
            writer.WriteAttributeString("materialName", parameter.materialName);
            writer.WriteAttributeString("propertyName", parameter.propertyName);
        }
        private static MaterialInfo ReadMaterialInfoXml(ObjectCtrlInfo oci, XmlNode node)
        {
            return new MaterialInfo(oci, node.Attributes["materialName"].Value, node.Attributes["propertyName"].Value);
        }

        private static bool IsCompatibleWithTarget(ItemInfo.RowItemType rowtype)
        {
            if (selectedInterpolable != null && selectedInterpolable.RowType == rowtype)
                if (rowtype == ItemInfo.RowItemType.Renderer && !selectedInterpolable.RendererName.IsNullOrEmpty())
                    return true;
                else if (rowtype == ItemInfo.RowItemType.Shader && !selectedInterpolable.MaterialName.IsNullOrEmpty())
                    return true;
                else if ((rowtype == ItemInfo.RowItemType.TextureProperty || rowtype == ItemInfo.RowItemType.ColorProperty || rowtype == ItemInfo.RowItemType.FloatProperty) && !selectedInterpolable.MaterialName.IsNullOrEmpty() && !selectedInterpolable.PropertyName.IsNullOrEmpty())
                    return true;
            return false;
        }

        private class MaterialInfo
        {
            public GameObject go;
            public string materialName;
            public string propertyName;
            private readonly int _hashCode;

            public MaterialInfo(ObjectCtrlInfo oci, string materialName, string propertyName)
            {
                this.go = GetGameObjectFromOci(oci);
                this.materialName = materialName;
                this.propertyName = propertyName;

                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + this.materialName.GetHashCode();
                    this._hashCode = hash * 31 + this.propertyName.GetHashCode();
                }
            }

            public Material GetMaterial()
            {
                foreach (var rend in GetRendererList(go))
                    foreach (var mat in GetMaterials(go, rend))
                        if (mat.NameFormatted() == materialName)
                            return mat;
                return null;
            }

            public override int GetHashCode()
            {
                return this._hashCode;
            }
        }
    }
}
#endif