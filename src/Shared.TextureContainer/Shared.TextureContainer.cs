using System;
using System.IO;
using UnityEngine;

namespace KK_Plugins
{
    /// <summary>
    /// A class for containing texture data, stored as a byte array. Access the texture with the Texture property and use Dispose to safely destroy it and prevent memory leaks.
    /// </summary>
    public sealed class TextureContainer : IDisposable
    {
        private byte[] _data;
        private Texture _texture;

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
        public Texture Texture
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
                UnityEngine.Object.Destroy(_texture);
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
        private static Texture TextureFromBytes(byte[] texBytes, TextureFormat format = TextureFormat.ARGB32, bool mipmaps = true)
        {
            if (texBytes == null || texBytes.Length == 0) return null;

            //LoadImage automatically resizes the texture so the texture size doesn't matter here
            var tex = new Texture2D(2, 2, format, mipmaps);
            tex.LoadImage(texBytes);

            RenderTexture rt = new RenderTexture(tex.width, tex.height, 0);
            rt.useMipMap = mipmaps;
            RenderTexture.active = rt;
            Graphics.Blit(tex, rt);

            return rt;
        }
    }
}
