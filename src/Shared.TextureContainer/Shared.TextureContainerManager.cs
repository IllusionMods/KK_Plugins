using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

namespace KK_Plugins
{
    /// <summary>
    /// Class that manages texture containers.
    /// </summary>
    public sealed class TextureContainerManager
    {
        /// <summary>
        /// Dictionary to hold textures
        /// </summary>
        static Dictionary<TextureKey, Token> _textureHolder = new Dictionary<TextureKey, Token>();

        /// <summary>
        /// Class for using dictionary keys for information needed to generate textures
        /// </summary>
        internal class TextureKey
        {
            public readonly byte[] data;
            public readonly int hash;
            public readonly TextureFormat format;
            public readonly bool mipmaps;

            public TextureKey(byte[] data, TextureFormat format = TextureFormat.ARGB32, bool mipmaps = true)
            {
                //First part of the data is sufficient to calculate the hash.
                int hash = (int)CRC32Calculator.CalculateCRC32(data, 1 << 10);
                hash ^= format.GetHashCode();
                hash ^= mipmaps.GetHashCode();

                this.data = data;
                this.hash = hash;
                this.format = format;
                this.mipmaps = mipmaps;
            }

            public override bool Equals(object obj)
            {
                if (obj is TextureKey other)
                {
                    return other.hash == hash &&
                        other.format == format &&
                        other.mipmaps == mipmaps &&
                        Utility.FastSequenceEqual(other.data, data);
                }

                return false;
            }


            public override int GetHashCode()
            {
                return hash;
            }
        }

        /// <summary>
        /// Texture holder with reference counter
        /// </summary>
        public class Token
        {
            //Reference counter. when it reaches 0, the texture is released.
            internal int refCount;
            internal TextureKey key;
            private Texture _texture;

            internal Token(TextureKey key)
            {
                this.key = key;
            }

            public byte[] Data => key.data;

            public Texture Texture
            {
                get
                {
                    if (_texture == null && key.data != null)
                        _texture = TextureFromBytes(key.data, key.format, key.mipmaps);

                    return _texture;
                }
            }
            
            public void Destroy()
            {
                if (_texture != null)
                {
                    UnityEngine.Object.Destroy(_texture);
                    _texture = null;
                }   
            }
        }

        /// <summary>
        /// Acquire TextureHolder. If it already exists, return it.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Token Acquire( string filePath )
        {
            return Acquire(LoadTextureBytes(filePath));
        }

        /// <summary>
        /// Acquire TextureHolder. If it already exists, return it.
        /// </summary>
        /// <param name="texBytes"></param>
        /// <returns></returns>
        public static Token Acquire( byte[] texBytes )
        {
            if (texBytes == null)
                throw new ArgumentNullException(nameof(texBytes));

            TextureKey key = new TextureKey(texBytes);
            if (!_textureHolder.TryGetValue(key, out var holder))
                holder = _textureHolder[key] = new Token(key);

            ++holder.refCount;
            return holder;
        }

        /// <summary>
        /// Release the TextureHolder. 
        /// If there are zero TextureHolders with the same texture, the texture is released.
        /// </summary>
        /// <param name="holder"></param>
        public static void Release( Token holder )
        {
            if (--holder.refCount > 0)
                return;

            holder.Destroy();
            _textureHolder.Remove(holder.key);
        }

        /// <summary>
        /// Convert a byte array to Texture.
        /// </summary>
        /// <param name="texBytes">Byte array containing the image</param>
        /// <param name="format">TextureFormat</param>
        /// <param name="mipmaps">Whether to generate mipmaps</param>
        /// <returns></returns>
        private static Texture TextureFromBytes(byte[] texBytes, TextureFormat format, bool mipmaps)
        {
            if (texBytes == null || texBytes.Length == 0) return null;

            //LoadImage automatically resizes the texture so the texture size doesn't matter here
            Texture2D tex = new Texture2D(2, 2, format, mipmaps);

            try
            {
                tex.LoadImage(texBytes);

                //Transfer to GPU memory and delete data in normal memory
                RenderTexture rt = new RenderTexture(tex.width, tex.height, 0);
                rt.useMipMap = mipmaps;
                Graphics.Blit(tex, rt);
                return rt;
            }
            finally
            {
                // delete data in normal memory
                UnityEngine.Object.Destroy(tex);
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
    }

    /// <summary>
    /// Class for calculating CRC32 from a byte array
    /// </summary>
    public class CRC32Calculator
    {
        private static readonly uint[] Crc32Table;

        static CRC32Calculator()
        {
            // Initialize CRC32 table
            Crc32Table = GenerateCrc32Table();
        }

        public static uint CalculateCRC32(byte[] data, int size)
        {
            uint crc32 = 0xFFFFFFFF; // Set initial value
            size = Mathf.Min(data.Length, size);

            for( int i = 0; i < size; ++i )
            {
                crc32 = (crc32 >> 8) ^ Crc32Table[(crc32 ^ data[i]) & 0xFF];
            }

            return crc32 ^ 0xFFFFFFFF; // Invert the final result
        }

        private static uint[] GenerateCrc32Table()
        {
            uint[] table = new uint[256];

            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
                }
                table[i] = crc;
            }

            return table;
        }
    }
}
