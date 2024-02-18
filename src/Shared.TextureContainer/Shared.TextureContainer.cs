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
        private TextureContainerManager.TextureHolder _holder;

        /// <summary>
        /// Load a byte array containing texture data.
        /// </summary>
        /// <param name="data"></param>
        public TextureContainer(byte[] data)
        {
            _holder = TextureContainerManager.Acquire(data);
        }

        /// <summary>
        /// Load the texture at the specified file path.
        /// </summary>
        /// <param name="filePath">Path of the file to load</param>
        public TextureContainer(string filePath)
        {
            _holder = TextureContainerManager.Acquire(filePath);
        }

        /// <summary>
        /// Byte array containing the texture data.
        /// </summary>
        public byte[] Data
        {
            get => _holder.key.data;

            set
            {
                var newHolder = TextureContainerManager.Acquire(value);
                TextureContainerManager.Release(_holder);
                _holder = newHolder;
            }
        }

        /// <summary>
        /// Texture data. Created from the Data byte array when accessed.
        /// </summary>
        public Texture Texture
        {
            get
            {
                return _holder.Texture;
            }
        }

        /// <summary>
        /// Dispose of the texture data. Does not dispose of the byte array. Texture data will be recreated when accessing the Texture property, if needed.
        /// </summary>
        public void Dispose()
        {
            TextureContainerManager.Release(_holder);
        }
    }
}
