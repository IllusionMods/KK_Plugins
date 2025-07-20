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

        private static Mesh GetMeshFromRenderer(Renderer rend)
        {
            switch (rend)
            {
                case MeshRenderer meshRenderer:
                    return meshRenderer.GetComponent<MeshFilter>().mesh;
                case SkinnedMeshRenderer skinnedMeshRenderer:
                    return MaterialEditorPluginBase.ExportBakedMesh.Value ? BakeMesh(skinnedMeshRenderer) : skinnedMeshRenderer.sharedMesh;
                default:
                    return null;
            }
        }

        private static Mesh BakeMesh(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            var bakedMesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(bakedMesh);
            return bakedMesh;
        }

        /// <summary>
        /// Converts the mesh data from a given Renderer into an OBJ formatted string.
        /// </summary>
        /// <param name="rend">The Renderer containing the mesh to be converted to an OBJ string. Supported types include MeshRenderer and SkinnedMeshRenderer.</param>
        /// <returns>A string representation of the mesh in OBJ format, or an empty string if the Renderer does not have a valid mesh.</returns>
        private static string MeshToObjString(Renderer rend)
        {
            Mesh mesh = GetMeshFromRenderer(rend);
            if (mesh == null) return string.Empty;

            var scale = rend.transform.lossyScale;
            var inverseScale = Matrix4x4.Scale(scale).inverse;
            StringBuilder sb = new StringBuilder();

            for (var i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 v = mesh.vertices[i];
                if (MaterialEditorPluginBase.ExportBakedMesh.Value && MaterialEditorPluginBase.ExportBakedWorldPosition.Value)
                    v = rend.transform.TransformPoint(v);
                sb.AppendLine($"v {-v.x} {v.y} {v.z}");
            }

            for (var i = 0; i < mesh.uv.Length; i++)
            {
                Vector2 uv = mesh.uv[i];
                sb.AppendLine($"vt {uv.x} {uv.y}");
            }

            for (var i = 0; i < mesh.normals.Length; i++)
            {
                Vector3 n = mesh.normals[i];
                if (MaterialEditorPluginBase.ExportBakedMesh.Value && MaterialEditorPluginBase.ExportBakedWorldPosition.Value)
                    n = rend.transform.TransformDirection(n);
                sb.AppendLine($"vn {-n.x} {n.y} {n.z}");
            }

            for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
            {
                sb.AppendLine($"g {rend.NameFormatted()}_{submeshIndex}");
                int[] triangles = mesh.GetTriangles(submeshIndex);

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int v1 = triangles[i] + 1;
                    int v2 = triangles[i + 2] + 1;
                    int v3 = triangles[i + 1] + 1;
                    sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", v1, v2, v3);
                }
            }

            return sb.ToString();
        }
    }
}
