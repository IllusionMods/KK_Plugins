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

        internal override string GetRendererPropertyValueOriginal(int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetRendererPropertyValueOriginal(coordinateIndex, slot, rendererName, property);
            else
                return GetSceneController().GetRendererPropertyValueOriginal(slot, rendererName, property);
        }
        internal override void AddRendererProperty(int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddRendererProperty(coordinateIndex, slot, rendererName, property, value, valueOriginal, gameObject);
            else
                GetSceneController().AddRendererProperty(slot, rendererName, property, value, valueOriginal, gameObject);
        }
        internal override void RemoveRendererProperty(int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveRendererProperty(coordinateIndex, slot, rendererName, property, gameObject);
            else
                GetSceneController().RemoveRendererProperty(slot, rendererName, property, gameObject);
        }

        internal override string GetMaterialShaderNameOriginal(int coordinateIndex, int slot, string materialName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialShaderOriginal(coordinateIndex, slot, materialName);
            else
                return GetSceneController().GetMaterialShaderOriginal(slot, materialName);
        }
        internal override void AddMaterialShaderName(int coordinateIndex, int slot, string materialName, string value, string valueOriginal, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialShader(coordinateIndex, slot, materialName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialShader(slot, materialName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialShaderName(int coordinateIndex, int slot, string materialName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialShader(coordinateIndex, slot, materialName, gameObject);
            else
                GetSceneController().RemoveMaterialShader(slot, materialName, gameObject);
        }

        internal override int? GetMaterialShaderRenderQueueOriginal(int coordinateIndex, int slot, string materialName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialShaderRenderQueueOriginal(coordinateIndex, slot, materialName);
            else
                return GetSceneController().GetMaterialShaderRenderQueueOriginal(slot, materialName);
        }
        internal override void AddMaterialShaderRenderQueue(int coordinateIndex, int slot, string materialName, int value, int valueOriginal, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialShaderRenderQueue(coordinateIndex, slot, materialName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialShaderRenderQueue(slot, materialName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialShaderRenderQueue(int coordinateIndex, int slot, string materialName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialShaderRenderQueue(coordinateIndex, slot, materialName, gameObject);
            else
                GetSceneController().RemoveMaterialShaderRenderQueue(slot, materialName, gameObject);
        }

        internal override bool GetMaterialTextureValueOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureOriginal(coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialTextureOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialTexture(int coordinateIndex, int slot, string materialName, string propertyName, string filePath, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialTextureFromFile(coordinateIndex, slot, materialName, propertyName, filePath, gameObject, true);
            else
                GetSceneController().AddMaterialTextureFromFile(slot, materialName, propertyName, filePath, gameObject, true);
        }
        internal override void RemoveMaterialTexture(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTexture(coordinateIndex, slot, materialName, propertyName);
            else
                GetSceneController().RemoveMaterialTexture(slot, materialName, propertyName);
        }

        internal override Vector2? GetMaterialTextureOffsetOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureOffsetOriginal(coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialTextureOffsetOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialTextureOffset(int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialTextureOffset(coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialTextureOffset(slot, materialName, propertyName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialTextureOffset(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTextureOffset(coordinateIndex, slot, materialName, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialTextureOffset(slot, materialName, propertyName, gameObject);
        }

        internal override Vector2? GetMaterialTextureScaleOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureScaleOriginal(coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialTextureScaleOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialTextureScale(int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialTextureScale(coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialTextureScale(slot, materialName, propertyName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialTextureScale(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTextureScale(coordinateIndex, slot, materialName, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialTextureScale(slot, materialName, propertyName, gameObject);
        }

        internal override Color? GetMaterialColorPropertyValueOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialColorPropertyValueOriginal(coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialColorPropertyValueOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialColorProperty(int coordinateIndex, int slot, string materialName, string propertyName, Color value, Color valueOriginal, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialColorProperty(coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialColorProperty(slot, materialName, propertyName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialColorProperty(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialColorProperty(coordinateIndex, slot, materialName, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialColorProperty(slot, materialName, propertyName, gameObject);
        }

        internal override string GetMaterialFloatPropertyValueOriginal(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialFloatPropertyValueOriginal(coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialFloatPropertyValueOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialFloatProperty(int coordinateIndex, int slot, string materialName, string propertyName, float value, float valueOriginal, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).AddMaterialFloatProperty(coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialFloatProperty(slot, materialName, propertyName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialFloatProperty(int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            var chaControl = gameObject.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialFloatProperty(coordinateIndex, slot, materialName, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialFloatProperty(slot, materialName, propertyName, gameObject);
        }
    }
}
