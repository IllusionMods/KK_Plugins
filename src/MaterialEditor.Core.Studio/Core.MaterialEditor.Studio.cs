using BepInEx;
using BepInEx.Bootstrap;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using MaterialEditorAPI;
using Studio;
using System.IO;
using System.Linq;
using System.Reflection;
using UILib;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static MaterialEditorAPI.MaterialAPI;
#if AI || HS2
using AIChara;
using ChaClothesComponent = AIChara.CmpClothes;
using ChaCustomHairComponent = AIChara.CmpHair;
using ChaAccessoryComponent = AIChara.CmpAccessory;
#endif
#if PH
using ChaControl = Human;
#endif

namespace KK_Plugins.MaterialEditor
{
    /// <summary>
    /// Plugin responsible for handling the Studio UI and the KKAPI Studio controller
    /// </summary>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(MaterialEditorPlugin.PluginGUID, MaterialEditorPlugin.PluginVersion)]
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
#if !PH
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
#endif
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MEStudio : MaterialEditorUI
    {
        /// <summary>
        /// MaterialEditor Studio plugin GUID
        /// </summary>
        public const string GUID = MaterialEditorPlugin.PluginGUID + ".studio";
        /// <summary>
        /// MaterialEditor Studio plugin PluginName
        /// </summary>
        public const string PluginName = MaterialEditorPlugin.PluginName + " Studio";
        /// <summary>
        /// MaterialEditor Studio plugin Version
        /// </summary>
        public const string Version = MaterialEditorPlugin.PluginVersion;
        /// <summary>
        /// Instance of the plugin
        /// </summary>
        public static MEStudio Instance;

        internal static Dropdown ItemTypeDropDown;

        private void Start()
        {
            Instance = this;
            SceneManager.sceneLoaded += (s, lsm) => InitStudioUI(s.name);
            StudioSaveLoadApi.RegisterExtraBehaviour<SceneController>(MaterialEditorPlugin.PluginGUID);
        }

        private void InitStudioUI(string sceneName)
        {
            if (sceneName != "Studio") return;
            SceneManager.sceneLoaded -= (s, lsm) => InitStudioUI(s.name);

            InitUI();

            ItemTypeDropDown = UIUtility.CreateDropdown("ItemType", DragPanel.transform);
            ItemTypeDropDown.transform.SetRect(1f, 0f, 1f, 1f, -200f, 1f, -19f, -1f);
            ItemTypeDropDown.captionText.transform.SetRect(0.05f, 0f, 1f, 1f, 5f, 2f, -15f, -2f);
            ItemTypeDropDown.captionText.alignment = TextAnchor.MiddleLeft;
            ItemTypeDropDown.gameObject.SetActive(false);

#if PH
            RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar/Button Folder").GetComponent<RectTransform>();
#else
            RectTransform original = GameObject.Find("StudioScene").transform.Find("Canvas Object List/Image Bar/Button Route").GetComponent<RectTransform>();
#endif
            Button materialEditorButton = Instantiate(original.gameObject).GetComponent<Button>();
            RectTransform materialEditorButtonRectTransform = materialEditorButton.transform as RectTransform;
            materialEditorButton.transform.SetParent(original.parent, true);
            materialEditorButton.transform.localScale = original.localScale;
            materialEditorButtonRectTransform.SetRect(original.anchorMin, original.anchorMax, original.offsetMin, original.offsetMax);
#if PH
            materialEditorButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(-40f, 0f);
#else
            materialEditorButtonRectTransform.anchoredPosition = original.anchoredPosition + new Vector2(-48f, 0f);
#endif

            Texture2D texture2D = new Texture2D(32, 32);
            texture2D.LoadImage(LoadIcon());
            var MatEditorIcon = materialEditorButton.targetGraphic as Image;
            MatEditorIcon.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 32, 32), new Vector2(16, 16));
            MatEditorIcon.color = Color.white;

