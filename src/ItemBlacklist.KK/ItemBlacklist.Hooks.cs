using ChaCustom;
using HarmonyLib;
using Sideloader.AutoResolver;
using System.Collections.Generic;
using UnityEngine;

namespace KK_Plugins
{
    public partial class ItemBlacklist
    {
        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(CustomSelectListCtrl), "OnPointerEnter")]
            internal static void OnPointerEnterPostfix(CustomSelectListCtrl __instance, GameObject obj)
            {
                CustomSelectListCtrlInstance = __instance;
                CurrentCustomSelectInfoComponent = obj?.GetComponent<CustomSelectInfoComponent>();
                MouseIn = true;
            }
            [HarmonyPostfix, HarmonyPatch(typeof(CustomSelectListCtrl), "OnPointerExit")]
            internal static void OnPointerExitPostfix() => MouseIn = false;

            [HarmonyPostfix, HarmonyPatch(typeof(CustomSelectKind), "Initialize")]
            internal static void CustomSelectKindInitialize(CustomSelectListCtrl ___listCtrl)
            {
                List<CustomSelectInfo> lstSelectInfo = (List<CustomSelectInfo>)Traverse.Create(___listCtrl).Field("lstSelectInfo").GetValue();

                int category = 0;
                int hiddenCount = 0;
                foreach (CustomSelectInfo customSelectInfo in lstSelectInfo)
                {
                    category = customSelectInfo.category;

                    if (customSelectInfo.index >= UniversalAutoResolver.BaseSlotID)
                    {
                        ResolveInfo Info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo)customSelectInfo.category, customSelectInfo.index);
                        if (Info != null)
                        {
                            if (CheckBlacklist(Info.GUID, (int)Info.CategoryNo, Info.Slot))
                            {
                                ___listCtrl.DisvisibleItem(customSelectInfo.index, true);
                                hiddenCount++;
                            }
                        }
                    }
                }
                if (hiddenCount > 0)
                    Logger.LogInfo($"Hiding {hiddenCount} items for category {category} ({(ChaListDefine.CategoryNo)category})");
            }
        }
    }
}