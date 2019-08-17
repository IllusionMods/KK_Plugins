using CommonCode;
using System.IO;
using System.Text;
using UnityEngine;

namespace KK_MaterialEditor
{
    public partial class KK_MaterialEditor
    {
        public static partial class Export
        {
            /// <summary>
            /// Exports the mesh of the SkinnedMeshRenderer or MeshRenderer
            /// </summary>
            public static void ExportObj(Renderer rend)
            {
                string filename = Path.Combine(ExportPath, $"{rend.NameFormatted()}.obj");
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    string mesh = MeshToObjString(rend);
                    if (!mesh.IsNullOrEmpty())
                    {
                        sw.Write(mesh);
                        CC.Log($"Exported {filename}");
                        CC.OpenFileInExplorer(filename);
                    }
                }
            }

            private static string MeshToObjString(Renderer rend)
            {
                Mesh m;
                if (rend is MeshRenderer meshRenderer)
                    m = meshRenderer.GetComponent<MeshFilter>().mesh;
                else if (rend is SkinnedMeshRenderer skinnedMeshRenderer)
                    m = skinnedMeshRenderer.sharedMesh;
                else return "";

                StringBuilder sb = new StringBuilder();

                sb.Append("g ").Append(rend.name).Append("\n");

                foreach (Vector3 v in m.vertices)
                    sb.Append($"v {v.x} {v.y} {v.z}\n");
                sb.Append("\n");

                foreach (Vector3 v in m.normals)
                    sb.Append($"vn {v.x} {v.y} {v.z}\n");
                sb.Append("\n");


                foreach (Vector3 v in m.uv)
                    sb.Append($"vt {v.x} {v.y}\n");

                for (int material = 0; material < m.subMeshCount; material++)
                {
                    sb.Append("\n");

                    int[] triangles = m.GetTriangles(material);
                    for (int i = 0; i < triangles.Length; i += 3)
                        sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
                }
                return sb.ToString();
            }
        }
    }
}