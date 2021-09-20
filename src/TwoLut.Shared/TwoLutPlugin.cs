using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Sideloader.AutoResolver;
using Studio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KK_Plugins
{
    // Based on TwoLut.dll by essu
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class TwoLutPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.twolut";
        public const string PluginName = "Two Luts in Studio";
        public const string Version = "1.0";
        public const string PluginNameInternal = Constants.Prefix + "_TwoLut";

        internal static new ManualLogSource Logger;

        private static Dropdown _newDropdown;
        private static KeyValuePair<int, Info.LoadCommonInfo>[] _cachedDicFilterLoadInfo;
        private static AmplifyColorEffect _amplifyColorEffect;

        private static int _currentLut2LocalSlot;
        public static int CurrentLut2LocalSlot
        {
            get => _currentLut2LocalSlot;
            set
            {
                if (_currentLut2LocalSlot == value) return;

                _currentLut2LocalSlot = value;

                int listIndex = Array.FindIndex(_cachedDicFilterLoadInfo, pair => pair.Key == _currentLut2LocalSlot);
                if (listIndex == -1)
                {
                    _currentLut2LocalSlot = 0;
                    _newDropdown.value = 0;
                }
                else
                {
                    _newDropdown.value = listIndex;
                }

                var loadCommonInfo = _cachedDicFilterLoadInfo[listIndex];
                Logger.LogDebug($"Loading lut2: id={listIndex} bundlePath={loadCommonInfo.Value.bundlePath}");
                var lutTexture = CommonLib.LoadAsset<Texture>(loadCommonInfo.Value.bundlePath, loadCommonInfo.Value.fileName, false, string.Empty);
                _amplifyColorEffect.LutBlendTexture = lutTexture;
            }
        }

        private void Start()
        {
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(TwoLutPlugin), nameof(TwoLutPlugin));
            StudioSaveLoadApi.RegisterExtraBehaviour<TwoLutController>(nameof(TwoLutPlugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(SystemButtonCtrl), nameof(SystemButtonCtrl.Init))]
        private static void SystemButtonCtrlInitHook()
        {
            var studioInstance = Studio.Studio.Instance;
            var amplifyColorEffectInfo = studioInstance.systemButtonCtrl.amplifyColorEffectInfo;

            var oldDropdown = amplifyColorEffectInfo.dropdownLut;
            var container = oldDropdown.transform.parent;

            container.GetComponent<LayoutElement>().preferredHeight += 25;

            // Move the blend controls down one position to make space for the second lut dropdown
            OffsetRectTransformY(amplifyColorEffectInfo.icBlend.slider.GetComponent<RectTransform>(), -25);
            OffsetRectTransformY(amplifyColorEffectInfo.icBlend.input.GetComponent<RectTransform>(), -25);
            OffsetRectTransformY(amplifyColorEffectInfo.icBlend.buttonDefault.GetComponent<RectTransform>(), -25);
            OffsetRectTransformY(container.Find("TextMeshPro Blend").GetComponent<RectTransform>(), -25);

            _newDropdown = InsertDuplicateElement(oldDropdown.gameObject, oldDropdown.gameObject).GetComponent<Dropdown>();
            OffsetRectTransformY(_newDropdown.GetComponent<RectTransform>(), -25);
            _newDropdown.template.sizeDelta = new Vector2(0, 950); // Expand the dropdown in case the fix is applied after we copy the dropdown

            var label = container.Find("TextMeshPro Lut").gameObject;
            var newLabel = InsertDuplicateElement(label, _newDropdown.gameObject);
            OffsetRectTransformY(newLabel.GetComponent<RectTransform>(), -25);
            newLabel.GetComponent<TextMeshProUGUI>().text = "色味２";

            _amplifyColorEffect = amplifyColorEffectInfo.ace;
            _cachedDicFilterLoadInfo = Singleton<Info>.Instance.dicFilterLoadInfo.ToArray();

            var dd = _newDropdown.GetComponent<Dropdown>();
            dd.onValueChanged.RemoveAllListeners();
            dd.value = 0;
            dd.onValueChanged.AddListener(listIndex =>
            {
                var loadCommonInfo = _cachedDicFilterLoadInfo[listIndex];
                CurrentLut2LocalSlot = loadCommonInfo.Key;
            });

            var arrowButtons = studioInstance.manipulatePanelCtrl.charaPanelInfo.mpCharCtrl.handInfo.piLeftHand.buttons;
            AddLeftRightDropdownButtons(oldDropdown, arrowButtons);
            AddLeftRightDropdownButtons(_newDropdown, arrowButtons);
        }

        private static GameObject InsertDuplicateElement(GameObject go, GameObject prevSibling)
        {
            var newGo = Instantiate(go, prevSibling.transform.parent, false);
            newGo.transform.SetSiblingIndex(prevSibling.transform.GetSiblingIndex() + 1);
            return newGo;
        }

        private static void OffsetRectTransformY(RectTransform rt, float offset)
        {
            var offMin = rt.offsetMin;
            offMin.y += offset;
            rt.offsetMin = offMin;

            var offMax = rt.offsetMax;
            offMax.y += offset;
            rt.offsetMax = offMax;
        }

        private static void AddLeftRightDropdownButtons(Dropdown targetDropdown, Button[] originalArrowButtons)
        {
            var oldRt = targetDropdown.GetComponent<RectTransform>();

            var leftBtn = InsertDuplicateElement(originalArrowButtons[0].gameObject, oldRt.gameObject).GetComponent<Button>();
            // y -1 because the actual button images don't line up with each other for some reason
            leftBtn.transform.localPosition = new Vector3(oldRt.localPosition.x + oldRt.rect.width + 6, oldRt.localPosition.y - 1, 0);
            leftBtn.onClick.ActuallyRemoveAllListeners();
            leftBtn.onClick.AddListener(() => targetDropdown.value = Mathf.Clamp(targetDropdown.value - 1, 0, _cachedDicFilterLoadInfo.Length - 1));

            var rightBtn = InsertDuplicateElement(originalArrowButtons[1].gameObject, oldRt.gameObject).GetComponent<Button>();
            rightBtn.transform.localPosition = new Vector3(oldRt.localPosition.x + oldRt.rect.width + 30, oldRt.localPosition.y, 0);
            rightBtn.onClick.ActuallyRemoveAllListeners();
            rightBtn.onClick.AddListener(() => targetDropdown.value = Mathf.Clamp(targetDropdown.value + 1, 0, _cachedDicFilterLoadInfo.Length - 1));
        }

        private class TwoLutController : SceneCustomFunctionController
        {
            private const string DataKeyGuid = "guid";
            private const string DataKeySlot = "slot";

            protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
            {
                var ext = GetExtendedData();

                switch (operation)
                {
                    case SceneOperationKind.Load:
                        if (ext == null)
                            goto case SceneOperationKind.Clear;
                        // If slot info is invalid, reset to game default
                        if (!ext.data.TryGetValue(DataKeySlot, out var slot) || !(slot is int slotInt))
                            goto case SceneOperationKind.Clear;
                        // If there's no guid it's a non-zipmod item, use ID directly to get it
                        if (!ext.data.TryGetValue(DataKeyGuid, out var guid) || !(guid is string guidStr))
                        {
                            CurrentLut2LocalSlot = slotInt;
                            break;
                        }
                        var resolveInfo = UniversalAutoResolver.LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.GUID == guidStr && x.Slot == slotInt);
                        // If resolve info is not found (mod missing), try falling back to using the slot as it is
                        CurrentLut2LocalSlot = resolveInfo?.LocalSlot ?? slotInt;
                        break;
                    case SceneOperationKind.Clear:
                        // Game default / midday
                        CurrentLut2LocalSlot = 0;
                        break;
                    case SceneOperationKind.Import:
                        // Importing doesn't copy lut settings
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
                }
            }

            protected override void OnSceneSave()
            {
                PluginData pluginData = null;
                if (CurrentLut2LocalSlot > 0)
                {
                    pluginData = new PluginData();

                    var resolveInfo = UniversalAutoResolver.LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.LocalSlot == CurrentLut2LocalSlot);
                    if (resolveInfo != null)
                    {
                        pluginData.data[DataKeyGuid] = resolveInfo.GUID;
                        pluginData.data[DataKeySlot] = resolveInfo.Slot;
                    }
                    else
                    {
                        // For hardmods and stock items use the local slot ID directly since it never changes
                        pluginData.data[DataKeySlot] = CurrentLut2LocalSlot;
                    }
                }
                SetExtendedData(pluginData);
            }
        }
    }
}