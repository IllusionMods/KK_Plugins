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
        private TextureContainerManager.Token _token;

        /// <summary>
        /// Load a byte array containing texture data.
        /// </summary>
        /// <param name="data"></param>
        public TextureContainer(byte[] data)
        {
            _token = TextureContainerManager.Acquire(data);
        }

        /// <summary>
        /// Load the texture at the specified file path.
        /// </summary>
        /// <param name="filePath">Path of the file to load</param>
        public TextureContainer(string filePath)
        {
            _token = TextureContainerManager.Acquire(filePath);
        }

        /// <summary>
        /// Byte array containing the texture data.
        /// </summary>
        public byte[] Data
        {
            get => _token.Data;

            set
            {
                var newToken = TextureContainerManager.Acquire(value);
                TextureContainerManager.Release(_token);
                _token = newToken;
            }
        }

        /// <summary>
        /// Texture data. Created from the Data byte array when accessed.
        /// </summary>
        public Texture Texture
        {
            get
            {
                return _token.Texture;
            }
        }

        /// <summary>
        /// Dispose of the texture data.
        /// </summary>
        public void Dispose()
        {
            TextureContainerManager.Release(_token);
        }
    }
}
