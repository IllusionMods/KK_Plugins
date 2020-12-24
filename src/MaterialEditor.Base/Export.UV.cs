using System.IO;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Contains methods for exporting the renderer data in various formats
    /// </summary>
    public static partial class Export
    {
        /// <summary>
        /// Exports the UV map(s) of the SkinnedMeshRenderer or MeshRenderer
        /// </summary>
        public static void ExportUVMaps(Renderer rend)
        {
            bool openedFile = false;
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            var lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);

            Mesh mr;
            if (rend is MeshRenderer meshRenderer)
                mr = meshRenderer.GetComponent<MeshFilter>().mesh;
            else if (rend is SkinnedMeshRenderer skinnedMeshRenderer)
                mr = skinnedMeshRenderer.sharedMesh;
            else return;

            for (int x = 0; x < mr.subMeshCount; x++)
            {
                var tris = mr.GetTriangles(x);
                var uvs = mr.uv;

                const int size = 4096;
                var _renderTexture = RenderTexture.GetTemporary(size, size);
                var lineColor = Color.black;
                Graphics.SetRenderTarget(_renderTexture);
                GL.PushMatrix();
                GL.LoadOrtho();
                //GL.LoadPixelMatrix(); // * 2 - 1, maps differently.
                GL.Clear(false, true, Color.clear);

                lineMaterial.SetPass(0);
                GL.Begin(GL.LINES);
                GL.Color(lineColor);

                for (var i = 0; i < tris.Length; i += 3)
                {
                    var v = uvs[tris[i]];
                    var n1 = uvs[tris[i + 1]];
                    var n2 = uvs[tris[i + 2]];

                    GL.Vertex(v);
                    GL.Vertex(n1);

                    GL.Vertex(v);
                    GL.Vertex(n2);

                    GL.Vertex(n1);
                    GL.Vertex(n2);
                }
                GL.End();

                GL.PopMatrix();
                Graphics.SetRenderTarget(null);

                var png = MaterialEditorPluginBase.GetT2D(_renderTexture);
                RenderTexture.ReleaseTemporary(_renderTexture);

                string filename = Path.Combine(MaterialEditorPluginBase.ExportPath, $"{rend.NameFormatted()}_{x}.png");
                File.WriteAllBytes(filename, png.EncodeToPNG());
                Object.DestroyImmediate(png);
                MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
                if (!openedFile)
                    Utilities.OpenFileInExplorer(filename);
                openedFile = true;
            }
        }
    }
}
