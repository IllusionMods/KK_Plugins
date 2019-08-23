using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using UnityEngine;

namespace CommonCode
{
    internal class CC
    {
        private static int _language = -1;
        /// <summary>
        /// Safely get the language as configured in setup.xml if it exists.
        /// </summary>
        public static int Language
        {
            get
            {
                if (_language == -1)
                {
                    try
                    {
                        var dataXml = XElement.Load("UserData/setup.xml");

                        if (dataXml != null)
                        {
                            IEnumerable<XElement> enumerable = dataXml.Elements();
                            foreach (XElement xelement in enumerable)
                            {
                                if (xelement.Name.ToString() == "Language")
                                {
                                    _language = int.Parse(xelement.Value);
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        _language = 0;
                    }
                    finally
                    {
                        if (_language == -1)
                            _language = 0;
                    }
                }

                return _language;
            }
        }
        /// <summary>
        /// Open explorer focused on the specified file or directory
        /// </summary>
        internal static void OpenFileInExplorer(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));

            try { NativeMethods.OpenFolderAndSelectFile(filename); }
            catch (Exception) { Process.Start("explorer.exe", $"/select, \"{filename}\""); }
        }
        internal static class NativeMethods
        {
            /// <summary>
            /// Open explorer focused on item. Reuses already opened explorer windows unlike Process.Start
            /// </summary>
            public static void OpenFolderAndSelectFile(string filename)
            {
                var pidl = ILCreateFromPathW(filename);
                SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
                ILFree(pidl);
            }

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr ILCreateFromPathW(string pszPath);

            [DllImport("shell32.dll")]
            private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

            [DllImport("shell32.dll")]
            private static extern void ILFree(IntPtr pidl);
        }

        internal static void Log(string text) => BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Info, text);
        internal static void Log(BepInEx.Logging.LogLevel level, string text) => BepInEx.Logger.Log(level, text);
        internal static void Log(object text) => BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Info, text?.ToString());
        internal static void Log(BepInEx.Logging.LogLevel level, object text) => BepInEx.Logger.Log(level, text?.ToString());
        internal static void StackTrace() => Log(new System.Diagnostics.StackTrace());

        internal static class Paths
        {
            internal static readonly string FemaleCardPath = Path.Combine(UserData.Path, "chara/female/");
            internal static readonly string MaleCardPath = Path.Combine(UserData.Path, "chara/male/");
            internal static readonly string CoordinateCardPath = Path.Combine(UserData.Path, "coordinate/");
        }
    }

    internal static class Extensions
    {
        private static readonly System.Random rng = new System.Random();
        public static void Randomize<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public static string NameFormatted(this GameObject go) => go?.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Material go) => go?.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Renderer go) => go?.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Shader go) => go?.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Mesh go) => go?.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        /// <summary>
        /// Convert string to Color
        /// </summary>
        public static Color ToColor(this string color)
        {
            var segments = color.Split(',');
            if (color.Length >= 3)
            {
                if (float.TryParse(segments[0], out float r) &&
                    float.TryParse(segments[1], out float g) &&
                    float.TryParse(segments[2], out float b))
                {
                    var c = new Color(r, g, b);
                    if (segments.Length == 4 && float.TryParse(segments[3], out float a))
                        c.a = a;
                    return c;
                }
            }
            return Color.white;
        }
    }

    internal static class MeshExtension
    {
        public static Mesh Submesh(this Mesh mesh, int submeshIndex)
        {
            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                return null;
            int[] indices = mesh.GetTriangles(submeshIndex);
            Vertices source = new Vertices(mesh);
            Vertices dest = new Vertices();
            Dictionary<int, int> map = new Dictionary<int, int>();
            int[] newIndices = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                int o = indices[i];
                if (!map.TryGetValue(o, out int n))
                {
                    n = dest.Add(source, o);
                    map.Add(o, n);
                }
                newIndices[i] = n;
            }
            Mesh submesh = new Mesh();
            dest.AssignTo(submesh);
            submesh.triangles = newIndices;
            submesh.name = $"{mesh.NameFormatted()}_{submeshIndex}";
            return submesh;
        }

        private class Vertices
        {
            private List<Vector3> verts = null;
            private List<Vector2> uv1 = null;
            private List<Vector2> uv2 = null;
            private List<Vector2> uv3 = null;
            private List<Vector2> uv4 = null;
            private List<Vector3> normals = null;
            private List<Vector4> tangents = null;
            private List<Color32> colors = null;
            private List<BoneWeight> boneWeights = null;

            public Vertices() => verts = new List<Vector3>();
            public Vertices(Mesh mesh)
            {
                verts = CreateList(mesh.vertices);
                uv1 = CreateList(mesh.uv);
                uv2 = CreateList(mesh.uv2);
                uv3 = CreateList(mesh.uv3);
                uv4 = CreateList(mesh.uv4);
                normals = CreateList(mesh.normals);
                tangents = CreateList(mesh.tangents);
                colors = CreateList(mesh.colors32);
                boneWeights = CreateList(mesh.boneWeights);
            }

            private List<T> CreateList<T>(T[] source)
            {
                if (source == null || source.Length == 0)
                    return null;
                return new List<T>(source);
            }
            private void Copy<T>(ref List<T> dest, List<T> source, int index)
            {
                if (source == null)
                    return;
                if (dest == null)
                    dest = new List<T>();
                dest.Add(source[index]);
            }
            public int Add(Vertices other, int index)
            {
                int i = verts.Count;
                Copy(ref verts, other.verts, index);
                Copy(ref uv1, other.uv1, index);
                Copy(ref uv2, other.uv2, index);
                Copy(ref uv3, other.uv3, index);
                Copy(ref uv4, other.uv4, index);
                Copy(ref normals, other.normals, index);
                Copy(ref tangents, other.tangents, index);
                Copy(ref colors, other.colors, index);
                Copy(ref boneWeights, other.boneWeights, index);
                return i;
            }
            public void AssignTo(Mesh target)
            {
                target.SetVertices(verts);
                if (uv1 != null) target.SetUVs(0, uv1);
                if (uv2 != null) target.SetUVs(1, uv2);
                if (uv3 != null) target.SetUVs(2, uv3);
                if (uv4 != null) target.SetUVs(3, uv4);
                if (normals != null) target.SetNormals(normals);
                if (tangents != null) target.SetTangents(tangents);
                if (colors != null) target.SetColors(colors);
                if (boneWeights != null) target.boneWeights = boneWeights.ToArray();
            }
        }
    }
}
