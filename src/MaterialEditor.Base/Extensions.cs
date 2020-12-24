using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MaterialEditorAPI
{
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
        public static string NameFormatted(this GameObject go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Material go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Renderer go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Shader go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        public static string NameFormatted(this Mesh go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
        /// <summary>
        /// Convert string to Color
        /// </summary>
        public static Color ToColor(this string color)
        {
            var segments = color.Split(',');
            if (color.Length >= 3)
            {
                if (float.TryParse(segments[0], out float r) && float.TryParse(segments[1], out float g) && float.TryParse(segments[2], out float b))
                {
                    var c = new Color(r, g, b);
                    if (segments.Length == 4 && float.TryParse(segments[3], out float a))
                        c.a = a;
                    return c;
                }
            }
            return Color.white;
        }

        public static GameObject FindLoop(this Transform transform, string name)
        {
            if (string.Compare(name, transform.gameObject.name) == 0)
                return transform.gameObject;

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject gameObject = transform.GetChild(i).FindLoop(name);
                if (gameObject != null)
                    return gameObject;
            }
            return null;
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component => gameObject == null ? null : gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();

        public static bool IsNullOrEmpty(this string self) => string.IsNullOrEmpty(self);

        public static bool IsNullOrEmpty(this string[] args, int index)
        {
            bool ret = false;
            args.SafeGet(index).SafeProc(delegate (string s)
            {
                ret = !s.IsNullOrEmpty();
            });
            return !ret;
        }

        public static bool IsNullOrEmpty(this List<string> args, int index)
        {
            bool ret = false;
            args.SafeGet(index).SafeProc(delegate (string s)
            {
                ret = !s.IsNullOrEmpty();
            });
            return !ret;
        }

        public static bool IsNullOrEmpty<T>(this IList<T> self) => self == null || self.Count == 0;

        public static bool IsNullOrEmpty<T>(this List<T> self) => self == null || self.Count == 0;

        public static bool IsNullOrEmpty(this MulticastDelegate self) => self == null || self.GetInvocationList() == null || self.GetInvocationList().Length == 0;

        public static bool IsNullOrEmpty(this UnityEvent self) => self == null || self.GetPersistentEventCount() == 0;

        public static bool IsNullOrEmpty(this UnityEvent self, int target) => self.IsNullOrEmpty() || self.GetPersistentTarget(target) == null || self.GetPersistentMethodName(target).IsNullOrEmpty();

        public static T SafeGet<T>(this T[] array, int index) => array == null ? default : (uint)index >= array.Length ? default : array[index];

        public static bool SafeProc<T>(this T[] array, int index, Action<T> act) => array.SafeGet(index).SafeProc(act);

        public static T SafeGet<T>(this List<T> list, int index) => list == null ? default : (uint)index >= list.Count ? default : list[index];

        public static bool SafeProc<T>(this List<T> list, int index, Action<T> act) => list.SafeGet(index).SafeProc(act);

        public static bool SafeProc(this string[] args, int index, Action<string> act)
        {
            if (args.IsNullOrEmpty(index))
                return false;
            act.Call(args[index]);
            return true;
        }

        public static bool SafeProc(this List<string> args, int index, Action<string> act)
        {
            if (args.IsNullOrEmpty(index))
                return false;
            act.Call(args[index]);
            return true;
        }

        public static bool SafeProc<T>(this T self, Action<T> act)
        {
            bool flag = self != null;
            if (flag)
                act.Call(self);
            return flag;
        }

        public static bool SafeProcObject<T>(this T self, Action<T> act) where T : UnityEngine.Object
        {
            bool flag = self != null;
            if (flag)
                act.Call(self);
            return flag;
        }

        public static void Call(this Action action) => action?.Invoke();

        public static void Call<T>(this Action<T> action, T arg) => action?.Invoke(arg);

        public static void Call<T1, T2>(this Action<T1, T2> action, T1 arg1, T2 arg2) => action?.Invoke(arg1, arg2);

        public static void Call<T1, T2, T3>(this Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) => action?.Invoke(arg1, arg2, arg3);

        public static TResult Call<TResult>(this Func<TResult> func, TResult result = default) => func == null ? result : func();

        public static TResult Call<T, TResult>(this Func<T, TResult> func, T arg, TResult result = default) => func == null ? result : func(arg);

        public static TResult Call<T1, T2, TResult>(this Func<T1, T2, TResult> func, T1 arg1, T2 arg2, TResult result = default) => func == null ? result : func(arg1, arg2);

        public static TResult Call<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3, TResult result = default) => func == null ? result : func(arg1, arg2, arg3);
    }

    internal static class MeshExtensions
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
            private List<Vector3> verts;
            private List<Vector2> uv1;
            private List<Vector2> uv2;
            private List<Vector2> uv3;
            private List<Vector2> uv4;
            private List<Vector3> normals;
            private List<Vector4> tangents;
            private List<Color32> colors;
            private List<BoneWeight> boneWeights;

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

            private static List<T> CreateList<T>(T[] source)
            {
                if (source == null || source.Length == 0)
                    return null;
                return new List<T>(source);
            }
            private static void Copy<T>(ref List<T> dest, List<T> source, int index)
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
