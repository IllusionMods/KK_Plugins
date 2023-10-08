#if !PH
using KKAPI.Utilities;
using Studio;
using System.Linq;
using System.Xml;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;


namespace MaterialEditorAPI
{
    internal static class TimelineCompatibilityHelper
    {
        internal static void PopulateTimeline()
        {
            if (!TimelineCompatibility.IsTimelineAvailable()) return;

            TimelineCompatibility.AddInterpolableModelDynamic(
                   owner: "MaterialEditor",
                   id: "targetEnabled",
                   name: "Enabled",
                   interpolateBefore: (oci, parameter, leftValue, rightValue, factor) => ((Renderer)parameter).enabled = (bool)leftValue,
                   interpolateAfter: null,
                   isCompatibleWithTarget: (oci) => oci != null,
                   getValue: (oci, parameter) => {
                       GameObject _go = null;
                       if (oci is OCIItem ociItem)
                           _go = ociItem.objectItem;
                       else if (oci is OCIChar ociChar)
                           _go = ociChar.charInfo.gameObject;

                       if (_go != null)
                           return GetRendererList(_go).First().enabled;
                       else return true;
                   },
                   readValueFromXml: (parameter, node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                   writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString((bool)value)),
                   getParameter: (oci) =>
                   {
                       GameObject _go;
                       if (oci is OCIItem ociItem)
                           _go = ociItem.objectItem;
                       else if (oci is OCIChar ociChar)
                           _go = ociChar.charInfo.gameObject;
                       else return null;
                       
                       return GetRendererList(_go).First();
                   },
                   readParameterFromXml: (oci, node) =>
                   {
                       GameObject _go = null;
                       if (oci is OCIItem ociItem)
                           _go = ociItem.objectItem;
                       else if (oci is OCIChar ociChar)
                           _go = ociChar.charInfo.gameObject;

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
                   getFinalName: (currentName, oci, parameter) => $"Enabled: {parameter}"
               );
        }
    }
}
#endif