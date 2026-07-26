using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MaterialEditorAPI;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;
#if !EC
using KKAPI.Studio;
#endif
#if AI || HS2
using AIChara;
#endif
#if PH
using ChaFileCoordinate = Character.CustomParameter;
using ChaControl = Human;
#endif

namespace KK_Plugins.MaterialEditor
{
    using MEAnimationController = MEAnimationController<MaterialEditorCharaController, MaterialEditorCharaController.MaterialTextureProperty>;

    /// <summary>
    /// KKAPI character controller that handles saving and loading character data as well as provides methods to get or set the saved data
    /// </summary>
    public partial class MaterialEditorCharaController : CharaCustomFunctionController
    {
        /// <summary>
        /// Under what name the controller's textures get saved in cards
        /// </summary>
        public const string TexDicSaveKey = nameof(TextureDictionary);

        internal static readonly List<MaterialEditorCharaController> charaControllers = new List<MaterialEditorCharaController>();

        private readonly List<RendererProperty> RendererPropertyList = new List<RendererProperty>();
        private readonly List<ProjectorProperty> ProjectorPropertyList = new List<ProjectorProperty>();
        private readonly List<MaterialNameProperty> MaterialNamePropertyList = new List<MaterialNameProperty>();
        private readonly List<MaterialFloatProperty> MaterialFloatPropertyList = new List<MaterialFloatProperty>();
        private readonly List<MaterialColorProperty> MaterialColorPropertyList = new List<MaterialColorProperty>();
        private readonly List<MaterialKeywordProperty> MaterialKeywordPropertyList = new List<MaterialKeywordProperty>();
        internal readonly List<MaterialTextureProperty> MaterialTexturePropertyList = new List<MaterialTextureProperty>();
        private readonly List<MaterialShader> MaterialShaderList = new List<MaterialShader>();
        private readonly List<MaterialCopy> MaterialCopyList = new List<MaterialCopy>();

        internal readonly Dictionary<int, TextureContainer> TextureDictionary = new Dictionary<int, TextureContainer>();

        private readonly Dictionary<MaterialTextureProperty, MEAnimationController> AnimationControllerMap = new Dictionary<MaterialTextureProperty, MEAnimationController>();

        static MaterialEditorCharaController()
        {
            InitAnimationController();
        }

        /// <summary>
        /// Index of the currently worn coordinate. Always 0 except for in Koikatsu
        /// </summary>
#if KK || KKS
        public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
#else
        public int CurrentCoordinateIndex => 0;
#endif
        private string FileToSet;
        private string PropertyToSet;
        private Material MatToSet;
        private int SlotToSet;
        private ObjectType ObjectTypeToSet;
        private GameObject GameObjectToSet;
        internal int? DuplicatingFrom = null;

        /// <summary></summary>
        protected override void Awake()
        {
            charaControllers.Add(this);
            base.Awake();
        }

        /// <summary></summary>
        protected override void OnDestroy()
        {
            charaControllers.Remove(this);
            base.OnDestroy();
        }

        /// <summary>
        /// Handles saving data to character cards
        /// </summary>
        /// <param name="currentGameMode"></param>
        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
#if KK || KKS
            //Always run on save to also purge them for cards made before this purging was implemented
            PurgeUnusedCoordinates();
#endif
            PurgeUnusedTextures();

