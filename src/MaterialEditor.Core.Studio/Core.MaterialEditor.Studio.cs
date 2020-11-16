using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using KKAPI;
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
using ChaClothesComponent = AIChara.CmpClothes;
using ChaCustomHairComponent = AIChara.CmpHair;
using ChaAccessoryComponent = AIChara.CmpAccessory;
#endif

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// Plugin responsible for handling the Studio UI and the KKAPI Studio controller
    /// </summary>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(MaterialEditorPlugin.GUID, MaterialEditorPlugin.Version)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MEStudio : UI
    {
        /// <summary>
        /// MaterialEditor Studio plugin GUID
        /// </summary>
        public const string GUID = MaterialEditorPlugin.GUID + ".studio";
        /// <summary>
        /// MaterialEditor Studio plugin PluginName
        /// </summary>
        public const string PluginName = MaterialEditorPlugin.PluginName + " Studio";
        /// <summary>
        /// MaterialEditor Studio plugin Version
        /// </summary>
        public const string Version = MaterialEditorPlugin.Version;
        /// <summary>
        /// Instance of the plugin
        /// </summary>
        public static MEStudio Instance;

        private void Start()
        {
            Instance = this;
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
            materialEditorButton.onClick.AddListener(() => { UpdateUI(); });

            Harmony.CreateAndPatchAll(typeof(StudioHooks));
        }

        /// <summary>
        /// Shows the MaterialEditor UI for the selected item or refreshes the UI if already open
        /// </summary>
        public void UpdateUI()
        {
            if (Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes.Length != 1)
                return;

            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
                if (Studio.Studio.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    if (objectCtrlInfo is OCIItem ociItem)
                    {
                        PopulateList(ociItem.objectItem, slot: GetObjectID(objectCtrlInfo));
                        ItemTypeDropDown.gameObject.SetActive(false);
                    }
                    else if (objectCtrlInfo is OCIChar ociChar)
                    {
                        PopulateList(ociChar.charInfo.gameObject, slot: GetObjectID(objectCtrlInfo));
                        PopulateItemTypeDropdown(ociChar.charInfo);
                        ItemTypeDropDown.gameObject.SetActive(true);
                    }
        }

        /// <summary>
        /// Get the ID for the specified ObjectCtrlInfo
        /// </summary>
        /// <param name="oci"></param>
        /// <returns>ID for the specified ObjectCtrlInfo</returns>
        public static int GetObjectID(ObjectCtrlInfo oci) => Studio.Studio.Instance.dicObjectCtrl.First(x => x.Value == oci).Key;
        /// <summary>
        /// Get the KKAPI scene controller for MaterialEditor. Provides access to methods for getting and setting material properties for studio objects.
        /// </summary>
        /// <returns></returns>
        public static SceneController GetSceneController() => Chainloader.ManagerObject.transform.GetComponentInChildren<SceneController>();

        internal override string GetRendererPropertyValueOriginal(int slot, Renderer renderer, RendererProperties property, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetRendererPropertyValueOriginal(slot, renderer, property, go);
            else
                return GetSceneController().GetRendererPropertyValueOriginal(slot, renderer, property);
        }
        internal override void SetRendererProperty(int slot, Renderer renderer, RendererProperties property, string value, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).SetRendererProperty(slot, renderer, property, value, go);
            else
                GetSceneController().SetRendererProperty(slot, renderer, property, value);
        }
        internal override void RemoveRendererProperty(int slot, Renderer renderer, RendererProperties property, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveRendererProperty(slot, renderer, property, go);
            else
                GetSceneController().RemoveRendererProperty(slot, renderer, property);
        }

        internal override void MaterialCopyEdits(int slot, Material material, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).MaterialCopyEdits(slot, material, go);
            else
                GetSceneController().MaterialCopyEdits(slot, material);
        }
        internal override void MaterialPasteEdits(int slot, Material material, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).MaterialPasteEdits(slot, material, go);
            else
                GetSceneController().MaterialPasteEdits(slot, material);
        }

        internal override string GetMaterialShaderNameOriginal(int slot, Material material, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialShaderOriginal(slot, material, go);
            else
                return GetSceneController().GetMaterialShaderOriginal(slot, material);
        }
        internal override void SetMaterialShaderName(int slot, Material material, string value, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialShader(slot, material, value, go);
            else
                GetSceneController().SetMaterialShader(slot, material, value);
        }
        internal override void RemoveMaterialShaderName(int slot, Material material, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialShader(slot, material, go);
            else
                GetSceneController().RemoveMaterialShader(slot, material);
        }

        internal override int? GetMaterialShaderRenderQueueOriginal(int slot, Material material, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialShaderRenderQueueOriginal(slot, material, go);
            else
                return GetSceneController().GetMaterialShaderRenderQueueOriginal(slot, material);
        }
        internal override void SetMaterialShaderRenderQueue(int slot, Material material, int value, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialShaderRenderQueue(slot, material, value, go);
            else
                GetSceneController().SetMaterialShaderRenderQueue(slot, material, value);
        }
        internal override void RemoveMaterialShaderRenderQueue(int slot, Material material, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialShaderRenderQueue(slot, material, go);
            else
                GetSceneController().RemoveMaterialShaderRenderQueue(slot, material);
        }

        internal override bool GetMaterialTextureValueOriginal(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureOriginal(slot, material, propertyName, go);
            else
                return GetSceneController().GetMaterialTextureOriginal(slot, material, propertyName);
        }
        internal override void SetMaterialTexture(int slot, Material material, string propertyName, string filePath, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialTextureFromFile(slot, material, propertyName, filePath, go, true);
            else
                GetSceneController().SetMaterialTextureFromFile(slot, material, propertyName, filePath, true);
        }
        internal override void RemoveMaterialTexture(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTexture(slot, material, propertyName, go);
            else
                GetSceneController().RemoveMaterialTexture(slot, material, propertyName);
        }

        internal override Vector2? GetMaterialTextureOffsetOriginal(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureOffsetOriginal(slot, material, propertyName, go);
            else
                return GetSceneController().GetMaterialTextureOffsetOriginal(slot, material, propertyName);
        }
        internal override void SetMaterialTextureOffset(int slot, Material material, string propertyName, Vector2 value, GameObject go)
        {
            var chaControl = go.GetComponent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialTextureOffset(slot, material, propertyName, value, go);
            else
                GetSceneController().SetMaterialTextureOffset(slot, material, propertyName, value);
        }
        internal override void RemoveMaterialTextureOffset(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTextureOffset(slot, material, propertyName, go);
            else
                GetSceneController().RemoveMaterialTextureOffset(slot, material, propertyName);
        }

        internal override Vector2? GetMaterialTextureScaleOriginal(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureScaleOriginal(slot, material, propertyName, go);
            else
                return GetSceneController().GetMaterialTextureScaleOriginal(slot, material, propertyName);
        }
        internal override void SetMaterialTextureScale(int slot, Material material, string propertyName, Vector2 value, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialTextureScale(slot, material, propertyName, value, go);
            else
                GetSceneController().SetMaterialTextureScale(slot, material, propertyName, value);
        }
        internal override void RemoveMaterialTextureScale(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTextureScale(slot, material, propertyName, go);
            else
                GetSceneController().RemoveMaterialTextureScale(slot, material, propertyName);
        }

        internal override Color? GetMaterialColorPropertyValueOriginal(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialColorPropertyValueOriginal(slot, material, propertyName, go);
            else
                return GetSceneController().GetMaterialColorPropertyValueOriginal(slot, material, propertyName);
        }
        internal override void SetMaterialColorProperty(int slot, Material material, string propertyName, Color value, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialColorProperty(slot, material, propertyName, value, go);
            else
                GetSceneController().SetMaterialColorProperty(slot, material, propertyName, value);
        }
        internal override void RemoveMaterialColorProperty(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialColorProperty(slot, material, propertyName, go);
            else
                GetSceneController().RemoveMaterialColorProperty(slot, material, propertyName);
        }

        internal override float? GetMaterialFloatPropertyValueOriginal(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialFloatPropertyValueOriginal(slot, material, propertyName, go);
            else
                return GetSceneController().GetMaterialFloatPropertyValueOriginal(slot, material, propertyName);
        }
        internal override void SetMaterialFloatProperty(int slot, Material material, string propertyName, float value, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialFloatProperty(slot, material, propertyName, value, go);
            else
                GetSceneController().SetMaterialFloatProperty(slot, material, propertyName, value);
        }
        internal override void RemoveMaterialFloatProperty(int slot, Material material, string propertyName, GameObject go)
        {
            var chaControl = go.GetComponentInParent<ChaControl>();
            if (chaControl != null)
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialFloatProperty(slot, material, propertyName, go);
            else
                GetSceneController().RemoveMaterialFloatProperty(slot, material, propertyName);
        }
    }
}
