using BepInEx.Configuration;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MaterialEditorAPI;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using XUnity.ResourceRedirector;
using static MaterialEditorAPI.MaterialAPI;
#if AI || HS2
using AIChara;
#endif
#if EC
using Map;
#else
using Studio;
using KKAPI.Utilities;
#endif
#if PH
using ChaControl = Human;
#endif
namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorPlugin
    {
#if EC
        private void ExtendedSave_CardBeingImported(Dictionary<string, ExtensibleSaveFormat.PluginData> importedExtendedData)
        {
            if (importedExtendedData.TryGetValue(PluginGUID, out var data))
            {
                if (data.data.TryGetValue("RendererPropertyList", out var rendererProperties) && rendererProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.RendererProperty>>((byte[])rendererProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["RendererPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["RendererPropertyList"] = null;
                }

                if (data.data.TryGetValue("MaterialFloatPropertyList", out var floatProperties) && floatProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialFloatProperty>>((byte[])floatProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialFloatPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialFloatPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialColorPropertyList", out var colorProperties) && colorProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialColorProperty>>((byte[])colorProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialColorPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialColorPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialTexturePropertyList", out var textureProperties) && textureProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialTextureProperty>>((byte[])textureProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialTexturePropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialTexturePropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialShaderList", out var shaderProperties) && shaderProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialShader>>((byte[])shaderProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialShaderList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialShaderList"] = null;
                }
                if (data.data.TryGetValue("MaterialCopyList", out var copyProperties) && copyProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialCopy>>((byte[])copyProperties);
                    properties.RemoveAll(x => x.CoordinateIndex != 0); //Remove all but the first coordinate

                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialCopyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialCopyList"] = null;
                }
            }
        }
#elif KKS
        private void ExtendedSave_CardBeingImported(Dictionary<string, ExtensibleSaveFormat.PluginData> importedExtendedData, Dictionary<int, int?> coordinateMapping)
        {
            if (importedExtendedData.TryGetValue(PluginGUID, out var data))
            {
                if (data.data.TryGetValue("RendererPropertyList", out var rendererProperties) && rendererProperties != null)
                {
                    List<MaterialEditorCharaController.RendererProperty> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.RendererProperty>>((byte[])rendererProperties);
                    List<MaterialEditorCharaController.RendererProperty> propertiesNew = new List<MaterialEditorCharaController.RendererProperty>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["RendererPropertyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["RendererPropertyList"] = null;
                }

                if (data.data.TryGetValue("MaterialFloatPropertyList", out var floatProperties) && floatProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialFloatProperty> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialFloatProperty>>((byte[])floatProperties);
                    List<MaterialEditorCharaController.MaterialFloatProperty> propertiesNew = new List<MaterialEditorCharaController.MaterialFloatProperty>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialFloatPropertyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialFloatPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialColorPropertyList", out var colorProperties) && colorProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialColorProperty> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialColorProperty>>((byte[])colorProperties);
                    List<MaterialEditorCharaController.MaterialColorProperty> propertiesNew = new List<MaterialEditorCharaController.MaterialColorProperty>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialColorPropertyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialColorPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialTexturePropertyList", out var textureProperties) && textureProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialTextureProperty> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialTextureProperty>>((byte[])textureProperties);
                    List<MaterialEditorCharaController.MaterialTextureProperty> propertiesNew = new List<MaterialEditorCharaController.MaterialTextureProperty>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialTexturePropertyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialTexturePropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialShaderList", out var shaderProperties) && shaderProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialShader> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialShader>>((byte[])shaderProperties);
                    List<MaterialEditorCharaController.MaterialShader> propertiesNew = new List<MaterialEditorCharaController.MaterialShader>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialShaderList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialShaderList"] = null;
                }
                if (data.data.TryGetValue("MaterialCopyList", out var copyProperties) && copyProperties != null)
                {
                    List<MaterialEditorCharaController.MaterialCopy> properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialCopy>>((byte[])copyProperties);
                    List<MaterialEditorCharaController.MaterialCopy> propertiesNew = new List<MaterialEditorCharaController.MaterialCopy>();

                    foreach (var property in properties)
                    {
                        if (property.ObjectType == MaterialEditorCharaController.ObjectType.Accessory || property.ObjectType == MaterialEditorCharaController.ObjectType.Clothing)
                        {
                            if (coordinateMapping.TryGetValue(property.CoordinateIndex, out int? newIndex) && newIndex != null)
                            {
                                property.CoordinateIndex = (int)newIndex;
                                propertiesNew.Add(property);
                            }
                        }
                        else
                        {
                            propertiesNew.Add(property);
                        }
                    }

                    if (propertiesNew.Count > 0)
                        data.data["MaterialCopyList"] = MessagePackSerializer.Serialize(propertiesNew);
                    else
                        data.data["MaterialCopyList"] = null;
                }
            }
        }
#endif

#if EC
        private void ExtendedSave_CoordinateBeingImported(Dictionary<string, ExtensibleSaveFormat.PluginData> importedExtendedData)
        {
            if (importedExtendedData.TryGetValue(PluginGUID, out var data))
            {
                if (data.data.TryGetValue("RendererPropertyList", out var rendererProperties) && rendererProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.RendererProperty>>((byte[])rendererProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["RendererPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["RendererPropertyList"] = null;
                }

                if (data.data.TryGetValue("MaterialFloatPropertyList", out var floatProperties) && floatProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialFloatProperty>>((byte[])floatProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialFloatPropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialFloatPropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialColorProperty", out var colorProperties) && colorProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialColorProperty>>((byte[])colorProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialColorProperty"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialColorProperty"] = null;
                }
                if (data.data.TryGetValue("MaterialTexturePropertyList", out var textureProperties) && textureProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialTextureProperty>>((byte[])textureProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialTexturePropertyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialTexturePropertyList"] = null;
                }
                if (data.data.TryGetValue("MaterialShaderList", out var shaderProperties) && shaderProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialShader>>((byte[])shaderProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialShaderList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialShaderList"] = null;
                }
                if (data.data.TryGetValue("MaterialCopyList", out var copyProperties) && copyProperties != null)
                {
                    var properties = MessagePackSerializer.Deserialize<List<MaterialEditorCharaController.MaterialCopy>>((byte[])copyProperties);
                    properties.RemoveAll(x => x.ObjectType == MaterialEditorCharaController.ObjectType.Clothing && x.Slot == 7); //Remove indoor shoes
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property.Slot == 8)//Change slot index for outdoor shoes to the one used by EC
                            property.Slot = 7;
                    }

                    if (properties.Count > 0)
                        data.data["MaterialCopyList"] = MessagePackSerializer.Serialize(properties);
                    else
                        data.data["MaterialCopyList"] = null;
                }
            }
        }
#endif

    }
}
