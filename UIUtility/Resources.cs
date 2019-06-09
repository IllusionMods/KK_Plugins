using System;
using System.IO;
using System.Reflection;

namespace UILib
{
    internal static class Resource
    {
        public static string Namespace { get; set; }
        public static byte[] DefaultResourceKOI => LoadEmbeddedResource($"{Namespace}.Resources.DefaultResourcesKOI.unity3d");

        public static byte[] LoadEmbeddedResource(string resourceName)
        {
            try
            {
                var ass = Assembly.GetExecutingAssembly();
                using (var stream = ass.GetManifestResourceStream(resourceName))
                {
                    byte[] buffer = new byte[16 * 1024];
                    return ReadFully(stream);
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Error accessing resources ({resourceName})");
                throw;
            }
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
