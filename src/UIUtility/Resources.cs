using System;
using System.IO;
using System.Reflection;

namespace UILib
{
    internal static class Resource
    {
        private static byte[] _DefaultResources;
        public static byte[] DefaultResources
        {
            get
            {
                if (_DefaultResources == null)
                {
                    var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                    for (int i = 0; i < resources.Length; i++)
                    {
                        var resource = resources[i];
                        if (resource.EndsWith("Resources.DefaultResources.unity3d"))
                            _DefaultResources = LoadEmbeddedResource(resource);
                    }
                }
                return _DefaultResources;
            }
        }

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
