using System.Collections.Generic;
using UnityEngine;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins
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

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null) return null;
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }
    }

#if !PC && !SBPR && !HS
    internal static class CharacterExtensions
    {
#if PH
        public static GameObject[] GetClothes(this Human human) => human.wears.objWear;
        public static GameObject GetClothes(this Human human, int index) => human.wears.objWear[index];
        public static GameObject[] GetHair(this Human human) => human.hairs.objHairs;
        public static GameObject GetHair(this Human human, int index) => human.hairs.objHairs[index];
        public static GameObject GetHead(this Human human) => human.head.Obj;
        public static GameObject GetAccessory(this Human human, int index) => human.accessories.objAcs[index];
#else
        public static string GetCharacterName(this ChaControl chaControl) => chaControl.chaFile.parameter.fullname.Trim();
        public static GameObject[] GetClothes(this ChaControl chaControl) => chaControl.objClothes;
        public static GameObject GetClothes(this ChaControl chaControl, int index)
        {
#if EC
            if (index > 7)
                return null;
#endif
            return chaControl.objClothes[index];
        }
        public static GameObject[] GetHair(this ChaControl chaControl) => chaControl.objHair;
        public static GameObject GetHair(this ChaControl chaControl, int index) => chaControl.objHair[index];
        public static GameObject GetHead(this ChaControl chaControl) => chaControl.objHead;
#endif
    }
#endif

#if !PC && !SBPR && !EC
    internal static class StudioExtensions
    {
        public static string GetPatternPath(this Studio.OCIItem ociItem, int index)
        {
#if KK || KKS
            return ociItem.itemInfo.pattern[index].filePath;
#elif AI || HS2
            return ociItem.itemInfo.colors[index].pattern.filePath;
#else
            throw new System.NotImplementedException("StudioExtensions.GetPatternPath");
#endif
        }

        public static void SetPatternPath(this Studio.OCIItem ociItem, int index, string filePath)
        {
#if KK || KKS
            ociItem.itemInfo.pattern[index].filePath = filePath;
#elif AI || HS2
            ociItem.itemInfo.colors[index].pattern.filePath = filePath;
#else
            throw new System.NotImplementedException("StudioExtensions.SetPatternPath");
#endif
        }
    }
#endif

#if !PC && !SBPR
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
#endif
}
