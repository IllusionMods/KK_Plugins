#if !PH
using KKAPI.Utilities;
using Studio;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;
using static MaterialEditorAPI.MaterialAPI;


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
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Renderer)parameter).enabled = (bool)leftValue,
                   interpolateAfter: null,
                   isCompatibleWithTarget: (oci) => oci != null,
                   getValue: (oci, parameter) => {
                       GameObject _go = GetGameObjectFromOci(oci);

                       if (_go != null)
                           return GetRendererList(_go).First().enabled;
                       else return true;
                   },
                   readValueFromXml: (parameter, node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString((bool)value)),
                   getParameter: (oci) =>
                   {
                       GameObject _go = GetGameObjectFromOci(oci);
                       
                       return GetRendererList(_go).First();
                   },
                   readParameterFromXml: (oci, node) =>
                   {
                       GameObject _go = GetGameObjectFromOci(oci);

                       if (_go != null)
                           return GetRendererList(_go).First();
                       else return null;
                   },
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is Renderer && parameter != null)
                           return true;
                       return false;
                   },
                   writeParameterToXml: (oci, writer, parameter) => writer.WriteAttributeString("parameter", parameter.NameFormatted()),
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.NameFormatted()}"
               );

            //Renderer ShadowCastingMode
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "shadowCastingMode",
                   name: "Shadow Casting Mode",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Renderer)parameter).shadowCastingMode = (ShadowCastingMode)leftValue,
                   interpolateAfter: null,
                   isCompatibleWithTarget: (oci) => oci != null,
                   getValue: (oci, parameter) => {
                       GameObject _go = GetGameObjectFromOci(oci);

                       if (_go != null)
                           return GetRendererList(_go).First().shadowCastingMode;
                       else return ShadowCastingMode.On;
                   },
                   readValueFromXml: (parameter, node) => (ShadowCastingMode)XmlConvert.ToInt32(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", ((int)value).ToString()),
                   getParameter: (oci) =>
                   {
                       GameObject _go = GetGameObjectFromOci(oci);

                       return GetRendererList(_go).First();
                   },
                   readParameterFromXml: (oci, node) =>
                   {
                       GameObject _go = GetGameObjectFromOci(oci);

                       if (_go != null)
                           return GetRendererList(_go).First();
                       else return null;
                   },
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is Renderer && parameter != null)
                           return true;
                       return false;
                   },
                   writeParameterToXml: (oci, writer, parameter) => writer.WriteAttributeString("parameter", parameter.NameFormatted()),
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.NameFormatted()}"
               );

            //Renderer ReceiveShadows
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "receiveShadows",
                   name: "Receive Shadows",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Renderer)parameter).receiveShadows = (bool)leftValue,
                   interpolateAfter: null,
                   isCompatibleWithTarget: (oci) => oci != null,
                   getValue: (oci, parameter) => {
                       GameObject _go = GetGameObjectFromOci(oci);

                       if (_go != null)
                           return GetRendererList(_go).First().receiveShadows;
                       else return true;
                   },
                   readValueFromXml: (parameter, node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString((bool)value)),
                   getParameter: (oci) =>
                   {
                       GameObject _go = GetGameObjectFromOci(oci);

                       return GetRendererList(_go).First();
                   },
                   readParameterFromXml: (oci, node) =>
                   {
                       GameObject _go = GetGameObjectFromOci(oci);

                       if (_go != null)
                           return GetRendererList(_go).First();
                       else return null;
                   },
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is Renderer && parameter != null)
                           return true;
                       return false;
                   },
                   writeParameterToXml: (oci, writer, parameter) => writer.WriteAttributeString("parameter", parameter.NameFormatted()),
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.NameFormatted()}"
               );

            //Shader
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "shader",
                   name: "Shader",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetShader(parameter.go, parameter.materialName, leftValue),
                   interpolateAfter: null,
                   isCompatibleWithTarget: (oci) => oci != null,
                   getValue: (oci, parameter) => {
                       return GetMaterials(parameter.go, GetRendererList(parameter.go).First()).First().shader.NameFormatted();
                   },
                   readValueFromXml: (parameter, node) => node.Attributes["value"].Value,
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value),
                   getParameter: (oci) =>
                   {
                       var go = GetGameObjectFromOci(oci);
                       var renderer = GetRendererList(go).First();
                       var material = GetMaterials(go, renderer).First();
                       return new MaterialInfo(oci, material.NameFormatted(), "");
                   },
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   writeParameterToXml: WriteMaterialInfoXml,
                   readParameterFromXml: ReadMaterialInfoXml,
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}"
               );

            //Shader RenderQueue
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "renderQueue",
                   name: "Render Queue",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetRenderQueue(parameter.go, parameter.materialName, leftValue),
                   interpolateAfter: null,
                   isCompatibleWithTarget: (oci) => oci != null,
                   getValue: (oci, parameter) => {
                       return GetMaterials(parameter.go, GetRendererList(parameter.go).First()).First().renderQueue;
                   },
                   readValueFromXml: (parameter, node) => XmlConvert.ToInt32(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value.ToString()),
                   getParameter: (oci) =>
                   {
                       var go = GetGameObjectFromOci(oci);
                       var renderer = GetRendererList(go).First();
                       var material = GetMaterials(go, renderer).First();
                       return new MaterialInfo(oci, material.NameFormatted(), "");
                   },
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   writeParameterToXml: WriteMaterialInfoXml,
                   readParameterFromXml: ReadMaterialInfoXml,
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}"
               );

            //Float value
            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "floatProperty",
                   name: "Float Property",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => SetFloat(parameter.go, parameter.materialName, parameter.propertyName, Mathf.LerpUnclamped(leftValue, rightValue, factor)),
                   interpolateAfter: null,
                   isCompatibleWithTarget: (oci) => oci != null,
                   getValue: (oci, parameter) => {
                       return GetMaterials(parameter.go, GetRendererList(parameter.go).First()).First().GetFloat($"_{parameter.propertyName}");
                   },
                   readValueFromXml: (parameter, node) => XmlConvert.ToSingle(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", value.ToString()),
                   getParameter: (oci) =>
                   {
                       var go = GetGameObjectFromOci(oci);
                       var renderer = GetRendererList(go).First();
                       var material = GetMaterials(go, renderer).First();
                       return new MaterialInfo(oci, material.NameFormatted(), "Alpha");
                   },
                   checkIntegrity: (oci, parameter, leftValue, rightValue) =>
                   {
                       if (parameter is MaterialInfo && parameter != null)
                           return true;
                       return false;
                   },
                   writeParameterToXml: WriteMaterialInfoXml,
                   readParameterFromXml: ReadMaterialInfoXml,
                   getFinalName: (currentName, oci, parameter) => $"{currentName}: {parameter.materialName}"
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

        private static void WriteMaterialInfoXml(ObjectCtrlInfo oci, XmlTextWriter writer, MaterialInfo parameter)
        {
            writer.WriteAttributeString("materialName", parameter.materialName);
            writer.WriteAttributeString("propertyName", parameter.propertyName);
        }
        private static MaterialInfo ReadMaterialInfoXml(ObjectCtrlInfo oci, XmlNode node)
        {
            return new MaterialInfo(oci, node.Attributes["materialName"].Value, node.Attributes["propertyName"].Value);
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

            public override int GetHashCode()
            {
                return this._hashCode;
            }
        }
    }
}
#endif