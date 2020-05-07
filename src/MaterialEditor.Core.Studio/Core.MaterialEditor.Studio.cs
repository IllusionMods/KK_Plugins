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
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(GUID);
        }

        private void InitStudioUI(string sceneName)
        {
            if (sceneName != "Studio")
                return;

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

        internal override string GetRendererPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property) =>
            GetSceneController().GetRendererPropertyValueOriginal(slot, rendererName, property);
        internal override void AddRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal, GameObject gameObject) =>
           GetSceneController().AddRendererProperty(slot, rendererName, property, value, valueOriginal, gameObject);
        internal override void RemoveRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject) =>
            GetSceneController().RemoveRendererProperty(slot, rendererName, property, gameObject);

        internal override string GetMaterialShaderNameOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName) =>
            GetSceneController().GetMaterialShaderValue(slot, materialName)?.ShaderNameOriginal;
        internal override void AddMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, string value, string valueOriginal, GameObject gameObject) =>
           GetSceneController().AddMaterialShaderName(slot, materialName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject) =>
            GetSceneController().RemoveMaterialShaderName(slot, materialName, gameObject);

        internal override int? GetMaterialShaderRenderQueueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName) =>
            GetSceneController().GetMaterialShaderValue(slot, materialName)?.RenderQueueOriginal;
        internal override void AddMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, int value, int valueOriginal, GameObject gameObject) =>
           GetSceneController().AddMaterialShaderRenderQueue(slot, materialName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject) =>
            GetSceneController().RemoveMaterialShaderRenderQueue(slot, materialName, gameObject);

        internal override bool GetMaterialTextureValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            GetSceneController().GetMaterialTexturePropertyValue(slot, materialName, propertyName, TexturePropertyType.Texture) == null;
        internal override void AddMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, string filePath, GameObject gameObject) =>
             GetSceneController().AddMaterialTextureFromFile(slot, materialName, propertyName, filePath, gameObject, true);
        internal override void RemoveMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            GetSceneController().RemoveMaterialTextureProperty(slot, materialName, propertyName, TexturePropertyType.Texture);

        internal override Vector2? GetMaterialTextureOffsetOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            GetSceneController().GetMaterialTexturePropertyValue(slot, materialName, propertyName, TexturePropertyType.Offset);
        internal override void AddMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject) =>
           GetSceneController().AddMaterialTextureProperty(slot, materialName, propertyName, TexturePropertyType.Offset, value, valueOriginal, gameObject);
        internal override void RemoveMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            GetSceneController().RemoveMaterialTextureProperty(slot, materialName, propertyName, TexturePropertyType.Offset, gameObject);

        internal override Vector2? GetMaterialTextureScaleOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            GetSceneController().GetMaterialTexturePropertyValue(slot, materialName, propertyName, TexturePropertyType.Scale);
        internal override void AddMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject) =>
           GetSceneController().AddMaterialTextureProperty(slot, materialName, propertyName, TexturePropertyType.Scale, value, valueOriginal, gameObject);
        internal override void RemoveMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            GetSceneController().RemoveMaterialTextureProperty(slot, materialName, propertyName, TexturePropertyType.Scale, gameObject);

        internal override Color? GetMaterialColorPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            GetSceneController().GetMaterialColorPropertyValueOriginal(slot, materialName, propertyName);
        internal override void AddMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Color value, Color valueOriginal, GameObject gameObject) =>
           GetSceneController().AddMaterialColorProperty(slot, materialName, propertyName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            GetSceneController().RemoveMaterialColorProperty(slot, materialName, propertyName, gameObject);

        internal override string GetMaterialFloatPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            GetSceneController().GetMaterialFloatPropertyValueOriginal(slot, materialName, propertyName);
        internal override void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, float value, float valueOriginal, GameObject gameObject) =>
           GetSceneController().AddMaterialFloatProperty(slot, materialName, propertyName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            GetSceneController().RemoveMaterialFloatProperty(slot, materialName, propertyName, gameObject);

    }
}