            if (RendererPropertyList.Count == 0 && MaterialFloatPropertyList.Count == 0 && MaterialKeywordPropertyList.Count == 0 && MaterialColorPropertyList.Count == 0 && MaterialTexturePropertyList.Count == 0 && MaterialShaderList.Count == 0 && MaterialCopyList.Count == 0)
            {
                SetExtendedData(null);
            }
            else
            {
                var data = new PluginData();

                if (TextureDictionary.Count > 0)
                    TextureSaveHandler.Instance.Save(data, TexDicSaveKey, TextureDictionary, true);
                else
                    data.data.Add(TexDicSaveKey, null);

                if (RendererPropertyList.Count > 0)
                    data.data.Add(nameof(RendererPropertyList), MessagePackSerializer.Serialize(RendererPropertyList));
                else
                    data.data.Add(nameof(RendererPropertyList), null);

                if (ProjectorPropertyList.Count > 0)
                    data.data.Add(nameof(ProjectorPropertyList), MessagePackSerializer.Serialize(ProjectorPropertyList));
                else
                    data.data.Add(nameof(ProjectorPropertyList), null);

                if (MaterialNamePropertyList.Count > 0)
                    data.data.Add(nameof(MaterialNamePropertyList), MessagePackSerializer.Serialize(MaterialNamePropertyList));
                else
                    data.data.Add(nameof(MaterialNamePropertyList), null);

                if (MaterialFloatPropertyList.Count > 0)
                    data.data.Add(nameof(MaterialFloatPropertyList), MessagePackSerializer.Serialize(MaterialFloatPropertyList));
                else
                    data.data.Add(nameof(MaterialFloatPropertyList), null);

                if (MaterialKeywordPropertyList.Count > 0)
                    data.data.Add(nameof(MaterialKeywordPropertyList), MessagePackSerializer.Serialize(MaterialKeywordPropertyList));
                else
                    data.data.Add(nameof(MaterialKeywordPropertyList), null);

                if (MaterialColorPropertyList.Count > 0)
                    data.data.Add(nameof(MaterialColorPropertyList), MessagePackSerializer.Serialize(MaterialColorPropertyList));
                else
                    data.data.Add(nameof(MaterialColorPropertyList), null);

                if (MaterialTexturePropertyList.Count > 0)
                    data.data.Add(nameof(MaterialTexturePropertyList), MessagePackSerializer.Serialize(MaterialTexturePropertyList));
                else
                    data.data.Add(nameof(MaterialTexturePropertyList), null);

                if (MaterialShaderList.Count > 0)
                    data.data.Add(nameof(MaterialShaderList), MessagePackSerializer.Serialize(MaterialShaderList));
                else
                    data.data.Add(nameof(MaterialShaderList), null);

                if (MaterialCopyList.Count > 0)
                    data.data.Add(nameof(MaterialCopyList), MessagePackSerializer.Serialize(MaterialCopyList));
                else
                    data.data.Add(nameof(MaterialCopyList), null);

                SetExtendedData(data);
            }
        }

        /// <summary>
        /// Handles loading data from character cards
        /// </summary>
        /// <param name="currentGameMode"></param>
        /// <param name="maintainState"></param>
        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (!maintainState)
            {
                RemoveMaterialCopies(ChaControl.gameObject);

                CharacterLoading = true;
                LoadCharacterExtSaveData();
            }

