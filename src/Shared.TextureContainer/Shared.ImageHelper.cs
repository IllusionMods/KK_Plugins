using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KK_Plugins
{
    //Shamelessly stolen from https://stackoverflow.com/a/1246008
    public static class ImageHelper
    {
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
