using BepInEx;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace KK_Plugins
{
    public partial class StudioSceneLoadedSound
    {
        internal static class DragAndDropPatches
        {
            private static Harmony harmonyInstance = null;

            internal static void InstallPatches(BaseUnityPlugin dragDropPlugin)
            {
                var assembly = dragDropPlugin?.GetType()?.Assembly;

                InstallPrefix(assembly, "DragAndDrop.StudioHandler", "Scene_Load",
                    AccessTools.Method(typeof(Hooks), nameof(Hooks.OnClickLoadPrefix)));

                InstallPrefix(assembly, "DragAndDrop.StudioHandler", "Scene_Import",
                    AccessTools.Method(typeof(Hooks), nameof(Hooks.OnClickImportPrefix)));
            }

            private static void InstallPrefix(Assembly assembly, string targetTypeName, string targetMethodName, MethodInfo prefixMethod)
            {
                if (assembly != null)
                {
                    var targetMethod = FindTargetMethod(assembly, targetTypeName, targetMethodName);
                    if (targetMethod != null)
                    {
                        Logger.LogDebug($"patching {targetMethod}");

                        harmonyInstance = harmonyInstance ?? new Harmony($"harmonywrapper-auto-{Guid.NewGuid()}");
                        harmonyInstance.Patch(targetMethod, new HarmonyMethod(prefixMethod));
                    }
                }
            }

            internal static MethodBase FindTargetMethod(Assembly assembly, string typeName, string methodName)
            {
                var targetType = assembly?.GetType(typeName);
                if (targetType != null)
                {
                    return AccessTools.Method(targetType, methodName);
                }
                return null;
            }
        }
    }
}