using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using System;
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
                        {
                            if (!item.itemInfo.panel.filePath.IsNullOrEmpty())
                                Logger.LogInfo($"bg:{item.itemInfo.panel.filePath}");
                            SaveBGTex(item, item.itemInfo.panel.filePath);
                        }
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

            /// <summary>
            /// A class for containing texture data, stored as a byte array. Access the texture with the Texture property and use Dispose to safely destroy it and prevent memory leaks.
            /// </summary>
            public sealed class TextureContainer : IDisposable
            {
                private byte[] _data;
                private Texture2D _texture;

                /// <summary>
                /// Load a byte array containing texture data.
                /// </summary>
                /// <param name="data"></param>
                public TextureContainer(byte[] data)
                {
                    Data = data ?? throw new ArgumentNullException(nameof(data));
                }

                /// <summary>
                /// Load the texture at the specified file path.
                /// </summary>
                /// <param name="filePath">Path of the file to load</param>
                public TextureContainer(string filePath)
                {
                    var data = LoadTextureBytes(filePath);
                    Data = data ?? throw new ArgumentNullException(nameof(data));
                }

                /// <summary>
                /// Byte array containing the texture data.
                /// </summary>
                public byte[] Data
                {
                    get => _data;
                    set
                    {
                        Dispose();
                        _data = value;
                    }
                }

                /// <summary>
                /// Texture data. Created from the Data byte array when accessed.
                /// </summary>
                public Texture2D Texture
                {
                    get
                    {
                        if (_texture == null)
                            if (_data != null)
                                _texture = TextureFromBytes(_data);

                        return _texture;
                    }
                }

                /// <summary>
                /// Dispose of the texture data. Does not dispose of the byte array. Texture data will be recreated when accessing the Texture property, if needed.
                /// </summary>
                public void Dispose()
                {
                    if (_texture != null)
                    {
                        Destroy(_texture);
                        _texture = null;
                    }
                }

                /// <summary>
                /// Read the specified file and return a byte array.
                /// </summary>
                /// <param name="filePath">Path of the file to load</param>
                /// <returns>Byte array with texture data</returns>
                private static byte[] LoadTextureBytes(string filePath)
                {
                    return File.ReadAllBytes(filePath);
                }

                /// <summary>
                /// Convert a byte array to Texture2D.
                /// </summary>
                /// <param name="texBytes">Byte array containing the image</param>
                /// <param name="format">TextureFormat</param>
                /// <param name="mipmaps">Whether to generate mipmaps</param>
                /// <returns></returns>
                private static Texture2D TextureFromBytes(byte[] texBytes, TextureFormat format = TextureFormat.ARGB32, bool mipmaps = true)
                {
                    if (texBytes == null || texBytes.Length == 0) return null;

                    //LoadImage automatically resizes the texture
                    var tex = new Texture2D(2, 2, format, mipmaps);

                    tex.LoadImage(texBytes);
                    return tex;
                }
            }
        }
    }
}