            ChaControl.StartCoroutine(LoadData(true, true, true));
        }

        internal new void Update()
        {
            SetMaterialTextureFromFileByUpdate();
            MEAnimationController.UpdateAnimations(AnimationControllerMap);
            base.Update();
            if (MaterialEditorPlugin.PurgeOrphanedPropertiesHotkey.Value.IsDown())
                PurgeOrphanedProperties();
            if (MakerAPI.InsideMaker)
            {
                if (MaterialEditorPlugin.DisableShadowCastingHotkey.Value.IsDown())
                {
                    SetRendererPropertyRecursive(RendererProperties.ShadowCastingMode, "0");
                    MaterialEditorPlugin.Logger.LogMessage($"Disabled ShadowCasting");
                }
                else if (MaterialEditorPlugin.EnableShadowCastingHotkey.Value.IsDown())
                {
                    SetRendererPropertyRecursive(RendererProperties.ShadowCastingMode, "1");
                    MaterialEditorPlugin.Logger.LogMessage($"Enabled ShadowCasting");
                }
                else if (MaterialEditorPlugin.TwoSidedShadowCastingHotkey.Value.IsDown())
                {
                    SetRendererPropertyRecursive(RendererProperties.ShadowCastingMode, "2");
                    MaterialEditorPlugin.Logger.LogMessage($"Two Sided ShadowCasting");
                }
                else if (MaterialEditorPlugin.ShadowsOnlyShadowCastingHotkey.Value.IsDown())
                {
                    SetRendererPropertyRecursive(RendererProperties.ShadowCastingMode, "3");
                    MaterialEditorPlugin.Logger.LogMessage($"Shadows Only ShadowCasting");
                }
                else if (MaterialEditorPlugin.ResetShadowCastingHotkey.Value.IsDown())
                {
                    SetRendererPropertyRecursive(RendererProperties.ShadowCastingMode, "-1");
                    MaterialEditorPlugin.Logger.LogMessage($"Reset ShadowCasting ShadowCasting");
                }
                else if (MaterialEditorPlugin.DisableReceiveShadows.Value.IsDown())
                {
                    SetRendererPropertyRecursive(RendererProperties.ReceiveShadows, "0");
                    MaterialEditorPlugin.Logger.LogMessage($"Disabled ReceiveShadows");
                }
                else if (MaterialEditorPlugin.EnableReceiveShadows.Value.IsDown())
                {
                    SetRendererPropertyRecursive(RendererProperties.ReceiveShadows, "1");
                    MaterialEditorPlugin.Logger.LogMessage($"Enabled ReceiveShadows");
                }
                else if (MaterialEditorPlugin.ResetReceiveShadows.Value.IsDown())
                {
                    SetRendererPropertyRecursive(RendererProperties.ReceiveShadows, "-1");
                    MaterialEditorPlugin.Logger.LogMessage($"Reset ReceiveShadows");
                }
            }
        }

        internal void SetRendererPropertyRecursive(RendererProperties property, string value, bool affectBody = false)
        {
            if (affectBody)
                foreach (var rend in GetRendererList(ChaControl.gameObject))
                {
                    //Disable the shadowcaster renderer instead of changing the shadowcasting mode
                    if (property == RendererProperties.ShadowCastingMode && (rend.name == "o_shadowcaster" || rend.name == "o_shadowcaster_cm"))
                    {
                        if (value == "-1")
                            RemoveRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, RendererProperties.Enabled, ChaControl.gameObject);
                        //keep consistency in the casted shadow with how it would normally look
                        else if (value == "2" | value == "3")
                            {
                                RemoveRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, RendererProperties.Enabled, ChaControl.gameObject);
                                SetRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, property, value, ChaControl.gameObject);
                            }
                            else
                                SetRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, RendererProperties.Enabled, value, ChaControl.gameObject);
                    }
                    else
                    {
                        if (value == "-1")
                            RemoveRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, property, ChaControl.gameObject);
                        else
                            SetRendererProperty(0, MaterialEditorCharaController.ObjectType.Character, rend, property, value, ChaControl.gameObject);
                    }
                }
            var clothes = ChaControl.GetClothes();
            for (var i = 0; i < clothes.Length; i++)
            {
                var gameObj = clothes[i];
                foreach (var renderer in GetRendererList(gameObj))
                    if (value == "-1")
                        RemoveRendererProperty(i, MaterialEditorCharaController.ObjectType.Clothing, renderer, property, gameObj);
                    else
                        SetRendererProperty(i, MaterialEditorCharaController.ObjectType.Clothing, renderer, property, value, gameObj);
            }
            var hair = ChaControl.GetHair();
            for (var i = 0; i < hair.Length; i++)
            {
                var gameObj = hair[i];
                foreach (var renderer in GetRendererList(gameObj))
                    if (value == "-1")
                        RemoveRendererProperty(i, MaterialEditorCharaController.ObjectType.Hair, renderer, property, gameObj);
                    else
                        SetRendererProperty(i, MaterialEditorCharaController.ObjectType.Hair, renderer, property, value, gameObj);
            }
            var accessories = ChaControl.GetAccessoryObjects();
            for (var i = 0; i < accessories.Length; i++)
            {
                var gameObj = accessories[i];
                if (gameObj != null)
                    foreach (var renderer in GetRendererList(gameObj))
                        if (value == "-1")
                            RemoveRendererProperty(i, MaterialEditorCharaController.ObjectType.Accessory, renderer, property, gameObj);
                        else
                            SetRendererProperty(i, MaterialEditorCharaController.ObjectType.Accessory, renderer, property, value, gameObj);
            }
        }

        /// <summary>
        /// Used by SetMaterialTextureFromFile if setTexInUpdate is true, needed for loading files via file dialogue
        /// </summary>
        private void SetMaterialTextureFromFileByUpdate()
        {
            try
            {
                if (FileToSet != null)
                    SetMaterialTextureFromFile(SlotToSet, ObjectTypeToSet, MatToSet, PropertyToSet, FileToSet, GameObjectToSet);
            }
            catch
            {
                //MaterialEditorPlugin.Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, "Failed to load texture.");
            }
            finally
            {
                FileToSet = null;
                PropertyToSet = null;
                MatToSet = null;
                GameObjectToSet = null;
            }
        }

        /// <summary>
        /// Get the coordinate index based on object type, hair and character return 0, clothes and accessories return CurrentCoordinateIndex
        /// </summary>
        internal int GetCoordinateIndex(ObjectType objectType)
        {
#if KK || KKS
            if (objectType == ObjectType.Accessory || objectType == ObjectType.Clothing)
                return CurrentCoordinateIndex;
#endif
            return 0;
        }

        private bool coordinateChanging;
        /// <summary>
        /// Whether the coordinate is being changed this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool CoordinateChanging
        {
            get => coordinateChanging;
            set
            {
                coordinateChanging = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    coordinateChanging = false;
                }
            }
        }

        private bool accessorySelectedSlotChanging;
        /// <summary>
        /// Whether the selected accessory slot is being changed this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool AccessorySelectedSlotChanging
        {
            get => accessorySelectedSlotChanging;
            set
            {
                accessorySelectedSlotChanging = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    accessorySelectedSlotChanging = false;
                }
            }
        }

        private bool clothesChanging;
        /// <summary>
        /// Whether the clothes are being changed this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool ClothesChanging
        {
            get => clothesChanging;
            set
            {
                clothesChanging = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    clothesChanging = false;
                }
            }
        }

        private bool characterLoading;
        /// <summary>
        /// Whether the character is being changed this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool CharacterLoading
        {
            get => characterLoading;
            set
            {
                characterLoading = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    characterLoading = false;
                }
            }
        }

        private bool refreshingTextures;
        /// <summary>
        /// Whether the overlay plugin is refreshing textures this Update. Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool RefreshingTextures
        {
            get => refreshingTextures;
            set
            {
                refreshingTextures = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    refreshingTextures = false;
                }
            }
        }

        private bool customClothesOverride;
        /// <summary>
        /// Override flag set to distinguish between clothes being changed via character maker and clothes changed by changing outfit slots, loading the character, or other methods.
        /// Used by methods that happen later in the update. If set, reverts to false on next Update.
        /// </summary>
        public bool CustomClothesOverride
        {
            get => customClothesOverride;
            set
            {
                customClothesOverride = value;
                ChaControl.StartCoroutine(Reset());
                IEnumerator Reset()
                {
                    yield return null;
                    customClothesOverride = false;
                }
            }
        }

        private GameObject FindGameObject(ObjectType objectType, int slot)
        {
            if (objectType == ObjectType.Clothing)
                return ChaControl.GetClothes(slot);
            if (objectType == ObjectType.Accessory)
            {
                var acc = ChaControl.GetAccessoryObject(slot);
                if (acc != null)
                    return acc;
            }
            if (objectType == ObjectType.Hair)
            {
                var hair = ChaControl.GetHair(slot);
                if (hair != null)
                    return hair;
            }
            if (objectType == ObjectType.Character)
                return ChaControl.gameObject;
            return null;
        }

        /// <summary>
        /// Purge unused textures from TextureDictionary
        /// </summary>
        protected int PurgeUnusedTextures()
        {
            if (TextureDictionary.Count <= 0)
                return 0;

            HashSet<int> unuseds = new HashSet<int>(TextureDictionary.Keys);

            //Remove textures in use
            for (int i = 0; i < MaterialTexturePropertyList.Count; ++i)
            {
                var prop = MaterialTexturePropertyList[i];
                var texID = prop.TexID;
                if (texID.HasValue)
                    unuseds.Remove(texID.Value);

                if (prop.TexAnimationDef != null)
                {
                    var frames = prop.TexAnimationDef.frames;
                    for (int j = 0; j < frames.Length; ++j)
                        unuseds.Remove(frames[j].texID);
                }
            }

            foreach (var texID in unuseds)
            {
                TextureDictionary[texID].Dispose();
                TextureDictionary.Remove(texID);
            }
            return unuseds.Count;
        }

