using BepInEx;
using UnityEngine;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class AccessoryClothes : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.accessoryclothes";
        public const string PluginName = "Accessory Clothes";
        public const string PluginNameInternal = Constants.Prefix + "_AccessoryClothes";
        public const string Version = "1.0.2";
    }

    /// <summary>
    /// This component is added to the accessory object in order to run all of the necessary logic.
    /// This plugin doesn't do anything other than load this type so that unity can find it when loading accessories.
    /// Assembly name must be the same between games and never change because it's used by unity to figure out where the component attached to the accessory is coming from.
    /// </summary>
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

#if KK
            // KK darnkess has different enum values for the same names than games without it, so it needs to be obtained dynamically
            var rootBoneKey = (ChaReference.RefObjKey)System.Enum.Parse(typeof(ChaReference.RefObjKey), "A_ROOTBONE");
#else
            var rootBoneKey = ChaReference.RefObjKey.A_ROOTBONE;
#endif
            var objRootBone = chaControl.GetReferenceInfo(rootBoneKey);

            //AssignedWeightsAndSetBounds replaces the bones of an object with the body bones
            for (var index = 0; index < chaAccessory.rendNormal.Length; index++)
                chaControl.aaWeightsBody.AssignedWeightsAndSetBounds(chaAccessory.rendNormal[index].gameObject, "cf_j_root", chaControl.bounds, objRootBone.transform);

            //Get rid of this since it's no longer needed
            Destroy(ArmatureRoot.gameObject);
        }
    }
}
