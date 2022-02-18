using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static KK_Plugins.PoseTools.Plugin;

namespace KK_Plugins.PoseTools
{
    internal static class Hooks
    {
        /// <summary>
        /// Create the list but with .png files as well
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(PauseRegistrationList), nameof(PauseRegistrationList.InitList))]
        private static bool PauseRegistrationList_InitList_Prefix(PauseRegistrationList __instance)
        {
            for (int i = 0; i < __instance.transformRoot.childCount; i++)
            {
                UnityEngine.Object.Destroy(__instance.transformRoot.GetChild(i).gameObject);
            }
            __instance.transformRoot.DetachChildren();
            __instance.select = -1;
            __instance.buttonLoad.interactable = false;
            __instance.buttonDelete.interactable = false;
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(UserData.Create(GetFolder()), "*.dat").ToList());
            files.AddRange(Directory.GetFiles(UserData.Create(GetFolder()), "*.png"));
            if (ConfigOrderBy.Value == OrderBy.Filename)
                files.Sort();
            __instance.listPath = files;
            __instance.dicNode.Clear();
            for (int j = 0; j < __instance.listPath.Count; j++)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(__instance.prefabNode);
                gameObject.transform.SetParent(__instance.transformRoot, false);
                StudioNode component = gameObject.GetComponent<StudioNode>();
                component.active = true;
                int no = j;
                component.addOnClick = delegate
                {
                    __instance.OnClickSelect(no);
                };
                component.text = PauseCtrl.LoadName(__instance.listPath[j]);
                __instance.dicNode.Add(j, component);
            }
            return false;
        }

        /// <summary>
        /// Add folders to the list
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(PauseRegistrationList), nameof(PauseRegistrationList.InitList))]
        private static void PauseRegistrationList_InitList_Postfix(PauseRegistrationList __instance)
        {
            if (v_prefabNode == null)
            {
                v_prefabNode = __instance.prefabNode;
                v_transformRoot = __instance.transformRoot;
            }

            var dirs = CurrentDirectory.GetDirectories();
            for (var i = dirs.Length - 1; i >= 0; i--)
            {
                var subDir = dirs[i];
                AddListButton($"[{subDir.Name}]", () =>
                {
                    CurrentDirectory = subDir;
                    __instance.InitList();
                }).SetAsFirstSibling();
            }
            var fn = CurrentDirectory.FullName;
            if (fn.Length > DefaultRootLength)
            {
                AddListButton("..", () =>
                {
                    CurrentDirectory = CurrentDirectory.Parent;
                    __instance.InitList();
                }).SetAsFirstSibling();
            }
        }

        /// <summary>
        /// Replace the Save method
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.Save))]
        private static bool PoseSavePatch(OCIChar _ociChar, ref string _name)
        {
            var filename = $"{DateTime.Now:yyyy_MMdd_HHmm_ss_fff}.png";

            if (ConfigPoseNamePrefix.Value && !_name.IsNullOrEmpty())
                filename = $"{_name}_" + filename;

            if (!ConfigSavePng.Value)
                return true;

            var path = Path.Combine(UserData.Create(GetFolder()), filename);
            var fileInfo = new PauseCtrl.FileInfo(_ociChar);

            try
            {
                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    var buffer = Studio.Studio.Instance.gameScreenShot.CreatePngScreen(320, 180);

                    Texture2D screenshot = TextureFromBytes(buffer, mipmaps: false);
                    screenshot = OverwriteTexture(screenshot, Watermark, 0, screenshot.height - Watermark.height);
                    buffer = screenshot.EncodeToPNG();

                    binaryWriter.Write(buffer);
                    binaryWriter.Write(PauseCtrl.saveIdentifyingCode);
                    binaryWriter.Write(PauseCtrl.saveVersion);
                    binaryWriter.Write(_ociChar.oiCharInfo.sex);
                    binaryWriter.Write(_name);
                    fileInfo.Save(binaryWriter);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError("Failed to save pose .png, falling back to original game code.");
                Plugin.Logger.LogError(ex);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Patch with added PngFile.SkipPng
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.Load))]
        private static bool PauseCtrl_Load(OCIChar _ociChar, ref string _path, ref bool __result)
        {
            if (Path.GetExtension(_path).ToLower() == ".png")
            {
                var fileInfo = new PauseCtrl.FileInfo();
                using (var fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read))
                using (var binaryReader = new BinaryReader(fileStream))
                {
                    PngFile.SkipPng(binaryReader);

                    if (string.CompareOrdinal(binaryReader.ReadString(), PauseCtrl.saveIdentifyingCode) != 0)
                    {
                        __result = false;
                        return false;
                    }

                    int ver = binaryReader.ReadInt32();
                    binaryReader.ReadInt32();
                    binaryReader.ReadString();
                    fileInfo.Load(binaryReader, ver);
                }

                fileInfo.Apply(_ociChar);
                __result = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Patch with added PngFile.SkipPng and skip gender check
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.CheckIdentifyingCode))]
        private static bool PauseCtrl_CheckIdentifyingCode(string _path, ref bool __result)
        {
            var extension = Path.GetExtension(_path).ToLower();

            if (extension == ".png" || extension == ".dat")
            {
                using (FileStream input = new FileStream(_path, FileMode.Open, FileAccess.Read))
                using (BinaryReader binaryReader = new BinaryReader(input))
                {
                    //Skip png data
                    if (extension == ".png")
                        PngFile.SkipPng(binaryReader);
                    //Verify this is a pose, but without the sex check since poses load fine regardless
                    if (string.Compare(binaryReader.ReadString(), "【pose】") != 0)
                        __result = false;
                }
                __result = true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Patch with added PngFile.SkipPng
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl), nameof(PauseCtrl.LoadName))]
        private static bool PauseCtrl_LoadName(string _path, ref string __result)
        {
            if (Path.GetExtension(_path).ToLower() == ".png")
            {
                using (FileStream input = new FileStream(_path, FileMode.Open, FileAccess.Read))
                using (BinaryReader binaryReader = new BinaryReader(input))
                {
                    PngFile.SkipPng(binaryReader);
                    if (string.Compare(binaryReader.ReadString(), "【pose】") != 0)
                        __result = string.Empty;
                    binaryReader.ReadInt32();
                    binaryReader.ReadInt32();
                    __result = binaryReader.ReadString();
                }
                return false;
            }
            return true;
        }
    }
}
