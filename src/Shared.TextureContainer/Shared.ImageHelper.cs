using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KK_Plugins
{
    //Shamelessly stolen from https://stackoverflow.com/a/1246008
    public static class ImageHelper
    {
        /// <summary>
        /// File filter for all the supported images
        /// </summary>
        public const string FileFilter = "Images (*.png;.jpg;.apng;.gif;.webp)|*.png;*.jpg;*.apng;*.gif;*.webp|All files|*.*";

        /// <summary>
        /// Dict containing the dll and dll file for all external dependencies needed to load all supported file formats
        /// </summary>
        private static readonly Dictionary<string, string> dllDependencies = new Dictionary<string, string> 
        {
            { "webp", "libwebp.lib" }
        };

        /// <summary>
        /// Read the file signature from a byte array to deterimine its file format.
        /// https://www.garykessler.net/library/file_sigs.html
        /// </summary>
        /// <param name="imageBytes">Byte array containing the image</param>
        /// <returns>ImageFormat</returns>
        public static ImageFormat GetContentType(byte[] imageBytes)
        {
            foreach (var kvPair in imageFormatDecoders.OrderByDescending(x => x.Key.Length))
                if (kvPair.Key.Length <= imageBytes.Length && imageBytes.StartsWith(kvPair.Key))
                    return kvPair.Value;
            return ImageFormat.Unrecognized;
        }

        public static Texture2D LoadTexture2DFromBytes(byte[] texBytes, TextureFormat format, bool mipmaps)
        {
            Texture2D tex = null;
            var imageFormat = GetContentType(texBytes);

            //Only use magic numbers for custom supported image formats. Let LoadImage handle png/jpg/unknown
            if (imageFormat == ImageFormat.WebP)
            {
                tex = WebP.Texture2DExt.CreateTexture2DFromWebP(texBytes, mipmaps, false, out var error);
                if (error != WebP.Error.Success) tex = null;
            }

            //Always fall back to default load method if all others were skipped/failed
            if (tex == null)
            {
                //LoadImage automatically resizes the texture so the texture size doesn't matter here
                tex = new Texture2D(2, 2, format, mipmaps);
                tex.LoadImage(texBytes);
            }
            return tex;
        }

        internal static int CalculateTextureBytes(int width, int height, bool mipmap)
        {
            int bytes = width * height * 4;

            if (!mipmap)
                return bytes;

            while (width > 1 || height > 1)
            {
                width = (width + 1) >> 1;
                height = (height + 1) >> 1;
                bytes += width * height * 4;
            }

            return bytes;
        }

        /// <summary>
        /// Load all needed dependencies for loading all the supported file formats.
        /// Needs to be to be run before any classes using these dependencies are loaded!
        /// </summary>
        internal static void LoadDependencies(Type pluginType)
        {
            foreach(var dependency in dllDependencies)
                try
                {
                    LoadDependency(dependency.Key, dependency.Value, pluginType);
                }
                catch
                {
                    System.Console.WriteLine($"Failed to load {dependency.Value}");
                }
        }

        private static bool StartsWith(this byte[] thisBytes, byte[] thatBytes)
        {
            for (int i = 0; i < thatBytes.Length; i += 1)
            {
                if (thisBytes[i] != thatBytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        //Site that lists magic numbers for file formats https://www.garykessler.net/library/file_sigs.html
        private static Dictionary<byte[], ImageFormat> imageFormatDecoders = new Dictionary<byte[], ImageFormat>()
        {
            { new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, ImageFormat.Png },
            { new byte[]{ 0xff, 0xd8 }, ImageFormat.Jpeg },
            { new byte[]{ 0x52, 0x49, 0x46, 0x46 }, ImageFormat.WebP },
            { new byte[]{ 0x00, 0x00, 0x00 }, ImageFormat.Avif },
            { new byte[]{ 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, ImageFormat.Unrecognized},
        };

        public enum ImageFormat
        {
            Png,
            Jpeg,
            WebP,
            Avif,
            Unrecognized
        }


        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);
        [DllImport("__Internal", CharSet = CharSet.Ansi)]
        private static extern void mono_dllmap_insert(IntPtr assembly, string dll, string func, string tdll, string tfunc);

        private static void LoadDependency(string dllName, string dllFileName, Type pluginType)
        {
            var assemblyPath = Path.GetDirectoryName(pluginType.Assembly.Location);
            var nativeDllPath = Path.Combine(assemblyPath, dllFileName);
            if (LoadLibrary(nativeDllPath) == IntPtr.Zero)
                throw new IOException($"Failed to load {nativeDllPath}, verify that the file exists and is not corrupted.");
            // Needed to let the non-standard extension to work with dllimport
            mono_dllmap_insert(IntPtr.Zero, dllName, null, dllFileName, null);
        }
    }
}