#if KK || KKS

        /// <summary>
        /// Purge coordinate properties that reference a coordinate that no longer exists
        /// </summary>
        internal void PurgeUnusedCoordinates()
        {
            RendererPropertyList.RemoveAll(x => ChaControl.chaFile.coordinate.ElementAtOrDefault(x.CoordinateIndex) == null);
            ProjectorPropertyList.RemoveAll(x => ChaControl.chaFile.coordinate.ElementAtOrDefault(x.CoordinateIndex) == null);
            MaterialNamePropertyList.RemoveAll(x => ChaControl.chaFile.coordinate.ElementAtOrDefault(x.CoordinateIndex) == null);
            MaterialFloatPropertyList.RemoveAll(x => ChaControl.chaFile.coordinate.ElementAtOrDefault(x.CoordinateIndex) == null);
            MaterialColorPropertyList.RemoveAll(x => ChaControl.chaFile.coordinate.ElementAtOrDefault(x.CoordinateIndex) == null);
            MaterialKeywordPropertyList.RemoveAll(x => ChaControl.chaFile.coordinate.ElementAtOrDefault(x.CoordinateIndex) == null);
            MaterialTexturePropertyList.RemoveAll(x => ChaControl.chaFile.coordinate.ElementAtOrDefault(x.CoordinateIndex) == null);
            MaterialShaderList.RemoveAll(x => ChaControl.chaFile.coordinate.ElementAtOrDefault(x.CoordinateIndex) == null);
            MaterialCopyList.RemoveAll(x => ChaControl.chaFile.coordinate.ElementAtOrDefault(x.CoordinateIndex) == null);
        }
