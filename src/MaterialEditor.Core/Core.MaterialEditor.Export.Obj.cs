using System.IO;
using System.Text;
using UnityEngine;

namespace KK_Plugins.MaterialEditor
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
            string filename = Path.Combine(MaterialEditorPlugin.ExportPath, $"{rend.NameFormatted()}.obj");
            using (StreamWriter sw = new StreamWriter(filename))
            {
                string mesh = MeshToObjString(rend);
                if (!mesh.IsNullOrEmpty())
                {
                    sw.Write(mesh);
                    MaterialEditorPlugin.Logger.LogInfo($"Exported {filename}");
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

                for (var i = 0; i < subMesh.vertices.Length; i++)
                {
                    Vector3 v = subMesh.vertices[i];
                    sb.Append($"v {-v.x:0.000000} {v.y:0.000000} {v.z:0.000000}\n");
                }

                for (var i = 0; i < subMesh.uv.Length; i++)
                {
                    Vector3 v = subMesh.uv[i];
                    sb.Append($"vt {v.x:0.000000} {v.y:0.000000}\n");
                }

                for (var i = 0; i < subMesh.normals.Length; i++)
                {
                    Vector3 v = subMesh.normals[i];
                    sb.Append($"vn {-v.x:0.000000} {v.y:0.000000} {v.z:0.000000}\n");
                }

                int[] triangles = mesh.GetTriangles(x);
                for (int i = 0; i < triangles.Length; i += 3)
                    sb.Append($"f {triangles[i] + 1}/{triangles[i] + 1}/{triangles[i] + 1} {triangles[i + 2] + 1}/{triangles[i + 2] + 1}/{triangles[i + 2] + 1} {triangles[i + 1] + 1}/{triangles[i + 1] + 1}/{triangles[i + 1] + 1}\n");
            }
            return sb.ToString();
        }
    }
}