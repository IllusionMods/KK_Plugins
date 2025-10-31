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
            public readonly long hash;
            public readonly TextureFormat format;
            public readonly bool mipmaps;

            public TextureKey(byte[] data, TextureFormat format = TextureFormat.ARGB32, bool mipmaps = true)
            {
                //First part of the data is sufficient to calculate the hash.
                long hash = (long)CRC64Calculator.CalculateCRC64(data, 1 << 11, 1 << 9, true);
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
                           other.data.SequenceEqualFast(data);
                }

                return false;
            }


            public override int GetHashCode()
            {
                // Need to return int, so we do the best we can
                return (int)(hash ^ (hash >> 32));
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
        public static Token Acquire(string filePath) => Acquire(LoadTextureBytes(filePath));

        /// <summary>
        /// Acquire TextureHolder. If it already exists, return it.
        /// </summary>
        /// <param name="texBytes"></param>
        /// <returns></returns>
        public static Token Acquire(byte[] texBytes)
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
        public static void Release(Token holder)
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
            Texture2D tex = null;

            try
            {
                tex = ImageHelper.LoadTexture2DFromBytes(texBytes, format, mipmaps);

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

            for (int i = 0; i < size; ++i)
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

    /// <summary>
    /// Class for calculating CRC64 from a byte array
    /// </summary>
    public static class CRC64Calculator
    {
        private const int width = 64;
        private const ulong polynomial = 0xE4C11DB7B4AC89F3;
        private const ulong initialValue = 0xFFFFFFFFFFFFFFFF;
        private const ulong xorOutValue = 0xFFFFFFFFFFFFFFFF;

        private static readonly ulong[] Crc64Table;

        static CRC64Calculator()
        {
            // Initialize CRC64 table
            Crc64Table = GenerateCrc64Table();
        }

        /// <summary>
        /// Calculate the numeric hash
        /// </summary>
        /// <param name="data">The data bytes to hash.</param>
        /// <param name="size">How many bytes to hash. Leave out to hash all.</param>
        public static ulong CalculateCRC64(byte[] data, int? size = null, int? sizeEnd = null, bool hashLen = false)
        {
            size = Math.Min(data.Length, size ?? data.Length);
            sizeEnd = Math.Min(data.Length, sizeEnd ?? data.Length);
            byte[] crcCheckVal = CalculateCheckValue(data, size.Value, sizeEnd.Value, hashLen);
            Array.Resize(ref crcCheckVal, 8);
            return BitConverter.ToUInt64(crcCheckVal, 0);
        }

        public static byte[] CalculateCheckValue(byte[] data, int size, int sizeEnd, bool hashLen)
        {
            if (data == null) return null;

            ulong crc = initialValue;

            // Hash the start
            int i;
            for (i = 0; i < size; i++)
            {
                crc = Crc64Table[((crc >> (width - 8)) ^ data[i]) & 0xFF] ^ (crc << 8);
                crc &= UInt64.MaxValue >> (64 - width);
            }

            // Hash the end
            int length = data.Length;
            int downEnd = length - sizeEnd - 1;
            for (i = length - 1; i > downEnd; i--)
            {
                crc = Crc64Table[((crc >> (width - 8)) ^ data[i]) & 0xFF] ^ (crc << 8);
                crc &= UInt64.MaxValue >> (64 - width);
            }

            // Hash the length
            if (hashLen)
            {
                byte[] lengthBytes = BitConverter.GetBytes(length);
                foreach (byte b in lengthBytes)
                {
                    crc = Crc64Table[((crc >> (width - 8)) ^ b) & 0xFF] ^ (crc << 8);
                    crc &= UInt64.MaxValue >> (64 - width);
                }
            }

            ulong crcFinalValue = crc ^ xorOutValue;
            return BitConverter.GetBytes(crcFinalValue).Take((width + 7) / 8).ToArray();
        }

        private static ulong[] GenerateCrc64Table()
        {
            var lookupTable = new ulong[256];
            ulong topBit = (ulong)1 << (width - 1);

            for (int i = 0; i < lookupTable.Length; i++)
            {
                byte inByte = (byte)i;

                ulong r = (ulong)inByte << (width - 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((r & topBit) != 0)
                    {
                        r = (r << 1) ^ polynomial;
                    }
                    else
                    {
                        r <<= 1;
                    }
                }

                lookupTable[i] = r & (UInt64.MaxValue >> (64 - width));
            }

            return lookupTable;
        }
    }
}