#endif

        internal void PurgeOrphanedProperties()
        {
            int removedCount = 0;

            for (var i = 0; i < ChaControl.GetClothes().Length; i++)
                removeProperties(ObjectType.Clothing, i, ChaControl.GetClothes()[i]);
            for (var i = 0; i < ChaControl.GetAccessoryObjects().Length; i++)
                removeProperties(ObjectType.Accessory, i, ChaControl.GetAccessoryObjects()[i]);
            for (var i = 0; i < ChaControl.GetHair().Length; i++)
                removeProperties(ObjectType.Hair, i, ChaControl.GetHair()[i]);
            //The same is not done for the body because some properties are not exposed, while technically still there and used
            //An example would be the face alpha mask not being exposed in koikatsu's v+ shaders, while still being applied if set in a shader that does expose it

            void removeProperties(ObjectType objectType, int slot, GameObject go)
            {
                if (go == null) return;
                var renderers = GetRendererList(go);
                if (renderers == null) return;

                var materialNames = renderers.SelectMany(x => x.materials).Select(x => x.NameFormatted()).ToList();
                var projectors = GetProjectorList(objectType, go);
                materialNames.AddRange(projectors.Select(x => x.material.NameFormatted()));

                var materialPropertiesDict = renderers
                    .SelectMany(x => x.materials)
                    .GroupBy(x => x.NameFormatted())
                    .Select(x => x.First())
                    .ToDictionary(
                        x => x.NameFormatted(),
                        x => XMLShaderProperties[XMLShaderProperties.ContainsKey(x.shader.NameFormatted()) ? x.shader.NameFormatted() : "default"].Select(i => i.Key)
                );

                removedCount += ProjectorPropertyList.RemoveAll(
                    x => x.CoordinateIndex == CurrentCoordinateIndex
                    && x.Slot == slot
                    && x.ObjectType == objectType
                    && !projectors.Select(projector => projector.NameFormatted()).Contains(x.ProjectorName)
                );
                removedCount += RendererPropertyList.RemoveAll(
                    x => x.CoordinateIndex == CurrentCoordinateIndex
                    && x.Slot == slot
                    && x.ObjectType == objectType
                    && !renderers.Select(rend => rend.NameFormatted()).Contains(x.RendererName)
                );
                removedCount += MaterialNamePropertyList.RemoveAll(
                    x => x.CoordinateIndex == CurrentCoordinateIndex
                    && x.Slot == slot
                    && x.ObjectType == objectType
                    && !materialNames.Contains(x.MaterialName.FormatShadingObjectName())
                    && !materialNames.Contains(x.Value)
                );
                removedCount += MaterialFloatPropertyList.RemoveAll(
                    x => x.CoordinateIndex == CurrentCoordinateIndex
                    && x.Slot == slot
                    && x.ObjectType == objectType
                    && (
                        !materialNames.Contains(x.MaterialName)
                        || !materialPropertiesDict.ContainsKey(x.MaterialName)
                        || !materialPropertiesDict[x.MaterialName].Contains(x.Property)
                    )
                );
                removedCount += MaterialColorPropertyList.RemoveAll(
                    x => x.CoordinateIndex == CurrentCoordinateIndex
                    && x.Slot == slot
                    && x.ObjectType == objectType
                    && (
                        !materialNames.Contains(x.MaterialName)
                        || !materialPropertiesDict.ContainsKey(x.MaterialName)
                        || !materialPropertiesDict[x.MaterialName].Contains(x.Property)
                    )
                );
                removedCount += MaterialKeywordPropertyList.RemoveAll(
                    x => x.CoordinateIndex == CurrentCoordinateIndex
                    && x.Slot == slot
                    && x.ObjectType == objectType
                    && (
                        !materialNames.Contains(x.MaterialName)
                        || !materialPropertiesDict.ContainsKey(x.MaterialName)
                        || !materialPropertiesDict[x.MaterialName].Contains(x.Property)
                    )
                );
                removedCount += MaterialTexturePropertyList.RemoveAll(
                    x => x.CoordinateIndex == CurrentCoordinateIndex
                    && x.Slot == slot
                    && x.ObjectType == objectType
                    && (
                        !materialNames.Contains(x.MaterialName)
                        || !materialPropertiesDict.ContainsKey(x.MaterialName)
                        || !materialPropertiesDict[x.MaterialName].Contains(x.Property)
                    )
                );
                removedCount += MaterialShaderList.RemoveAll(
                    x => x.CoordinateIndex == CurrentCoordinateIndex
                    && x.Slot == slot
                    && x.ObjectType == objectType
                    && !materialNames.Contains(x.MaterialName)
                );
                removedCount += MaterialCopyList.RemoveAll(
                    x => x.CoordinateIndex == CurrentCoordinateIndex
                    && x.Slot == slot
                    && x.ObjectType == objectType
                    && !materialNames.Contains(x.MaterialName)
                );
            }
            var purgedTextures = PurgeUnusedTextures();
            if (purgedTextures == 0)
                MaterialEditorPluginBase.Logger.LogMessage($"Removed {removedCount} orphaned propertie(s)");
            else
                MaterialEditorPluginBase.Logger.LogMessage($"Removed {removedCount} orphaned propertie(s) and {purgedTextures} orphaned texture(s)");
        }

        /// <summary>
        /// Purge unused animation
        /// </summary>
        private void PurgeUnusedAnimation()
        {
            MEAnimationUtil.PurgeUnusedAnimation(AnimationControllerMap, MaterialTexturePropertyList);
        }

        /// <summary>
        /// Initialization of animation controllers
        /// </summary>
        private static void InitAnimationController()
        {
            MEAnimationController.UpdateTexture = SetTextureForAnimation;
            MEAnimationController.GetTexID = GetTexIDWithAnimation;
        }

        /// <summary>
        /// Get texture ID from MaterialTextureProperty
        /// </summary>
        private static int? GetTexIDWithAnimation(MaterialTextureProperty property)
        {
            return property.TexID;
        }

        /// <summary>
        /// Set of textures for animation
        /// </summary>
        private static void SetTextureForAnimation(MaterialEditorCharaController controller, GameObject go, MaterialTextureProperty property, int texID)
        {
            if (!controller.TextureDictionary.TryGetValue(texID, out var tex))
                return;

            SetTexture(go, property.MaterialName, property.Property, tex.Texture);
        }

        /// <summary>
        /// Type of object, used for saving MaterialEditor data.
        /// </summary>
        public enum ObjectType
        {
            /// <summary>
            /// Unknown type, things should never be of this type
            /// </summary>
            Unknown,
            /// <summary>
            /// Clothing
            /// </summary>
            Clothing,
            /// <summary>
            /// Accessory
            /// </summary>
            Accessory,
            /// <summary>
            /// Hair
            /// </summary>
            Hair,
            /// <summary>
            /// Parts of a character
            /// </summary>
            Character
        };

    }
}
