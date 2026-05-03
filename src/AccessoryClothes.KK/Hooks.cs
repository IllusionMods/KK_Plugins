using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace KK_Plugins
{
    internal class Hooks
    {
        internal static void InstallHooks() 
        {
            Harmony harmony = new Harmony("AccessoryClothesHarmony");

            Type implantorType = Traverse.Create(AccessTools.TypeByName("ModBoneImplantor.ModBoneImplantor"))
                .Type("AssignedAnotherWeightsHooks").GetValue<Type>();

            if(implantorType != null) 
            {
                MethodBase orig = AccessTools.Method(implantorType, "AssignWeightsAndImplantBones");
                MethodInfo prefix = AccessTools.Method(typeof(Hooks), "AssignWeightsAndImplantBonesPrefix");
                harmony.Patch(orig, new HarmonyMethod(prefix));
            }
        }

        public static void AssignWeightsAndImplantBonesPrefix(ref GameObject obj)
        {
            ListInfoComponent[] parentComponents = obj.GetComponentsInParent<ListInfoComponent>(true);
            ListInfoComponent listInfoComponent = ((parentComponents != null) ? parentComponents.FirstOrDefault() : null);            
            if (listInfoComponent != null && listInfoComponent.GetComponent<ChaAccessoryClothes>() != null)
            {
                obj = listInfoComponent.gameObject;
            }
        }
    }
}
