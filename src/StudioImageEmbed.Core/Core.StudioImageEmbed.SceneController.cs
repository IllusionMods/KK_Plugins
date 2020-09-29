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
            TextureContainer BGTex;

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                //Dispose of any textures when resetting the scene
                if (operation == SceneOperationKind.Clear)
                {
                    FrameTex?.Dispose();
                    FrameTex = null;
                    BGTex?.Dispose();
                    BGTex = null;
                    return;
                }

                //On loading or importing a scene, check if the item has any patterns that exist in the pattern folder and use MaterialEditor to handle these textures instead
                foreach (var loadedItem in loadedItems.Values)
                {
                    if (loadedItem is OCIItem item)
                    {
                        if (item.itemComponent != null)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                string filePath = item.GetPatternPath(i);
                                SavePatternTex(item, i, filePath);
                            }
                        }

                        if (item.panelComponent != null)
                            SaveBGTex(item, item.itemInfo.panel.filePath);
                    }
                }

                //On loading a scene load the saved frame texture, if any
                //Frames and BGs are not imported, only loaded
                if (operation == SceneOperationKind.Load)
                {
                    FrameTex?.Dispose();
                    FrameTex = null;
                    BGTex?.Dispose();
                    BGTex = null;

                    var data = GetExtendedData();
                    if (data?.data != null)
                    {
                        if (data.data.TryGetValue("FrameData", out var frameObj) && frameObj != null)
                            FrameTex = new TextureContainer((byte[])frameObj);
                        if (data.data.TryGetValue("BGData", out var bgObj) && bgObj != null)
                            BGTex = new TextureContainer((byte[])bgObj);
                    }

                    if (FrameTex != null)
                    {
                        var imageFrame = Traverse.Create(Studio.Studio.Instance.frameCtrl).Field("imageFrame").GetValue<RawImage>();
                        imageFrame.texture = FrameTex.Texture;
                        imageFrame.enabled = true;

                        var cameraUI = Traverse.Create(Studio.Studio.Instance.frameCtrl).Field("cameraUI").GetValue<Camera>();
                        cameraUI.enabled = true;
                    }

                    if (BGTex != null)
                    {
                        BackgroundCtrl m_BackgroundCtrl = Traverse.Create(Studio.Studio.Instance).Field("m_BackgroundCtrl").GetValue<BackgroundCtrl>();
                        MeshRenderer meshRenderer = Traverse.Create(m_BackgroundCtrl).Field("meshRenderer").GetValue<MeshRenderer>();

                        meshRenderer.material.SetTexture("_MainTex", BGTex.Texture);
                        m_BackgroundCtrl.isVisible = true;
                    }
                }
            }

            /// <summary>
            /// Save the frame texture to the scene data
            /// </summary>
            protected override void OnSceneSave()
            {
                var data = new PluginData();

                data.data["FrameData"] = FrameTex?.Data;
                data.data["BGData"] = BGTex?.Data;
                SetExtendedData(data);
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

            /// <summary>
            /// Set the bg texture to be saved to the scene data
            /// </summary>
            /// <param name="filePath">Full path of the image file</param>
            public void SetBGTex(string filePath)
            {
                if (!File.Exists(filePath)) return;

                BGTex?.Dispose();
                BGTex = new TextureContainer(filePath);
            }

            /// <summary>
            /// Clear the saved bg texture, if any
            /// </summary>
            public void ClearBGTex()
            {
                BGTex?.Dispose();
                BGTex = null;
            }
        }
    }
}
