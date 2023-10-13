#if !PH
using KKAPI.Utilities;
using Studio;
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
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetRendererEnabled(parameter.GetGameObject(oci), parameter.rendererName, leftValue),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetRenderer(oci).enabled,
                   readValueFromXml: (parameter, node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString(value)),
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   getParameter: GetMaterialInfoParameter,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) => CheckIntegrity(oci, parameter, leftValue, rightValue, ItemInfo.RowItemType.Renderer),
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.rendererName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Renderer)
               );

            //Renderer ShadowCastingMode
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "shadowCastingMode",
                   name: "Shadow Casting Mode",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetRendererShadowCastingMode(parameter.GetGameObject(oci), parameter.rendererName, leftValue),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetRenderer(oci).shadowCastingMode,
                   readValueFromXml: (parameter, node) => (ShadowCastingMode)XmlConvert.ToInt32(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", ((int)value).ToString()),
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   getParameter: GetMaterialInfoParameter,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) => CheckIntegrity(oci, parameter, leftValue, rightValue, ItemInfo.RowItemType.Renderer),
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.rendererName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Renderer)
               );

            //Renderer ReceiveShadows
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "receiveShadows",
                   name: "Receive Shadows",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetRendererReceiveShadows(parameter.GetGameObject(oci), parameter.rendererName, leftValue),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetRenderer(oci).receiveShadows,
                   readValueFromXml: (parameter, node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString((bool)value)),
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   getParameter: GetMaterialInfoParameter,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) => CheckIntegrity(oci, parameter, leftValue, rightValue, ItemInfo.RowItemType.Renderer),
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.rendererName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Renderer)
               );

            //Shader
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "shader",
                   name: "Shader",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetShader(parameter.GetGameObject(oci), parameter.materialName, leftValue),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial(oci).shader.NameFormatted(),
                   readValueFromXml: (parameter, node) => node.Attributes["value"].Value,
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value),
                   getParameter: GetMaterialInfoParameter,
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) => CheckIntegrity(oci, parameter, leftValue, rightValue, ItemInfo.RowItemType.Shader),
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Shader)
               );

            //Shader RenderQueue
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "renderQueue",
                   name: "Render Queue",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetRenderQueue(parameter.GetGameObject(oci), parameter.materialName, (int)Mathf.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial(oci).renderQueue,
                   readValueFromXml: (parameter, node) => XmlConvert.ToInt32(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value.ToString()),
                   getParameter: GetMaterialInfoParameter,
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) => CheckIntegrity(oci, parameter, leftValue, rightValue, ItemInfo.RowItemType.Shader),
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.Shader)
               );

            //Texture scale value
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "textureScaleProperty",
                   name: "Texture Scale Property",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetTextureScale(parameter.GetGameObject(oci), parameter.materialName, parameter.propertyName, Vector2.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial(oci).GetTextureScale($"_{parameter.propertyName}"),
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
                   checkIntegrity: (oci, parameter, leftValue, rightValue) => CheckIntegrity(oci, parameter, leftValue, rightValue, ItemInfo.RowItemType.TextureProperty),
                   getFinalName: (currentName, oci, parameter) => $"{parameter.propertyName} Scale: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.TextureProperty)
               );

            //Texture offset value
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "textureOffsetProperty",
                   name: "Texture Offset Property",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetTextureOffset(parameter.GetGameObject(oci), parameter.materialName, parameter.propertyName, Vector2.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial(oci).GetTextureOffset($"_{parameter.propertyName}"),
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
                   checkIntegrity: (oci, parameter, leftValue, rightValue) => CheckIntegrity(oci, parameter, leftValue, rightValue, ItemInfo.RowItemType.TextureProperty),
                   getFinalName: (currentName, oci, parameter) => $"{parameter.propertyName} Offset: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.TextureProperty)
               );

            //Color value
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "colorProperty",
                   name: "Color Property",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetColor(parameter.GetGameObject(oci), parameter.materialName, parameter.propertyName, Color.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial(oci).GetColor($"_{parameter.propertyName}"),
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
                   checkIntegrity: (oci, parameter, leftValue, rightValue) => CheckIntegrity(oci, parameter, leftValue, rightValue, ItemInfo.RowItemType.ColorProperty),
                   getFinalName: (currentName, oci, parameter) => $"{parameter.propertyName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.ColorProperty)
               );

            //Float value
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "floatProperty",
                   name: "Float Property",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetFloat(parameter.GetGameObject(oci), parameter.materialName, parameter.propertyName, Mathf.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   getValue: (oci, parameter) => parameter.GetMaterial(oci).GetFloat($"_{parameter.propertyName}"),
                   readValueFromXml: (parameter, node) => XmlConvert.ToSingle(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value.ToString()),
                   getParameter: GetMaterialInfoParameter,
                   readParameterFromXml: ReadMaterialInfoXml,
                   writeParameterToXml: WriteMaterialInfoXml,
                   checkIntegrity: (oci, parameter, leftValue, rightValue) => CheckIntegrity(oci, parameter, leftValue, rightValue, ItemInfo.RowItemType.FloatProperty),
                   getFinalName: (currentName, oci, parameter) => $"{parameter.propertyName}: {parameter.materialName}",
                   isCompatibleWithTarget: (oci) => IsCompatibleWithTarget(ItemInfo.RowItemType.FloatProperty)
               );
        }

        private static MaterialInfo GetMaterialInfoParameter(ObjectCtrlInfo oci)
        {
            return new MaterialInfo(selectedInterpolable.GameObject.GetFullPath(), selectedInterpolable.MaterialName, selectedInterpolable.PropertyName, selectedInterpolable.RendererName);
        }

        private static void WriteMaterialInfoXml(ObjectCtrlInfo oci, XmlTextWriter writer, MaterialInfo parameter)
        {
            writer.WriteAttributeString("gameObjectPath", parameter.gameObjectPath);
            writer.WriteAttributeString("materialName", parameter.materialName);
            writer.WriteAttributeString("propertyName", parameter.propertyName);
            writer.WriteAttributeString("rendererName", parameter.rendererName);
        }
        private static MaterialInfo ReadMaterialInfoXml(ObjectCtrlInfo oci, XmlNode node)
        {
            return new MaterialInfo(node.Attributes["gameObjectPath"].Value, node.Attributes["materialName"].Value, node.Attributes["propertyName"].Value, node.Attributes["rendererName"].Value);
        }

        private static bool CheckIntegrity(ObjectCtrlInfo oci, MaterialInfo parameter, object leftValue, object rightValue, ItemInfo.RowItemType rowType)
        {
            if (parameter != null)
                return parameter.CheckIntegrity(rowType);
            return false;
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
            public string gameObjectPath;
            public string materialName;
            public string propertyName;
            public string rendererName;
            private readonly int _hashCode;

            public MaterialInfo(string gameObjectPath, string materialName, string propertyName, string rendererName)
            {
                this.gameObjectPath = gameObjectPath;
                this.materialName = materialName;
                this.propertyName = propertyName;
                this.rendererName = rendererName;

                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + this.materialName.GetHashCode();
                    hash = hash * 31 + this.propertyName.GetHashCode();
                    this._hashCode = hash * 31 + this.gameObjectPath.GetHashCode();
                }
            }

            public Renderer GetRenderer(ObjectCtrlInfo oci)
            {
                return GetGameObject(oci).GetComponentsInChildren<Renderer>(true).First(x => x.NameFormatted() == rendererName);
            }

            public GameObject GetGameObject(ObjectCtrlInfo oci)
            {
                if (oci is OCIItem ociItem)
                    return ociItem.objectItem;
                else if (oci is OCIChar)
                    //Characters can reference multiple game objects depending on if the edits is on the body, clothes, accessories, etc.
                    //This makes sure we get the right one. This works because characters are always stored under a unique name within the scene, unlike objects
                    return GameObject.Find(gameObjectPath);
                return null;
            }

            public Material GetMaterial(ObjectCtrlInfo oci)
            {
                var go = GetGameObject(oci);
                foreach (var rend in GetRendererList(go))
                    foreach (var mat in GetMaterials(go, rend))
                        if (mat.NameFormatted() == materialName)
                            return mat;
                return null;
            }

            public bool CheckIntegrity(ItemInfo.RowItemType rowType)
            {
                if (gameObjectPath.IsNullOrEmpty())
                    return false;
                switch (rowType)
                {
                    case ItemInfo.RowItemType.Renderer:
                        if (rendererName.IsNullOrEmpty()) return false;
                        break;
                    case ItemInfo.RowItemType.Shader:
                        if (materialName.IsNullOrEmpty()) return false;
                        break;
                    case ItemInfo.RowItemType.TextureProperty:
                    case ItemInfo.RowItemType.ColorProperty:
                    case ItemInfo.RowItemType.FloatProperty:
                        if (materialName.IsNullOrEmpty() || propertyName.IsNullOrEmpty()) return false;
                        break;
                }
                return true;
            }

            public override int GetHashCode()
            {
                return this._hashCode;
            }
        }
    }
}
#endif