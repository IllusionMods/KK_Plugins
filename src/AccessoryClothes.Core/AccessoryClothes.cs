using BepInEx;
using HarmonyLib;
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
    }

    public class ChaAccessoryClothes : MonoBehaviour
    {
        public Transform ArmatureRoot;

        private void Awake()
        {
            //Move the armature outside of the character so these transforms are not found by certain methods that traverse the body hierarchy
            ArmatureRoot.SetParent(null);
        }

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

            //Get rid of this since it's no longer needed
            Destroy(ArmatureRoot.gameObject);
        }
    }
}
