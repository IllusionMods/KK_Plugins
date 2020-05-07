using BepInEx.Harmony;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UnityEngine;
using static KK_Plugins.MaterialEditor.MaterialAPI;
#if AI
using AIChara;
#endif

namespace KK_Plugins.MaterialEditor
{
    public partial class MEMaker : UI
    {
        public const string GUID = MaterialEditorPlugin.GUID + ".maker";
        public const string PluginName = MaterialEditorPlugin.PluginName + " Maker";
        public const string Version = MaterialEditorPlugin.Version;

        internal void Start()
        {
            MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            AccessoriesApi.SelectedMakerAccSlotChanged += AccessoriesApi_SelectedMakerAccSlotChanged;
            AccessoriesApi.AccessoryKindChanged += AccessoriesApi_AccessoryKindChanged;
            AccessoriesApi.AccessoryTransferred += AccessoriesApi_AccessoryTransferred;
            AccessoriesApi.AccessoriesCopied += AccessoriesApi_AccessoriesCopied;

            HarmonyWrapper.PatchAll(typeof(MakerHooks));
        }

        private void AccessoriesApi_AccessoryTransferred(object sender, AccessoryTransferEventArgs e) => HideUI();
        private void AccessoriesApi_AccessoryKindChanged(object sender, AccessorySlotEventArgs e) => HideUI();
        private void AccessoriesApi_SelectedMakerAccSlotChanged(object sender, AccessorySlotEventArgs e) => HideUI();
        private void AccessoriesApi_AccessoriesCopied(object sender, AccessoryCopyEventArgs e) => HideUI();

