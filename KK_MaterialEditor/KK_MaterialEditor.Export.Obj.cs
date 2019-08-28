using KK_Plugins.CommonCode;
using System.IO;
using System.Text;
using UnityEngine;

namespace KK_Plugins
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
                        Logger.LogInfo($"Exported {filename}");
                        CC.OpenFileInExplorer(filename);
                    }
                }
            }

            private static string MeshToObjString(Renderer rend)
            {
                Mesh mesh;
                if (rend is MeshRenderer meshRenderer)
                    mesh = meshRenderer.GetComponent<MeshFilter>().mesh;
                else if (rend is SkinnedMeshRenderer skinnedMeshRenderer)
                    mesh = skinnedMeshRenderer.sharedMesh;
                else return "";

                StringBuilder sb = new StringBuilder();

                for (int x = 0; x < mesh.subMeshCount; x++)
                {
                    Mesh subMesh = mesh.Submesh(x);

                    sb.Append("g ").Append($"{rend.NameFormatted()}_{x}").Append("\n");

                    foreach (Vector3 v in subMesh.vertices)
                        sb.Append(string.Format("v {0:0.000000} {1:0.000000} {2:0.000000}\n", -v.x, v.y, v.z));

                    foreach (Vector3 v in subMesh.uv)
                        sb.Append(string.Format("vt {0:0.000000} {1:0.000000}\n", v.x, v.y));

                    foreach (Vector3 v in subMesh.normals)
                        sb.Append(string.Format("vn {0:0.000000} {1:0.000000} {2:0.000000}\n", -v.x, v.y, v.z));

                    int[] triangles = mesh.GetTriangles(x);
                    for (int i = 0; i < triangles.Length; i += 3)
                        sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[i] + 1, triangles[i + 2] + 1, triangles[i + 1] + 1));
                }
                return sb.ToString();
            }
        }
    }
}