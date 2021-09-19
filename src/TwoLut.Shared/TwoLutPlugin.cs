using System;
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

        private static Dropdown newDD;

        private static int[] dicFilterLoadInfoKeys;

        private static StudioResolveInfo _secondLutResolveInfo;
        internal static StudioResolveInfo SecondLutResolveInfo
        {
            get => _secondLutResolveInfo;
            set
            {
                _secondLutResolveInfo = value;
                int ind;
                if (_secondLutResolveInfo == null || (ind = Array.IndexOf(dicFilterLoadInfoKeys, _secondLutResolveInfo.LocalSlot)) == -1) newDD.value = 0;
                else newDD.value = ind;
            }
        }

        private static void OffsetRectTransform(RectTransform rt, Vector2 offset)
        {
            var offMin = rt.offsetMin;
            offMin.y += offset.y;
            rt.offsetMin = offMin;

            var offMax = rt.offsetMax;
            offMax.y += offset.y;
            rt.offsetMax = offMax;
        }

        private static GameObject InsertDuplicateElement(GameObject go, GameObject prevSibling)
        {
            var newGo = Instantiate(go);
            newGo.transform.SetParent(go.transform.parent, false);
            newGo.transform.SetSiblingIndex(prevSibling.transform.GetSiblingIndex() + 1);
            return newGo;
        }

        private Harmony hi = Harmony.CreateAndPatchAll(typeof(TwoLutPlugin), nameof(TwoLutPlugin));

        void Start()
        {
            Logger = base.Logger;

            StudioSaveLoadApi.RegisterExtraBehaviour<kkapi_saveload>(nameof(TwoLutPlugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(SystemButtonCtrl), "Init")]
        public static void Init()
        {
            var acei = Traverse.Create(Studio.Studio.Instance.systemButtonCtrl).Field("amplifyColorEffectInfo").GetValue();
            var oldDD = Traverse.Create(acei).Field<Dropdown>("dropdownLut").Value;

            var container = oldDD.transform.parent;

            container.GetComponent<LayoutElement>().preferredHeight = 80; //45, todo: += 25
            newDD = InsertDuplicateElement(oldDD.gameObject, oldDD.gameObject).GetComponent<Dropdown>();

            var offset = new Vector2(0, -55);
            OffsetRectTransform(newDD.GetComponent<RectTransform>(), offset);

            var label = container.Find("TextMeshPro Lut").gameObject;
            var newLabel = InsertDuplicateElement(label, newDD.gameObject);
            OffsetRectTransform(newLabel.GetComponent<RectTransform>(), offset);

            newLabel.GetComponent<TextMeshProUGUI>().text = "Shade 2";

            dicFilterLoadInfoKeys = Singleton<Info>.Instance.dicFilterLoadInfo.Keys.ToArray();
            var vals = Singleton<Info>.Instance.dicFilterLoadInfo.Values.ToArray();

            var ace = Traverse.Create(acei).Property<AmplifyColorEffect>("ace").Value;
            var dd = newDD.GetComponent<Dropdown>();
            dd.value = 0;
            dd.onValueChanged.AddListener(_no =>
            {
                var loadCommonInfo = vals[_no]; //Well, this is stupid.
                _secondLutResolveInfo = UniversalAutoResolver.LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.LocalSlot == dicFilterLoadInfoKeys[_no]);
                var lutTexture = CommonLib.LoadAsset<Texture>(loadCommonInfo.bundlePath, loadCommonInfo.fileName, false, string.Empty);
                ace.LutBlendTexture = lutTexture;
            });
        }
    }

    public class kkapi_saveload : SceneCustomFunctionController
    {
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            var ext = GetExtendedData();

            object guid;
            object slot;

            if (operation == SceneOperationKind.Clear || ext == null || !ext.data.TryGetValue("guid", out guid) || !ext.data.TryGetValue("slot", out slot))
            {
                TwoLutPlugin.SecondLutResolveInfo = null;
            }
            else if (operation == SceneOperationKind.Load)
            {
                TwoLutPlugin.SecondLutResolveInfo = UniversalAutoResolver.LoadedStudioResolutionInfo.FirstOrDefault(x => x.ResolveItem && x.GUID == (string)guid && x.Slot == (int)slot);
            }
        }

        protected override void OnSceneSave()
        {
            if (TwoLutPlugin.SecondLutResolveInfo == null) return;
            SetExtendedData(new PluginData
            {
                data = {
                { "guid", TwoLutPlugin.SecondLutResolveInfo.GUID },
                { "slot", TwoLutPlugin.SecondLutResolveInfo.Slot },
            }
            });
        }
    }
}