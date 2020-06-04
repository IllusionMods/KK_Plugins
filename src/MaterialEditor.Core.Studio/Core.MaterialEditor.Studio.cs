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
                        PopulateList(ociItem.objectItem, ObjectType.StudioItem, slot: GetObjectID(objectCtrlInfo));
                    else if (objectCtrlInfo is OCIChar ociChar)
                        PopulateList(ociChar.charInfo.gameObject, ObjectType.Character, slot: GetObjectID(objectCtrlInfo));
        }

        public static int GetObjectID(ObjectCtrlInfo oci) => Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == oci).Key;

        public static SceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<SceneController>();

        internal override string GetRendererPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                return MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rendererName, property);
            else
                return GetSceneController().GetRendererPropertyValueOriginal(slot, rendererName, property);
        }
        internal override void AddRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).AddRendererProperty(objectType, coordinateIndex, slot, rendererName, property, value, valueOriginal, gameObject);
            else
                GetSceneController().AddRendererProperty(slot, rendererName, property, value, valueOriginal, gameObject);
        }
        internal override void RemoveRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).RemoveRendererProperty(objectType, coordinateIndex, slot, rendererName, property, gameObject);
            else
                GetSceneController().RemoveRendererProperty(slot, rendererName, property, gameObject);
        }

        internal override string GetMaterialShaderNameOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                return MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).GetMaterialShaderValue(objectType, coordinateIndex, slot, materialName)?.ShaderNameOriginal;
            else
                return GetSceneController().GetMaterialShaderValue(slot, materialName)?.ShaderNameOriginal;
        }
        internal override void AddMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, string value, string valueOriginal, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).AddMaterialShader(objectType, coordinateIndex, slot, materialName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialShaderName(slot, materialName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).RemoveMaterialShaderName(objectType, coordinateIndex, slot, materialName, gameObject);
            else
                GetSceneController().RemoveMaterialShaderName(slot, materialName, gameObject);
        }

        internal override int? GetMaterialShaderRenderQueueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                return MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).GetMaterialShaderValue(objectType, coordinateIndex, slot, materialName)?.RenderQueueOriginal;
            else
                return GetSceneController().GetMaterialShaderValue(slot, materialName)?.RenderQueueOriginal;
        }
        internal override void AddMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, int value, int valueOriginal, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).AddMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialShaderRenderQueue(slot, materialName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).RemoveMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName, gameObject);
            else
                GetSceneController().RemoveMaterialShaderRenderQueue(slot, materialName, gameObject);
        }

        internal override bool GetMaterialTextureValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                return MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).GetMaterialTextureOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialTextureOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, string filePath, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).AddMaterialTextureFromFile(objectType, coordinateIndex, slot, materialName, propertyName, filePath, gameObject, true);
            else
                GetSceneController().AddMaterialTextureFromFile(slot, materialName, propertyName, filePath, gameObject, true);
        }
        internal override void RemoveMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).RemoveMaterialTexture(objectType, coordinateIndex, slot, materialName, propertyName);
            else
                GetSceneController().RemoveMaterialTexture(slot, materialName, propertyName);
        }

        internal override Vector2? GetMaterialTextureOffsetOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                return MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).GetMaterialTextureOffsetOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialTextureOffsetOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).AddMaterialTextureOffset(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialTextureOffset(slot, materialName, propertyName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).RemoveMaterialTextureOffset(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialTextureOffset(slot, materialName, propertyName, gameObject);
        }

        internal override Vector2? GetMaterialTextureScaleOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                return MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).GetMaterialTextureScaleOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialTextureScaleOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).AddMaterialTextureScale(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialTextureScale(slot, materialName, propertyName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).RemoveMaterialTextureScale(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialTextureScale(slot, materialName, propertyName, gameObject);
        }

        internal override Color? GetMaterialColorPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                return MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).GetMaterialColorPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialColorPropertyValueOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Color value, Color valueOriginal, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialColorProperty(slot, materialName, propertyName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).RemoveMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialColorProperty(slot, materialName, propertyName, gameObject);
        }

        internal override string GetMaterialFloatPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                return MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).GetMaterialFloatPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
            else
                return GetSceneController().GetMaterialFloatPropertyValueOriginal(slot, materialName, propertyName);
        }
        internal override void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, float value, float valueOriginal, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).AddMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
            else
                GetSceneController().AddMaterialFloatProperty(slot, materialName, propertyName, value, valueOriginal, gameObject);
        }
        internal override void RemoveMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject)
        {
            if (objectType == ObjectType.Character)
                MaterialEditorPlugin.GetCharaController(gameObject.GetComponent<ChaControl>()).RemoveMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);
            else
                GetSceneController().RemoveMaterialFloatProperty(slot, materialName, propertyName, gameObject);
        }
    }
}
