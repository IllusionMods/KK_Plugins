using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

namespace KK_Plugins
{
    public sealed class TextureContainerManager
    {
        static Dictionary<TextureHolderKey, TextureHolder> _textureHolder = new Dictionary<TextureHolderKey, TextureHolder>();

        public class TextureHolderKey
        {
            public readonly byte[] data;
            public readonly uint dataHash;
            public readonly TextureFormat format;
            public readonly bool mipmaps;

            public TextureHolderKey(byte[] data, TextureFormat format = TextureFormat.ARGB32, bool mipmaps = true)
            {
                this.data = data;
                //First part of the data is sufficient to calculate the hash.
                dataHash = CRC32Calculator.CalculateCRC32(data, 1 << 10);
                this.format = format;
                this.mipmaps = mipmaps;
            }

            public override bool Equals(object obj)
            {
                if (obj is TextureHolderKey)
                {
                    var other = (TextureHolderKey)obj;

                    return
                        other.dataHash == dataHash &&
                        other.format == format &&
                        other.mipmaps == mipmaps &&
                        other.data.SequenceEqual(data);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return (int)dataHash ^ format.GetHashCode() ^ mipmaps.GetHashCode();
            }
        }

        public class TextureHolder
        {
            internal int refCount;
            internal TextureHolderKey key;
            private Texture _texture;

            public TextureHolder(TextureHolderKey key)
            {
                this.key = key;
            }

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

        public static TextureHolder Acquire( string filePath )
        {
            return Acquire(LoadTextureBytes(filePath));
        }

        public static TextureHolder Acquire( byte[] texBytes )
        {
            if (texBytes == null)
                throw new ArgumentNullException(nameof(texBytes));

            TextureHolderKey key = new TextureHolderKey(texBytes);
            if (!_textureHolder.TryGetValue(key, out var holder))
                holder = _textureHolder[key] = new TextureHolder(key);

            ++holder.refCount;
            return holder;
        }

        public static void Release( TextureHolder holder )
        {
            if (--holder.refCount > 0)
                return;

            holder.Destroy();
            _textureHolder.Remove(holder.key);
        }

        /// <summary>
        /// Convert a byte array to Texture2D.
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

                RenderTexture rt = new RenderTexture(tex.width, tex.height, 0);
                rt.useMipMap = mipmaps;
                Graphics.Blit(tex, rt);
                return rt;
            }
            finally
            {
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
