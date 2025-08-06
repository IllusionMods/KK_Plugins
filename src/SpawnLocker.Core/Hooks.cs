using HarmonyLib;
using KKAPI.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SpawnLocker
{
    internal static class Hooks
    {
        public static void Apply()
        {
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks), SpawnLockMain.GUID);

            var transpiler = new HarmonyMethod(typeof(Hooks), nameof(Hooks.NPCLoadAllTranspile));

            foreach (var targetMethod in typeof(ActionScene).GetMethods(AccessTools.all).Where(x => x.Name == nameof(ActionScene.NPCLoadAll)))
            {
                SpawnLockMain.Logger.LogDebug("Patching: " + targetMethod.FullDescription());
                harmony.PatchMoveNext(targetMethod, transpiler: transpiler);
            }
        }

        private static IEnumerable<CodeInstruction> NPCLoadAllTranspile(IEnumerable<CodeInstruction> instructions)
        {
            var targetMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Take)).MakeGenericMethod(typeof(SaveData.Heroine));
            var newTakeMethod = AccessTools.Method(typeof(SpawnLockMain), nameof(SpawnLockMain._SpawnLockTake));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode != OpCodes.Call || instruction.operand as MethodInfo != targetMethod)
                {
                    yield return instruction;
                }
                else
                {
                    yield return new CodeInstruction(OpCodes.Call, newTakeMethod);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionGame.PreviewClassData), nameof(ActionGame.PreviewClassData.Set))]
        private static void PreviewClassDataSetPostfix(ActionGame.PreviewClassData __instance, SaveData.CharaData charaData)
        {
            _UpdateStatus(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionGame.PreviewClassData), nameof(ActionGame.PreviewClassData.Clear))]
        private static void PreviewClassDataClearPostfix(ActionGame.PreviewClassData __instance)
        {
            _UpdateStatus(__instance);
        }

        static void _UpdateStatus(ActionGame.PreviewClassData __instance)
        {
            var observer = __instance.button.GetComponent<ClickObserver>();

            if (observer != null)
            {
                observer.UpdateStatus();
            }
        }

        [HarmonyPostfix]
#if KK
        [HarmonyPatch(typeof(ActionGame.PreviewClassData), MethodType.Constructor, new System.Type[] { typeof(UnityEngine.GameObject) })]
        private static void PreviewClassDataConstructorPostfix(ActionGame.PreviewClassData __instance)
#elif KKS
        [HarmonyPatch(typeof(ActionGame.PreviewClassData), nameof(ActionGame.PreviewClassData.Awake))]
        private static void PreviewClassAwakePostfix(ActionGame.PreviewClassData __instance)
#endif
        {
            var observer = __instance.button.gameObject.GetOrAddComponent<ClickObserver>();
            observer.previewData = __instance;
        }
    }
}
