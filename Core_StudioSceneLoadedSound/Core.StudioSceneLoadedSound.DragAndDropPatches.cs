using BepInEx;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using System;
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

                StudioSaveLoadApi.SceneLoad += SceneLoad;

                InstallPrefix(assembly, "DragAndDrop.StudioHandler", "Scene_Load",
                    AccessTools.Method(typeof(DragAndDropPatches), nameof(DragAndDrop_StudioHandler_Prefix)));

                InstallPrefix(assembly, "DragAndDrop.StudioHandler", "Scene_Import",
                    AccessTools.Method(typeof(DragAndDropPatches), nameof(DragAndDrop_StudioHandler_Prefix)));
            }

            private static void InstallPrefix(Assembly assembly, string targetTypeName, string targetMethodName, MethodInfo prefixMethod)
            {
                if (assembly != null)
                {
                    var targetMethod = FindTargetMethod(assembly, targetTypeName, targetMethodName);
                    if (targetMethod != null)
                    {
#if DEBUG
                        Logger.LogDebug($"patching {targetMethod}");
#endif
                        harmonyInstance = harmonyInstance ?? new Harmony($"harmonywrapper-auto-{Guid.NewGuid()}");
                        harmonyInstance.Patch(targetMethod, new HarmonyMethod(prefixMethod));
                    }
                }
            }

            internal static void DragAndDrop_StudioHandler_Prefix()
            {
                DragAndDropped = true;
            }

            private static void SceneLoad(object sender, SceneLoadEventArgs e)
            {
                if (DragAndDropped && (e.Operation == SceneOperationKind.Load || e.Operation == SceneOperationKind.Import))
                {
                    DragAndDropped = false;
                    PlayAlertSound();
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