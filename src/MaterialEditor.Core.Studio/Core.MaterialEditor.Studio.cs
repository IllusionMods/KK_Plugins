using BepInEx.Bootstrap;
using KKAPI.Studio.SaveLoad;
using Studio;
using System.Linq;
using UILib;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static KK_Plugins.MaterialEditor.MaterialAPI;
#if AI || HS2
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    public partial class MEStudio : UI
    {
        public const string GUID = MaterialEditorPlugin.GUID + ".studio";
        public const string PluginName = MaterialEditorPlugin.PluginName + " Studio";
        public const string Version = MaterialEditorPlugin.Version;

        internal void Start()
        {
            SceneManager.sceneLoaded += (s, lsm) => InitStudioUI(s.name);
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(MaterialEditorPlugin.GUID);
        }

        private void InitStudioUI(string sceneName)
        {
            if (sceneName != "Studio") return;
            SceneManager.sceneLoaded -= (s, lsm) => InitStudioUI(s.name);

            InitUI();

            RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar/Button Route").GetComponent<RectTransform>();
            Button materialEditorButton = Instantiate(original.gameObject).GetComponent<Button>();
            RectTransform materialEditorButtonRectTransform = materialEditorButton.transform as RectTransform;
            materialEditorButton.transform.SetParent(original.parent, true);
            materialEditorButton.transform.localScale = original.localScale;
            materialEditorButtonRectTransform.SetRect(original.anchorMin, original.anchorMax, original.offsetMin, original.offsetMax);
            materialEditorButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(-48f, 0f);

            Texture2D texture2D = new Texture2D(32, 32);
            texture2D.LoadImage(LoadIcon());
            var MatEditorIcon = materialEditorButton.targetGraphic as Image;
            MatEditorIcon.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
            MatEditorIcon.color = Color.white;

            materialEditorButton.onClick = new Button.ButtonClickedEvent();
            materialEditorButton.onClick.AddListener(() => { PopulateListStudio(); });
        }

        private void PopulateListStudio()
        {
            if (Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes.Length != 1)
                return;

            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
                if (Studio.Studio.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    if (objectCtrlInfo is OCIItem ociItem)
                        PopulateList(ociItem.objectItem, slot: GetObjectID(objectCtrlInfo));
                    else if (objectCtrlInfo is OCIChar ociChar)
                        PopulateList(ociChar.charInfo.gameObject, slot: GetObjectID(objectCtrlInfo));
        }

        public static int GetObjectID(ObjectCtrlInfo oci) => Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == oci).Key;

        public static SceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<SceneController>();

        internal override string GetRendererPropertyValueOriginal(int slot, Renderer renderer, RendererProperties property, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetRendererPropertyValueOriginal(slot, renderer, property, gameObject);
            else
                return GetSceneController().GetRendererPropertyValueOriginal(slot, renderer, property);
        }
        internal override void AddRendererProperty(int slot, Renderer renderer, RendererProperties property, string value, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddRendererProperty(slot, renderer, property, value, gameObject);
            else
                GetSceneController().AddRendererProperty(slot, renderer, property, value);
        }
        internal override void RemoveRendererProperty(int slot, Renderer renderer, RendererProperties property, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveRendererProperty(slot, renderer, property, gameObject);
            else
                GetSceneController().RemoveRendererProperty(slot, renderer, property);
        }

        internal override string GetMaterialShaderNameOriginal(int slot, Material material, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialShaderOriginal(slot, material, gameObject);
            else
                return GetSceneController().GetMaterialShaderOriginal(slot, material);
        }
        internal override void AddMaterialShaderName(int slot, Material material, string value, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialShader(slot, material, value, gameObject);
            else
                GetSceneController().AddMaterialShader(slot, material, value);
        }
        internal override void RemoveMaterialShaderName(int slot, Material material, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialShader(slot, material, gameObject);
            else
                GetSceneController().RemoveMaterialShader(slot, material);
        }

        internal override int? GetMaterialShaderRenderQueueOriginal(int slot, Material material, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialShaderRenderQueueOriginal(slot, material, gameObject);
            else
                return GetSceneController().GetMaterialShaderRenderQueueOriginal(slot, material);
        }
        internal override void AddMaterialShaderRenderQueue(int slot, Material material, int value, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialShaderRenderQueue(slot, material, value, gameObject);
            else
                GetSceneController().AddMaterialShaderRenderQueue(slot, material, value);
        }
        internal override void RemoveMaterialShaderRenderQueue(int slot, Material material, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialShaderRenderQueue(slot, material, gameObject);
            else
                GetSceneController().RemoveMaterialShaderRenderQueue(slot, material);
        }

        internal override bool GetMaterialTextureValueOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureOriginal(slot, material, propertyName, gameObject);
            else
                return GetSceneController().GetMaterialTextureOriginal(slot, material, propertyName);
        }
        internal override void AddMaterialTexture(int slot, Material material, string propertyName, string filePath, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialTextureFromFile(slot, material, propertyName, filePath, gameObject, true);
            else
                GetSceneController().AddMaterialTextureFromFile(slot, material, propertyName, filePath, true);
        }
        internal override void RemoveMaterialTexture(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTexture(slot, material, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialTexture(slot, material, propertyName);
        }

        internal override Vector2? GetMaterialTextureOffsetOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureOffsetOriginal(slot, material, propertyName, gameObject);
            else
                return GetSceneController().GetMaterialTextureOffsetOriginal(slot, material, propertyName);
        }
        internal override void AddMaterialTextureOffset(int slot, Material material, string propertyName, Vector2 value, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialTextureOffset(slot, material, propertyName, value, gameObject);
            else
                GetSceneController().AddMaterialTextureOffset(slot, material, propertyName, value);
        }
        internal override void RemoveMaterialTextureOffset(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTextureOffset(slot, material, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialTextureOffset(slot, material, propertyName);
        }

        internal override Vector2? GetMaterialTextureScaleOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureScaleOriginal(slot, material, propertyName, gameObject);
            else
                return GetSceneController().GetMaterialTextureScaleOriginal(slot, material, propertyName);
        }
        internal override void AddMaterialTextureScale(int slot, Material material, string propertyName, Vector2 value, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialTextureScale(slot, material, propertyName, value, gameObject);
            else
                GetSceneController().AddMaterialTextureScale(slot, material, propertyName, value);
        }
        internal override void RemoveMaterialTextureScale(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTextureScale(slot, material, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialTextureScale(slot, material, propertyName);
        }

        internal override Color? GetMaterialColorPropertyValueOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialColorPropertyValueOriginal(slot, material, propertyName, gameObject);
            else
                return GetSceneController().GetMaterialColorPropertyValueOriginal(slot, material, propertyName);
        }
        internal override void AddMaterialColorProperty(int slot, Material material, string propertyName, Color value, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialColorProperty(slot, material, propertyName, value, gameObject);
            else
                GetSceneController().AddMaterialColorProperty(slot, material, propertyName, value);
        }
        internal override void RemoveMaterialColorProperty(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialColorProperty(slot, material, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialColorProperty(slot, material, propertyName);
        }

        internal override string GetMaterialFloatPropertyValueOriginal(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialFloatPropertyValueOriginal(slot, material, propertyName, gameObject);
            else
                return GetSceneController().GetMaterialFloatPropertyValueOriginal(slot, material, propertyName);
        }
        internal override void AddMaterialFloatProperty(int slot, Material material, string propertyName, float value, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialFloatProperty(slot, material, propertyName, value, gameObject);
            else
                GetSceneController().AddMaterialFloatProperty(slot, material, propertyName, value);
        }
        internal override void RemoveMaterialFloatProperty(int slot, Material material, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialFloatProperty(slot, material, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialFloatProperty(slot, material, propertyName);
        }
    }
}