            materialEditorButton.onClick = new Button.ButtonClickedEvent();
            materialEditorButton.onClick.AddListener(() => { UpdateUI(); });
        }

        internal byte[] LoadIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(KK_Plugins)}.Resources.MaterialEditorIcon.png"))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                return bytesInStream;
            }
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
                        PopulateList(ociItem.objectItem, GetObjectID(objectCtrlInfo));
                        ItemTypeDropDown.gameObject.SetActive(false);
                    }
                    else if (objectCtrlInfo is OCIChar ociChar)
                    {
                        PopulateList(ociChar.charInfo.gameObject, new ObjectData(0, MaterialEditorCharaController.ObjectType.Character));
                        var chaControl = ociChar.GetChaControl();
                        PopulateItemTypeDropdown(chaControl);
                        ItemTypeDropDown.gameObject.SetActive(true);
                    }
        }

        /// <summary>
        /// Populate the ItemType dropdown for switching between displaying various types of items on a character
        /// </summary>
        protected void PopulateItemTypeDropdown(ChaControl chaControl)
        {
            ItemTypeDropDown.onValueChanged.RemoveAllListeners();
            ItemTypeDropDown.onValueChanged.AddListener(value => ChangeItemType(value, chaControl));
            ItemTypeDropDown.options.Clear();
            ItemTypeDropDown.options.Add(new Dropdown.OptionData("Body"));
            ItemTypeDropDown.Set(0);
            ItemTypeDropDown.captionText.text = "Body";

            var clothes = chaControl.GetClothes();
            for (var i = 0; i < clothes.Length; i++)
#if PH
                if (clothes[i] != null)
#else
                if (clothes[i] != null && clothes[i].GetComponentInChildren<ChaClothesComponent>() != null)
#endif
                    ItemTypeDropDown.options.Add(new Dropdown.OptionData($"Clothes {ClothesIndexToString(i)}"));

            var hair = chaControl.GetHair();
            for (var i = 0; i < hair.Length; i++)
#if PH
                if (hair[i] != null)
#else
                if (hair[i] != null && chaControl.objHair[i].GetComponent<ChaCustomHairComponent>() != null)
#endif
                    ItemTypeDropDown.options.Add(new Dropdown.OptionData($"Hair {HairIndexToString(i)}"));

            var accessories = chaControl.GetAccessoryObjects();
            for (var i = 0; i < accessories.Length; i++)
                if (accessories[i] != null)
                    ItemTypeDropDown.options.Add(new Dropdown.OptionData($"Accessory {AccessoryIndexToString(i)}"));
        }

        private void ChangeItemType(int selectedItem, ChaControl chaControl)
        {
            var option = ItemTypeDropDown.OptionText(selectedItem).Split(' ');
            int index = 0;

            if (chaControl == null)
                PopulateList(null, null);

            switch (option[0])
            {
                case "Body":
                    PopulateList(chaControl.gameObject, new ObjectData(0, MaterialEditorCharaController.ObjectType.Character));
                    break;
                case "Clothes":
                    if (option.Length > 1)
                        index = ClothesStringToIndex(option[1]);

                    var clothes = chaControl.GetClothes(index);
#if PH
                    if (index == -1 || clothes == null)
#else
                    if (index == -1 || clothes == null || chaControl.objClothes[index].GetComponentInChildren<ChaClothesComponent>() == null)
#endif
                        PopulateList(chaControl.gameObject, 0);
                    else
                        PopulateList(clothes, new ObjectData(index, MaterialEditorCharaController.ObjectType.Clothing));
                    break;
                case "Hair":
                    if (option.Length > 1)
                        index = HairStringToIndex(option[1]);

                    var hair = chaControl.GetHair(index);
#if PH
                    if (index == -1 || hair == null)
#else
                    if (index == -1 || hair == null || hair.GetComponent<ChaCustomHairComponent>() == null)
#endif
                        PopulateList(chaControl.gameObject, 0);
                    else
                        PopulateList(hair, new ObjectData(index, MaterialEditorCharaController.ObjectType.Hair));
                    break;
                case "Accessory":
                    if (option.Length > 1)
                        index = AccessoryStringToIndex(option[1]);
                    var accessory = chaControl.GetAccessoryObject(index);
                    if (accessory == null)
                        PopulateList(chaControl.gameObject, 0);
                    else
                        PopulateList(accessory, new ObjectData(index, MaterialEditorCharaController.ObjectType.Accessory));
                    break;
            }
        }

        private string ClothesIndexToString(int index)
        {
            switch (index)
            {
                case 0:
                    return "Top";
                case 1:
                    return "Bottom";
                case 2:
                    return "Bra";
                case 3:
                    return "Underwear";
#if PH
                case 4:
                    return "Swimwear";
                case 5:
                    return "SwimTop";
                case 6:
                    return "SwimBot";
                case 7:
                    return "Gloves";
                case 8:
                    return "Pantyhose";
                case 9:
                    return "Legwear";
                case 10:
                    return "Shoes";
#else
                case 4:
                    return "Gloves";
                case 5:
                    return "Pantyhose";
                case 6:
                    return "Legwear";
#if !KK
                case 7:
                    return "Shoes";
#else
                case 7:
                    return "Indoor Shoes";
                case 8:
                    return "Outdoor Shoes";
#endif
#endif
                default:
                    return "";
            }
        }
        private int ClothesStringToIndex(string s)
        {
            switch (s)
            {
                case "Top":
                    return 0;
                case "Bottom":
                    return 1;
                case "Bra":
                    return 2;
                case "Underwear":
                    return 3;
#if PH
                case "Swimwear":
                    return 4;
                case "SwimTop":
                    return 5;
                case "SwimBot":
                    return 6;
                case "Gloves":
                    return 7;
                case "Pantyhose":
                    return 8;
                case "Legwear":
                    return 9;
                case "Shoes":
                    return 10;
#else
                case "Gloves":
                    return 4;
                case "Pantyhose":
                    return 5;
                case "Legwear":
                    return 6;
                case "Indoor Shoes":
                case "Indoor":
                case "Shoes":
                    return 7;
                case "Outdoor Shoes":
                case "Outdoor":
                    return 8;
#endif
                default:
                    return -1;
            }
        }

        private string HairIndexToString(int index)
        {
            switch (index)
            {
                case 0:
                    return "Back";
                case 1:
                    return "Front";
                case 2:
                    return "Side";
                case 3:
                    return "Extension";
                default:
                    return "";
            }
        }
        private int HairStringToIndex(string s)
        {
            switch (s)
            {
                case "Back":
                    return 0;
                case "Front":
                    return 1;
                case "Side":
                    return 2;
                case "Extension":
                    return 3;
                default:
                    return -1;
            }
        }
        private string AccessoryIndexToString(int index) => $"{index + 1:00}";
        private int AccessoryStringToIndex(string s)
        {
            if (int.TryParse(s, out int index))
                return index - 1;
            return -1;
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

        public override string GetRendererPropertyValueOriginal(object data, Renderer renderer, RendererProperties property, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                return MaterialEditorPlugin.GetCharaController(chaControl).GetRendererPropertyValueOriginal(objectData.Slot, objectData.ObjectType, renderer, property, go);
            }
            else
                return GetSceneController().GetRendererPropertyValueOriginal((int)data, renderer, property);
        }
        public override void SetRendererProperty(object data, Renderer renderer, RendererProperties property, string value, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).SetRendererProperty(objectData.Slot, objectData.ObjectType, renderer, property, value, go);
            }
            else
                GetSceneController().SetRendererProperty((int)data, renderer, property, value);
        }
        public override void RemoveRendererProperty(object data, Renderer renderer, RendererProperties property, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveRendererProperty(objectData.Slot, objectData.ObjectType, renderer, property, go);
            }
            else
                GetSceneController().RemoveRendererProperty((int)data, renderer, property);
        }

        public override void MaterialCopyEdits(object data, Material material, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).MaterialCopyEdits(objectData.Slot, objectData.ObjectType, material, go);
            }
            else
                GetSceneController().MaterialCopyEdits((int)data, material);
        }
        public override void MaterialPasteEdits(object data, Material material, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).MaterialPasteEdits(objectData.Slot, objectData.ObjectType, material, go);
            }
            else
                GetSceneController().MaterialPasteEdits((int)data, material);
        }

        public override string GetMaterialShaderNameOriginal(object data, Material material, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialShaderOriginal(objectData.Slot, objectData.ObjectType, material, go);
            }
            else
                return GetSceneController().GetMaterialShaderOriginal((int)data, material);
        }
        public override void SetMaterialShaderName(object data, Material material, string value, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialShader(objectData.Slot, objectData.ObjectType, material, value, go);
            }
            else
                GetSceneController().SetMaterialShader((int)data, material, value);
        }
        public override void RemoveMaterialShaderName(object data, Material material, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialShader(objectData.Slot, objectData.ObjectType, material, go);
            }
            else
                GetSceneController().RemoveMaterialShader((int)data, material);
        }

        public override int? GetMaterialShaderRenderQueueOriginal(object data, Material material, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialShaderRenderQueueOriginal(objectData.Slot, objectData.ObjectType, material, go);
            }
            else
                return GetSceneController().GetMaterialShaderRenderQueueOriginal((int)data, material);
        }
        public override void SetMaterialShaderRenderQueue(object data, Material material, int value, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialShaderRenderQueue(objectData.Slot, objectData.ObjectType, material, value, go);
            }
            else
                GetSceneController().SetMaterialShaderRenderQueue((int)data, material, value);
        }
        public override void RemoveMaterialShaderRenderQueue(object data, Material material, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialShaderRenderQueue(objectData.Slot, objectData.ObjectType, material, go);
            }
            else
                GetSceneController().RemoveMaterialShaderRenderQueue((int)data, material);
        }

        public override bool GetMaterialTextureValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                return GetSceneController().GetMaterialTextureOriginal((int)data, material, propertyName);
        }
        public override void SetMaterialTexture(object data, Material material, string propertyName, string filePath, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialTextureFromFile(objectData.Slot, objectData.ObjectType, material, propertyName, filePath, go, true);
            }
            else
                GetSceneController().SetMaterialTextureFromFile((int)data, material, propertyName, filePath, true);
        }
        public override void RemoveMaterialTexture(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTexture(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                GetSceneController().RemoveMaterialTexture((int)data, material, propertyName);
        }

        public override Vector2? GetMaterialTextureOffsetOriginal(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureOffsetOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                return GetSceneController().GetMaterialTextureOffsetOriginal((int)data, material, propertyName);
        }
        public override void SetMaterialTextureOffset(object data, Material material, string propertyName, Vector2 value, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialTextureOffset(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
            }
            else
                GetSceneController().SetMaterialTextureOffset((int)data, material, propertyName, value);
        }
        public override void RemoveMaterialTextureOffset(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTextureOffset(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                GetSceneController().RemoveMaterialTextureOffset((int)data, material, propertyName);
        }

        public override Vector2? GetMaterialTextureScaleOriginal(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialTextureScaleOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                return GetSceneController().GetMaterialTextureScaleOriginal((int)data, material, propertyName);
        }
        public override void SetMaterialTextureScale(object data, Material material, string propertyName, Vector2 value, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialTextureScale(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
            }
            else
                GetSceneController().SetMaterialTextureScale((int)data, material, propertyName, value);
        }
        public override void RemoveMaterialTextureScale(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialTextureScale(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                GetSceneController().RemoveMaterialTextureScale((int)data, material, propertyName);
        }

        public override Color? GetMaterialColorPropertyValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialColorPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                return GetSceneController().GetMaterialColorPropertyValueOriginal((int)data, material, propertyName);
        }
        public override void SetMaterialColorProperty(object data, Material material, string propertyName, Color value, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialColorProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
            }
            else
                GetSceneController().SetMaterialColorProperty((int)data, material, propertyName, value);
        }
        public override void RemoveMaterialColorProperty(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialColorProperty(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                GetSceneController().RemoveMaterialColorProperty((int)data, material, propertyName);
        }

        public override float? GetMaterialFloatPropertyValueOriginal(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                return MaterialEditorPlugin.GetCharaController(chaControl).GetMaterialFloatPropertyValueOriginal(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                return GetSceneController().GetMaterialFloatPropertyValueOriginal((int)data, material, propertyName);
        }
        public override void SetMaterialFloatProperty(object data, Material material, string propertyName, float value, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).SetMaterialFloatProperty(objectData.Slot, objectData.ObjectType, material, propertyName, value, go);
            }
            else
                GetSceneController().SetMaterialFloatProperty((int)data, material, propertyName, value);
        }
        public override void RemoveMaterialFloatProperty(object data, Material material, string propertyName, GameObject go)
        {
            if (data is ObjectData objectData)
            {
                var chaControl = go.GetComponentInParent<ChaControl>();
                MaterialEditorPlugin.GetCharaController(chaControl).RemoveMaterialFloatProperty(objectData.Slot, objectData.ObjectType, material, propertyName, go);
            }
            else
                GetSceneController().RemoveMaterialFloatProperty((int)data, material, propertyName);
        }
    }
}