        private void MakerAPI_MakerBaseLoaded(object s, RegisterCustomControlsEvent e)
        {
            InitUI();
#if AI
            var ButtonAllLocation = MakerConstants.Body.All;
#else
            var ButtonAllLocation = MakerConstants.Face.All;
#endif

            MakerAPI.AddAccessoryWindowControl(new MakerButton("Open Material Editor", null, this)).OnClick.AddListener(delegate { PopulateListAccessory(); });
            e.AddControl(new MakerButton("Open Material Editor (Body)", ButtonAllLocation, this)).OnClick.AddListener(delegate { PopulateListBody(); });
            e.AddControl(new MakerButton("Open Material Editor (Face)", ButtonAllLocation, this)).OnClick.AddListener(delegate { PopulateListFace(); });
            e.AddControl(new MakerButton("Open Material Editor (All)", ButtonAllLocation, this)).OnClick.AddListener(delegate { PopulateListCharacter(); });

#if !AI
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Top, this)).OnClick.AddListener(delegate { PopulateListClothes(0); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(delegate { PopulateListClothes(1); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Bra, this)).OnClick.AddListener(delegate { PopulateListClothes(2); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(delegate { PopulateListClothes(3); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(delegate { PopulateListClothes(4); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Panst, this)).OnClick.AddListener(delegate { PopulateListClothes(5); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Socks, this)).OnClick.AddListener(delegate { PopulateListClothes(6); });
#if KK
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(delegate { PopulateListClothes(8); });
#else
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Clothes.Shoes, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
#endif
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Back, this)).OnClick.AddListener(delegate { PopulateListHair(0); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Front, this)).OnClick.AddListener(delegate { PopulateListHair(1); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Side, this)).OnClick.AddListener(delegate { PopulateListHair(2); });
            e.AddControl(new MakerButton("Open Material Editor", MakerConstants.Hair.Extension, this)).OnClick.AddListener(delegate { PopulateListHair(3); });
#endif
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
#if AI
            MakerCategory hairCategory = new MakerCategory(MakerConstants.Hair.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Back)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(0); });
            e.AddControl(new MakerButton("Open Material Editor (Front)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(1); });
            e.AddControl(new MakerButton("Open Material Editor (Side)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(2); });
            e.AddControl(new MakerButton("Open Material Editor (Extension)", hairCategory, this)).OnClick.AddListener(delegate { PopulateListHair(3); });
            e.AddSubCategory(hairCategory);

            MakerCategory clothesCategory = new MakerCategory(MakerConstants.Clothes.CategoryName, "ME", 0, "Material Editor");
            e.AddControl(new MakerButton("Open Material Editor (Top)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(0); });
            e.AddControl(new MakerButton("Open Material Editor (Bottom)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(1); });
            e.AddControl(new MakerButton("Open Material Editor (Bra)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(2); });
            e.AddControl(new MakerButton("Open Material Editor (Underwear)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(3); });
            e.AddControl(new MakerButton("Open Material Editor (Gloves)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(4); });
            e.AddControl(new MakerButton("Open Material Editor (Pantyhose)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(5); });
            e.AddControl(new MakerButton("Open Material Editor (Socks)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(6); });
            e.AddControl(new MakerButton("Open Material Editor (Shoes)", clothesCategory, this)).OnClick.AddListener(delegate { PopulateListClothes(7); });
            e.AddSubCategory(clothesCategory);
#endif
        }

        private void PopulateListClothes(int index)
        {
            var chaControl = MakerAPI.GetCharacterControl();
            int coordinateIndex = MaterialEditorPlugin.GetCharaController(chaControl).CurrentCoordinateIndex;
            PopulateList(chaControl.objClothes[index], ObjectType.Clothing, coordinateIndex: coordinateIndex, slot: index);
        }

        private void PopulateListAccessory()
        {
            var chaControl = MakerAPI.GetCharacterControl();
            int coordinateIndex = MaterialEditorPlugin.GetCharaController(chaControl).CurrentCoordinateIndex;
            var chaAccessoryComponent = AccessoriesApi.GetAccessory(MakerAPI.GetCharacterControl(), AccessoriesApi.SelectedMakerAccSlot);
            PopulateList(chaAccessoryComponent?.gameObject, ObjectType.Accessory, coordinateIndex: coordinateIndex, slot: AccessoriesApi.SelectedMakerAccSlot);
        }

        private void PopulateListHair(int index)
        {
            var chaControl = MakerAPI.GetCharacterControl();
            PopulateList(chaControl.objHair[index], ObjectType.Hair, slot: index);
        }

        internal override string GetRendererPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetRendererPropertyValueOriginal(objectType, coordinateIndex, slot, rendererName, property);
        internal override void AddRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, string value, string valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddRendererProperty(objectType, coordinateIndex, slot, rendererName, property, value, valueOriginal, gameObject);
        internal override void RemoveRendererProperty(ObjectType objectType, int coordinateIndex, int slot, string rendererName, RendererProperties property, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveRendererProperty(objectType, coordinateIndex, slot, rendererName, property, gameObject);

        internal override string GetMaterialShaderNameOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderValue(objectType, coordinateIndex, slot, materialName)?.ShaderNameOriginal;
        internal override void AddMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, string value, string valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialShader(objectType, coordinateIndex, slot, materialName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialShaderName(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShaderName(objectType, coordinateIndex, slot, materialName, gameObject);

        internal override int? GetMaterialShaderRenderQueueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialShaderValue(objectType, coordinateIndex, slot, materialName)?.RenderQueueOriginal;
        internal override void AddMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, int value, int valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialShaderRenderQueue(ObjectType objectType, int coordinateIndex, int slot, string materialName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialShaderRenderQueue(objectType, coordinateIndex, slot, materialName, gameObject);

        internal override bool GetMaterialTextureValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTexturePropertyValue(objectType, coordinateIndex, slot, materialName, propertyName, TexturePropertyType.Texture) == null;
        internal override void AddMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, string filePath, GameObject gameObject) =>
             MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialTextureFromFile(objectType, coordinateIndex, slot, materialName, propertyName, filePath, gameObject, true);
        internal override void RemoveMaterialTexture(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName, TexturePropertyType.Texture);

        internal override Vector2? GetMaterialTextureOffsetOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTexturePropertyValue(objectType, coordinateIndex, slot, materialName, propertyName, TexturePropertyType.Offset);
        internal override void AddMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName, TexturePropertyType.Offset, value, valueOriginal, gameObject);
        internal override void RemoveMaterialTextureOffset(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName, TexturePropertyType.Offset, gameObject);

        internal override Vector2? GetMaterialTextureScaleOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialTexturePropertyValue(objectType, coordinateIndex, slot, materialName, propertyName, TexturePropertyType.Scale);
        internal override void AddMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Vector2 value, Vector2 valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName, TexturePropertyType.Scale, value, valueOriginal, gameObject);
        internal override void RemoveMaterialTextureScale(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialTextureProperty(objectType, coordinateIndex, slot, materialName, propertyName, TexturePropertyType.Scale, gameObject);

        internal override Color? GetMaterialColorPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialColorPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
        internal override void AddMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, Color value, Color valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialColorProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialColorProperty(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);

        internal override string GetMaterialFloatPropertyValueOriginal(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).GetMaterialFloatPropertyValueOriginal(objectType, coordinateIndex, slot, materialName, propertyName);
        internal override void AddMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, float value, float valueOriginal, GameObject gameObject) =>
           MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).AddMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, value, valueOriginal, gameObject);
        internal override void RemoveMaterialFloatProperty(ObjectType objectType, int coordinateIndex, int slot, string materialName, string propertyName, GameObject gameObject) =>
            MaterialEditorPlugin.GetCharaController(MakerAPI.GetCharacterControl()).RemoveMaterialFloatProperty(objectType, coordinateIndex, slot, materialName, propertyName, gameObject);
    }
}
