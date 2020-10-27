using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class AccessoryClothes : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.accessoryclothes";
        public const string PluginName = "Accessory Clothes";
        public const string PluginNameInternal = Constants.Prefix + "_AccessoryClothes";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;
        internal static AccessoryClothes Instance;

        internal void Main()
        {
            Logger = base.Logger;
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        /// <summary>
        /// FindLoop but doesn't search through accessories
        /// </summary>
        public static GameObject FindLoopNoAcc(Transform transform, string name)
        {
            if (string.CompareOrdinal(name, transform.gameObject.name) == 0)
                return transform.gameObject;

            if (transform.gameObject.name.StartsWith("ca_slot"))
                return null;

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject gameObject = FindLoopNoAcc(transform.GetChild(i), name);
                if (gameObject != null)
                    return gameObject;
            }

            return null;
        }

        static class Hooks
        {
#if KK
            //Prevent certain methods from searching through accessory bones, if these methods find body bone names within accessories it breaks everything
            //This is done by replacing calls to FindLoop with calls to a similar method that doesn't search accessories
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(Studio.FKCtrl), nameof(Studio.FKCtrl.InitBones))]
            [HarmonyPatch(typeof(Studio.AddObjectAssist), nameof(Studio.AddObjectAssist.InitBone))]
            [HarmonyPatch(typeof(Studio.AddObjectAssist), nameof(Studio.AddObjectAssist.InitHairBone))]
            private static IEnumerable<CodeInstruction> InitBoneTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionsList = instructions.ToList();

                for (var index = 0; index < instructionsList.Count; index++)
                {
                    var x = instructionsList[index];
                    if (x.operand?.ToString() == "UnityEngine.GameObject FindLoop(UnityEngine.Transform, System.String)")
                        x.operand = typeof(AccessoryClothes).GetMethod(nameof(FindLoopNoAcc), AccessTools.all);
                }

                return instructionsList;
            }
#endif
        }
    }

    public class ChaAccessoryClothes : MonoBehaviour
    {
        private void Start()
        {
            ChaAccessoryComponent chaAccessory = gameObject.GetComponent<ChaAccessoryComponent>();
            var chaControl = gameObject.GetComponentInParent<ChaControl>();
            var aaWeightsBody = (AssignedAnotherWeights)Traverse.Create(chaControl).Field("aaWeightsBody").GetValue();
            var bounds = (Bounds)Traverse.Create(chaControl).Field("bounds").GetValue();
            var objRootBone = chaControl.GetReferenceInfo(ChaReference.RefObjKey.A_ROOTBONE);

            //AssignedWeightsAndSetBounds replaces the bones of an object with the body bones
            for (var index = 0; index < chaAccessory.rendNormal.Length; index++)
                aaWeightsBody.AssignedWeightsAndSetBounds(chaAccessory.rendNormal[index].gameObject, "cf_j_root", bounds, objRootBone.transform);
        }
    }
}
