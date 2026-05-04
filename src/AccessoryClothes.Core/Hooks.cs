using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    internal static class Hooks
    {
        internal static void InstallHooks() 
        {
            //Avoid hard dependency on MBI
            Type implantorType = Type.GetType("ModBoneImplantor.ModBoneImplantor, ModBoneImplantor", false)?
                .GetNestedType("AssignedAnotherWeightsHooks", BindingFlags.Public | BindingFlags.NonPublic);

            if (implantorType != null) 
            {
                Harmony harmony = new Harmony("AccessoryClothesHarmony");
                MethodBase orig = AccessTools.Method(implantorType, "AssignWeightsAndImplantBones");
                MethodInfo prefix = AccessTools.Method(typeof(Hooks), "AssignWeightsAndImplantBonesPrefix");
                harmony.Patch(orig, new HarmonyMethod(prefix));
            }
        }


        /// <summary>
        /// In the case of AccessoryClothes, without modification the obj that is passed to AssignWeightsAndImplantBones is the 
        /// wrong one and will only have Transform and SMR components. The BoneImplantProcess components are on obj's parent GameObject.
        /// But we can't do this for all cases, we have to check that we are actually dealing with an AccessoryClothes item, so we check
        /// for the ChaAccessoryClothes MB being present to make sure.
        /// </summary>
        public static void AssignWeightsAndImplantBonesPrefix(ref GameObject obj)
        {
            ListInfoComponent[] parentComponents = obj.GetComponentsInParent<ListInfoComponent>(true); //Singular GetComponentInParent does not have includeInactive parameter in old Unity            
            ListInfoComponent listInfoComponent = ((parentComponents != null) ? parentComponents.FirstOrDefault() : null);
            if (listInfoComponent != null && listInfoComponent.GetComponent<ChaAccessoryClothes>() != null)
            { 
                    obj = listInfoComponent.gameObject;
            }
        }
    }
}
