//https://github.com/octo-code/webp-unity3d
using System;
using System.Text;
using System.Collections.Generic;

namespace WebP
{
    public class Info
    {
        public static string GetDecoderVersion()
        {
            uint v = (uint)WebP.Extern.NativeBindings.WebPGetDecoderVersion();
            var revision = v % 256;
            var minor = (v >> 8) % 256;
            var major = (v >> 16) % 256;
            return major + "." + minor + "." + revision;
        }

        public static string GetEncoderVersion()
        {
            uint v = (uint)WebP.Extern.NativeBindings.WebPGetEncoderVersion();
            var revision = v % 256;
            var minor = (v >> 8) % 256;
            var major = (v >> 16) % 256;
            return major + "." + minor + "." + revision;
        }
    }
}
