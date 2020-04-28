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
            TextureHolder FrameTex;

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                if (operation == SceneOperationKind.Load)
                {
                    FrameTex?.Dispose();
                    FrameTex = null;

                    var data = GetExtendedData();
                    if (data?.data != null)
                        if (data.data.TryGetValue("FrameData", out var frameObj) && frameObj != null)
                            FrameTex = new TextureHolder((byte[])frameObj);

                    if (FrameTex != null)
                    {
                        var imageFrame = Traverse.Create(Studio.Studio.Instance.frameCtrl).Field("imageFrame").GetValue<RawImage>();
                        imageFrame.texture = FrameTex.Texture;
                        imageFrame.enabled = true;

                        var cameraUI = Traverse.Create(Studio.Studio.Instance.frameCtrl).Field("cameraUI").GetValue<Camera>();
                        cameraUI.enabled = true;
                    }
                }
                else if (operation == SceneOperationKind.Clear)
                {
                    FrameTex?.Dispose();
                    FrameTex = null;
                }
                else //Do not import saved data
                    return;
            }

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
                FrameTex = new TextureHolder(filePath);
            }

            /// <summary>
            /// Clear the saved frame texture, if any
            /// </summary>
            public void ClearFrameTex()
            {
                FrameTex?.Dispose();
                FrameTex = null;
            }

            public sealed class TextureHolder : IDisposable
            {
                private byte[] _data;
                private Texture2D _texture;

                public TextureHolder(byte[] data)
                {
                    Data = data ?? throw new ArgumentNullException(nameof(data));
                }

                public TextureHolder(string filePath)
                {
                    var data = LoadTextureBytes(filePath);
                    Data = data ?? throw new ArgumentNullException(nameof(data));
                }

                public byte[] Data
                {
                    get => _data;
                    set
                    {
                        Dispose();
                        _data = value;
                    }
                }

                public Texture2D Texture
                {
                    get
                    {
                        if (_texture == null)
                        {
                            if (_data != null)
                                _texture = TextureFromBytes(_data);
                        }
                        return _texture;
                    }
                }

                public void Dispose()
                {
                    if (_texture != null)
                    {
                        Destroy(_texture);
                        _texture = null;
                    }
                }

                private static Texture2D LoadTexture(string filePath, TextureFormat format = TextureFormat.BC7, bool mipmaps = true)
                {
                    return TextureFromBytes(LoadTextureBytes(filePath), format, mipmaps);
                }

                private static byte[] LoadTextureBytes(string filePath)
                {
                    return File.ReadAllBytes(filePath);
                }

                /// <summary>
                /// Convert a byte array to Texture2D
                /// </summary>
                /// <param name="texBytes">Byte array containing the image</param>
                /// <param name="format">TextureFormat</param>
                /// <param name="mipmaps">Whether to generate mipmaps</param>
                /// <returns></returns>
                private static Texture2D TextureFromBytes(byte[] texBytes, TextureFormat format = TextureFormat.BC7, bool mipmaps = true)
                {
                    if (texBytes == null || texBytes.Length == 0) return null;

                    var tex = new Texture2D(2, 2, format, mipmaps);

                    //LoadImage automatically resizes the texture
                    tex.LoadImage(texBytes);
                    return tex;
                }
            }
        }
    }
}
