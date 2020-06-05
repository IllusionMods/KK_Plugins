using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class ImageEmbed
    {
        public class ImageEmbedSceneController : SceneCustomFunctionController
        {
            TextureContainer FrameTex;

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                //Dispose of any Frame texture when resetting the scene
                if (operation == SceneOperationKind.Clear)
                {
                    FrameTex?.Dispose();
                    FrameTex = null;
                    return;
                }

                //On loading or importing a scene, check if the item has any patterns that exist in the pattern folder and use MaterialEditor to handle these textures instead
                foreach (var loadedItem in loadedItems.Values)
                {
                    if (loadedItem is OCIItem item)
                    {
                        if (item?.itemComponent != null)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                string filePath = item.GetPatternPath(i);
                                SavePatternTex(item, i, filePath);
                            }
                        }

                        if (item?.panelComponent != null)
                            SaveBGTex(item, item.itemInfo.panel.filePath);
                    }
                }

                //On loading a scene load the saved frame texture, if any
                //Frames are not imported, only loaded
                if (operation == SceneOperationKind.Load)
                {
                    FrameTex?.Dispose();
                    FrameTex = null;

                    var data = GetExtendedData();
                    if (data?.data != null)
                        if (data.data.TryGetValue("FrameData", out var frameObj) && frameObj != null)
                            FrameTex = new TextureContainer((byte[])frameObj);

                    if (FrameTex != null)
                    {
                        var imageFrame = Traverse.Create(Studio.Studio.Instance.frameCtrl).Field("imageFrame").GetValue<RawImage>();
                        imageFrame.texture = FrameTex.Texture;
                        imageFrame.enabled = true;

                        var cameraUI = Traverse.Create(Studio.Studio.Instance.frameCtrl).Field("cameraUI").GetValue<Camera>();
                        cameraUI.enabled = true;
                    }
                }
            }

            /// <summary>
            /// Save the frame texture to the scene data
            /// </summary>
            protected override void OnSceneSave()
            {
                var data = new PluginData();

                if (FrameTex?.Data == null)
                    SetExtendedData(null);
                else
                {
                    data.data[$"FrameData"] = FrameTex.Data;
                    SetExtendedData(data);
                }
            }

            /// <summary>
            /// Set the frame texture to be saved to the scene data
            /// </summary>
            /// <param name="filePath">Full path of the image file</param>
            public void SetFrameTex(string filePath)
            {
                if (!File.Exists(filePath)) return;

                FrameTex?.Dispose();
                FrameTex = new TextureContainer(filePath);
            }

            /// <summary>
            /// Clear the saved frame texture, if any
            /// </summary>
            public void ClearFrameTex()
            {
                FrameTex?.Dispose();
                FrameTex = null;
            }
        }
    }
}
