using System.Collections.Generic;
using System.Linq;
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

        public static void LoadTexture2DFromBytes(byte[] texBytes, ref Texture2D tex)
        {
            var imageFormat = GetContentType(texBytes);
            //Only use magic numbers for custom supported image formats. Let LoadImage handle png/jpg/unknown
            if (imageFormat == ImageFormat.WebP)
                tex = WebP.Texture2DExt.CreateTexture2DFromWebP(texBytes, tex.mipmapCount > 1, false, out var error);
            else
                tex.LoadImage(texBytes);
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
    }
}
