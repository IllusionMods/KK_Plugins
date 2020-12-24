using System.IO;
using System.Text;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Contains methods for exporting the renderer data in various formats
    /// </summary>
    public static partial class Export
    {
        /// <summary>
        /// Exports the mesh of the SkinnedMeshRenderer or MeshRenderer
        /// </summary>
        public static void ExportObj(Renderer rend)
        {
            string filename = Path.Combine(MaterialEditorPluginBase.ExportPath, $"{rend.NameFormatted()}.obj");
            using (StreamWriter sw = new StreamWriter(filename))
            {
                string mesh = MeshToObjString(rend);
                if (!mesh.IsNullOrEmpty())
                {
                    sw.Write(mesh);
                    MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
                    Utilities.OpenFileInExplorer(filename);
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

                sb.AppendLine($"g {rend.NameFormatted()}_{x}");

                for (var i = 0; i < subMesh.vertices.Length; i++)
                {
                    Vector3 v = subMesh.vertices[i];
                    sb.AppendLine($"v {-v.x} {v.y} {v.z}");
                }

                for (var i = 0; i < subMesh.uv.Length; i++)
                {
                    Vector3 v = subMesh.uv[i];
                    sb.AppendLine($"vt {v.x} {v.y}");
                }

                for (var i = 0; i < subMesh.normals.Length; i++)
                {
                    Vector3 v = subMesh.normals[i];
                    sb.AppendLine($"vn {-v.x} {v.y} {v.z}");
                }

                int[] triangles = subMesh.GetTriangles(x);
                for (int i = 0; i < triangles.Length; i += 3)
                    sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[i] + 1, triangles[i + 2] + 1, triangles[i + 1] + 1);
            }
            return sb.ToString();
        }
    }
}