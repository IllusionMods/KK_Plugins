using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Illusion.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using TMPro;
using UILib;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class FreeHRandom : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.freehrandom";
        public const string PluginName = "Free H Random";
        public const string PluginNameInternal = Constants.Prefix + "_FreeHRandom";
        public const string Version = "1.4";

        private enum CharacterType { Heroine, Partner, Female3P, Player, Darkness }

        internal static new ManualLogSource Logger;

#if KKS
        internal static ConfigEntry<bool> IncludeDefaultMales { get; private set; }
        internal static ConfigEntry<bool> IncludeDefaultFemales { get; private set; }
#endif

        internal void Main()
        {
            Logger = base.Logger;
            //KK Party may not have these directories when first run, create them to avoid errors
            Directory.CreateDirectory(CC.Paths.FemaleCardPath);
            Directory.CreateDirectory(CC.Paths.MaleCardPath);
#if KKS
            Directory.CreateDirectory(CC.Paths.DefaultFemaleCardPath);
            Directory.CreateDirectory(CC.Paths.DefaultMaleCardPath);

            IncludeDefaultMales = Config.Bind("Config", "Include default males", true, "Whether default male cards are included in random selection");
            IncludeDefaultFemales = Config.Bind("Config", "Include default females", true, "Whether default female cards are included in random selection");
#endif

            SceneManager.sceneLoaded += (s, lsm) => InitUI(s.name);
        }

        private void InitUI(string sceneName)
        {
            if (sceneName == "FreeH")
            {
                CreateRandomButton("FreeHScene/Canvas/Panel/Normal/FemaleSelectButton", CharacterType.Heroine);
                CreateRandomButton("FreeHScene/Canvas/Panel/Normal/MaleSelectButton", CharacterType.Player);
                CreateRandomButton("FreeHScene/Canvas/Panel/Masturbation/FemaleSelectButton", CharacterType.Heroine);
                CreateRandomButton("FreeHScene/Canvas/Panel/Lesbian/FemaleSelectButton", CharacterType.Heroine);
                CreateRandomButton("FreeHScene/Canvas/Panel/Lesbian/PartnerSelectButton", CharacterType.Partner);
#if KK
                CreateRandomButton("FreeHScene/Canvas/Panel/3P/FemaleSelectButton", CharacterType.Female3P);
                CreateRandomButton("FreeHScene/Canvas/Panel/3P/MaleSelectButton", CharacterType.Player);
                CreateRandomButton("FreeHScene/Canvas/Panel/Dark/MaleSelectButton", CharacterType.Player);
                CreateRandomButton("FreeHScene/Canvas/Panel/Dark/FemaleSelectButton", CharacterType.Darkness);
#elif KKS
                CreateRandomButton("FreeHScene/Canvas/Panel/3P/main/FemaleSelectButton", CharacterType.Female3P);
                CreateRandomButton("FreeHScene/Canvas/Panel/3P/main/MaleSelectButton", CharacterType.Player);
#endif
            }
            else if (sceneName == "VRFreeHSelect")
            {
                CreateRandomButton("Canvas/Panel/Normal/FemaleSelectButton", CharacterType.Heroine);
                CreateRandomButton("Canvas/Panel/Normal/MaleSelectButton", CharacterType.Player);
                CreateRandomButton("Canvas/Panel/Masturbation/FemaleSelectButton", CharacterType.Heroine);
                CreateRandomButton("Canvas/Panel/Lesbian/FemaleSelectButton", CharacterType.Heroine);
                CreateRandomButton("Canvas/Panel/Lesbian/PartnerSelectButton", CharacterType.Partner);
                CreateRandomButton("Canvas/Panel/3P/main/FemaleSelectButton", CharacterType.Female3P);
                CreateRandomButton("Canvas/Panel/3P/main/MaleSelectButton", CharacterType.Player);
            }
            else if (sceneName == "VRCharaSelect")
            {
                CreateRandomButton("MainCanvas/Panel/Normal/FemaleSelectButton", CharacterType.Heroine);
                CreateRandomButton("MainCanvas/Panel/Normal/MaleSelectButton", CharacterType.Player);
                CreateRandomButton("MainCanvas/Panel/Masturbation/FemaleSelectButton", CharacterType.Heroine);
                CreateRandomButton("MainCanvas/Panel/Lesbian/FemaleSelectButton", CharacterType.Heroine);
                CreateRandomButton("MainCanvas/Panel/Lesbian/PartnerSelectButton", CharacterType.Partner);
                CreateRandomButton("MainCanvas/Panel/3P/FemaleSelectButton", CharacterType.Female3P);
                CreateRandomButton("MainCanvas/Panel/3P/MaleSelectButton", CharacterType.Player);
                CreateRandomButton("MainCanvas/Panel/Dark/MaleSelectButton", CharacterType.Player);
            }
        }
        /// <summary>
        /// Copy the male/female selection button and rewire it in to a Random button
        /// </summary>
        private static void CreateRandomButton(string buttonObjectPath, CharacterType characterType)
        {
            var buttonObject = GameObject.Find(buttonObjectPath);
            if (buttonObject == null)
                return;

            RectTransform buttonToCopy = buttonObject.GetComponent<RectTransform>();
            if (buttonToCopy == null)
                return;

            var copy = Instantiate(buttonToCopy.gameObject);
            copy.name = $"{buttonToCopy.name}Random";
            Button randomButton = copy.GetComponent<Button>();
            Transform randomButtonTransform = randomButton.transform;
            RectTransform testButtonRectTransform = randomButtonTransform as RectTransform;
            randomButtonTransform.SetParent(buttonToCopy.parent, true);
            randomButtonTransform.localScale = buttonToCopy.localScale;
            randomButtonTransform.localPosition = buttonToCopy.localPosition;
#if KKS
            randomButtonTransform.localEulerAngles = Vector3.zero;
#endif
            testButtonRectTransform.SetRect(buttonToCopy.anchorMin, buttonToCopy.anchorMax, buttonToCopy.offsetMin, buttonToCopy.offsetMax);
            testButtonRectTransform.anchoredPosition = buttonToCopy.anchoredPosition + new Vector2(0f, -50f);
            randomButton.onClick = new Button.ButtonClickedEvent();
            randomButton.onClick.AddListener(() => { RandomizeCharacter(characterType); });

            var tmp = copy.transform.Children().FirstOrDefault(x => x.name.StartsWith("TextMeshPro"));
            if (tmp != null)
                tmp.GetComponent<TextMeshProUGUI>().text = CC.Language == 0 ? "ランダム" : "Random";
        }
        /// <summary>
        /// Load the list of character cards and choose a random one
        /// </summary>
        private static void RandomizeCharacter(CharacterType characterType)
        {
            FolderAssist folderAssist = new FolderAssist();

            //Get some random cards
            if (characterType == CharacterType.Player)
            {
                folderAssist.CreateFolderInfoEx(CC.Paths.MaleCardPath, new[] { "*.png" });
#if KKS
                if (IncludeDefaultMales.Value)
                    folderAssist.CreateFolderInfoEx(CC.Paths.DefaultMaleCardPath, new[] { "*.png" }, false);
#endif
            }
            else
            {
                folderAssist.CreateFolderInfoEx(CC.Paths.FemaleCardPath, new[] { "*.png" });
#if KKS
                if (IncludeDefaultFemales.Value)
                    folderAssist.CreateFolderInfoEx(CC.Paths.DefaultFemaleCardPath, new[] { "*.png" }, false);
#endif
            }

            //Different fields for different versions of the game, get the correct one
            var listFileObj = folderAssist.GetType().GetField("_lstFile", AccessTools.all)?.GetValue(folderAssist);
            if (listFileObj == null)
                listFileObj = folderAssist.GetType().GetField("lstFile", AccessTools.all)?.GetValue(folderAssist);

            SetupCharacter((List<FolderAssist.FileInfo>)listFileObj, characterType);
        }

        /// <summary>
        /// Load and set the character
        /// </summary>
        private static void SetupCharacter(List<FolderAssist.FileInfo> lstFile, CharacterType characterType)
        {
            if (lstFile == null || lstFile.Count == 0)
                return;

            var hSceneObject = GetFreeHInstance();

            var chaFileControl = new ChaFileControl();
            var loadSuccess = false;

            lstFile.Randomize();

            var loadCharasUntilSuccess = lstFile.Select(GetFilePath).Where(filePath => chaFileControl.LoadCharaFile(filePath));
#if KK
            if (characterType == CharacterType.Darkness)
            {
                // Need to filter out incompatible personalities for darkness since there's no sanity checks
                var loadM = Traverse.Create(hSceneObject).Method("LoadDarkList");
                var list = loadM.GetValue<ICollection>();
                var availableDarknessPersonalities = list.Cast<object>().Select(x => Traverse.Create(x).Field<int>("personal").Value).ToList();

                loadCharasUntilSuccess = loadCharasUntilSuccess.Where(filePath => availableDarknessPersonalities.Contains(chaFileControl.parameter.personality));
            }
#endif
            loadSuccess = loadCharasUntilSuccess.Any();

            if (loadSuccess)
            {
                var member = hSceneObject.GetType().GetField("member", AccessTools.all).GetValue(hSceneObject);

                switch (characterType)
                {
                    case CharacterType.Heroine:
                        ReactiveProperty<SaveData.Heroine> heroine = (ReactiveProperty<SaveData.Heroine>)Traverse.Create(member).Field("resultHeroine").GetValue();
                        heroine.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        break;
                    case CharacterType.Partner:
                        ReactiveProperty<SaveData.Heroine> resultPartner = (ReactiveProperty<SaveData.Heroine>)Traverse.Create(member).Field("resultPartner").GetValue();
                        resultPartner.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        break;
                    case CharacterType.Female3P:
#if KK
                        if (GameObject.Find("Panel/3P/Stage1").activeInHierarchy)
#elif KKS
                        if (GameObject.Find("Panel/3P/main/Stage1").activeInHierarchy)
#endif
                        {
                            ReactiveProperty<SaveData.Heroine> heroine3P = (ReactiveProperty<SaveData.Heroine>)Traverse.Create(member).Field("resultHeroine").GetValue();
                            heroine3P.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        }
                        else
                        {
                            ReactiveProperty<SaveData.Heroine> resultPartner3P = (ReactiveProperty<SaveData.Heroine>)Traverse.Create(member).Field("resultPartner").GetValue();
                            resultPartner3P.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        }
                        break;
                    case CharacterType.Player:
                        ReactiveProperty<SaveData.Player> resultPlayer = (ReactiveProperty<SaveData.Player>)Traverse.Create(member).Field("resultPlayer").GetValue();
                        resultPlayer.SetValueAndForceNotify(new SaveData.Player(chaFileControl, false));
                        break;
                    case CharacterType.Darkness:
                        ReactiveProperty<SaveData.Heroine> resultDarkHeroine = (ReactiveProperty<SaveData.Heroine>)Traverse.Create(member).Field("resultDarkHeroine").GetValue();
                        resultDarkHeroine.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        break;
                }
            }
            else
            {
                Logger.LogMessage("No applicable cards were found to load. Only topmost male/female directory is searched.");
            }
        }

        private static object GetFreeHInstance()
        {
            if (Singleton<FreeHScene>.Instance != null)
                return Singleton<FreeHScene>.Instance;

            //Use reflection to get the VR version of the character select screen
#if KK
            Type VRHSceneType = Type.GetType("VRCharaSelectScene, Assembly-CSharp");
#elif KKS
            Type VRHSceneType = Type.GetType("VRFreeHSelect, Assembly-CSharp");
#endif
            return FindObjectOfType(VRHSceneType);
        }

        private static string GetFilePath(FolderAssist.FileInfo lstFile)
        {
            //different fields for different versions of the game, get the correct one
            string filePath = (string)lstFile.GetType().GetField("fullPath", AccessTools.all)?.GetValue(lstFile);
            if (filePath.IsNullOrEmpty())
                filePath = (string)lstFile.GetType().GetField("FullPath", AccessTools.all)?.GetValue(lstFile);
            return filePath;
        }
    }
}
