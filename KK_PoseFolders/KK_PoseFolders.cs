using BepInEx;
using BepInEx.Harmony;
using HarmonyLib;
using Studio;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Events;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_PoseFolders : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.posefolders";
        public const string PluginName = "Pose Folders";
        public const string Version = "1.0";

        private const string USERDATA = "userdata";
        private const string DEFAULT_ROOT = "studio/pose";
        private static readonly int UserdataRoot = new DirectoryInfo(USERDATA).FullName.Length + 1;  //+1 for slash
        private static DirectoryInfo CurrentDirectory = new DirectoryInfo(USERDATA + "/" + DEFAULT_ROOT);
        private static readonly int DefaultRootLength = CurrentDirectory.FullName.Length;

        private static GameObject v_prefabNode;
        private static Transform v_transformRoot;

        internal void Main()
        {
            if (!CurrentDirectory.Exists) CurrentDirectory.Create();
            HarmonyWrapper.PatchAll(typeof(KK_PoseFolders));
        }

        private static string GetFolder()
        {
            if (!CurrentDirectory.Exists)
                CurrentDirectory = new DirectoryInfo(USERDATA + "/" + DEFAULT_ROOT);
            return CurrentDirectory.FullName.Substring(UserdataRoot);
        }

        private static IEnumerable<CodeInstruction> ReplacePath(IEnumerable<CodeInstruction> _instructions)
        {
            var instructions = new List<CodeInstruction>(_instructions);

            for (var i = 0; i < instructions.Count; i++)
            {
                var inst = instructions[i];
                if (inst.opcode == OpCodes.Ldstr && inst.operand?.ToString() == DEFAULT_ROOT)
                {
                    inst.opcode = OpCodes.Call;
                    inst.operand = AccessTools.Method(typeof(KK_PoseFolders), nameof(GetFolder));
                    break;
                }
            }

            return instructions;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(PauseRegistrationList), "InitList")]
        public static IEnumerable<CodeInstruction> tpl_PauseRegistrationList_InitList(IEnumerable<CodeInstruction> _instructions) => ReplacePath(_instructions);

        [HarmonyTranspiler, HarmonyPatch(typeof(PauseCtrl), "Save")]
        public static IEnumerable<CodeInstruction> tpl_PauseCtrl_Save(IEnumerable<CodeInstruction> _instructions) => ReplacePath(_instructions);

        private static Transform AddListButton(string text, UnityAction callback)
        {
            var prefabNode = Instantiate(v_prefabNode);
            prefabNode.transform.SetParent(v_transformRoot, false);
            var component = prefabNode.GetComponent<StudioNode>();
            component.active = true;
            component.addOnClick = callback;
            component.text = text;

            return prefabNode.transform;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PauseRegistrationList), "InitList")]
        public static void post_PauseRegistrationList_InitList(PauseRegistrationList __instance)
        {
            if (v_prefabNode == null)
            {
                v_prefabNode = (GameObject)AccessTools.Field(typeof(PauseRegistrationList), "prefabNode").GetValue(__instance);
                v_transformRoot = (Transform)AccessTools.Field(typeof(PauseRegistrationList), "transformRoot").GetValue(__instance);
            }

            foreach (var subDir in CurrentDirectory.GetDirectories().Reverse())
            {
                AddListButton($"[{subDir.Name}]", () =>
                {
                    CurrentDirectory = subDir;
                    Traverse.Create(__instance).Method("InitList").GetValue();
                }).SetAsFirstSibling();
            }
            var fn = CurrentDirectory.FullName;
            if (fn.Length > DefaultRootLength)
            {
                AddListButton("..", () =>
                {
                    CurrentDirectory = CurrentDirectory.Parent;
                    Traverse.Create(__instance).Method("InitList").GetValue();
                }).SetAsFirstSibling();
            }
        }
    }
}
