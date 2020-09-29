using Studio;
using UnityEngine;

namespace KK_Plugins.StudioCustomMasking
{
    public class DrawColliderLines : MonoBehaviour
    {
        private Material _LineMaterial;
        public Material LineMaterial
        {
            get
            {
                if (_LineMaterial == null)
                {
                    AssetBundle bundle = AssetBundle.LoadFromMemory(UILib.Resource.LoadEmbeddedResource($"{nameof(KK_Plugins)}.Resources.colorontop.unity3d"));
                    Shader shader = bundle.LoadAsset<Shader>("ColorOnTop");
                    bundle.Unload(false);

                    //LineColor.a = 0.75f;
                    _LineMaterial = new Material(shader);
                    _LineMaterial.color = StudioCustomMasking.ColliderColor.Value;
                }
                return _LineMaterial;
            }
            set => _LineMaterial = value;
        }

        /// <summary>
        /// OnPostRender only works if this MB is attached to a camera
        /// </summary>
        private void OnPostRender()
        {
            if (StudioCustomMasking.HideLines) return;

            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
                if (Studio.Studio.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    if (objectCtrlInfo is OCIFolder ociFolder)
                        if (StudioCustomMasking.SceneControllerInstance.MaskingFolders.ContainsKey(ociFolder.objectInfo.dicKey))
                        {
                            var collider = ociFolder.objectItem.GetComponent<BoxCollider>();
                            if (collider != null)
                                DrawBox(collider);
                        }
        }

        /// <summary>
        /// Draw a box
        /// </summary>
        /// <param name="box">A box</param>
        private void DrawBox(BoxCollider box)
        {
            if (box == null) return;

            var boxTransform = box.transform;
            var min = box.center - box.size * 0.5f;
            var max = box.center + box.size * 0.5f;

            var P000 = boxTransform.TransformPoint(new Vector3(min.x, min.y, min.z));
            var P001 = boxTransform.TransformPoint(new Vector3(min.x, min.y, max.z));
            var P010 = boxTransform.TransformPoint(new Vector3(min.x, max.y, min.z));
            var P011 = boxTransform.TransformPoint(new Vector3(min.x, max.y, max.z));
            var P100 = boxTransform.TransformPoint(new Vector3(max.x, min.y, min.z));
            var P101 = boxTransform.TransformPoint(new Vector3(max.x, min.y, max.z));
            var P110 = boxTransform.TransformPoint(new Vector3(max.x, max.y, min.z));
            var P111 = boxTransform.TransformPoint(new Vector3(max.x, max.y, max.z));

            //Draw the rest of the owl
            DrawLine(P000, P001);
            DrawLine(P001, P011);
            DrawLine(P011, P010);
            DrawLine(P010, P000);
            DrawLine(P100, P101);
            DrawLine(P101, P111);
            DrawLine(P111, P110);
            DrawLine(P110, P100);
            DrawLine(P000, P100);
            DrawLine(P001, P101);
            DrawLine(P011, P111);
            DrawLine(P010, P110);
        }

        /// <summary>
        /// Draw a line from point1 to point2
        /// </summary>
        /// <param name="point1">Start point</param>
        /// <param name="point2">End point</param>
        private void DrawLine(Vector3 point1, Vector3 point2)
        {
            GL.Begin(GL.LINES);
            LineMaterial.SetPass(0);
            GL.Color(new Color(LineMaterial.color.r, LineMaterial.color.g, LineMaterial.color.b, LineMaterial.color.a));
            GL.Vertex3(point1.x, point1.y, point1.z);
            GL.Vertex3(point2.x, point2.y, point2.z);
            GL.End();
        }
    }
}
