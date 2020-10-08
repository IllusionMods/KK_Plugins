using BepInEx;
using HarmonyLib;
using Illusion.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public const string Version = "1.2";

        private enum CharacterType { Heroine, Partner, Female3P, Player }

        internal void Main()
        {
            //KK Party may not have these directories when first run, create them to avoid errors
            Directory.CreateDirectory(CC.Paths.FemaleCardPath);
            Directory.CreateDirectory(CC.Paths.MaleCardPath);

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
                CreateRandomButton("FreeHScene/Canvas/Panel/3P/FemaleSelectButton", CharacterType.Female3P);
                CreateRandomButton("FreeHScene/Canvas/Panel/3P/MaleSelectButton", CharacterType.Player);
                CreateRandomButton("FreeHScene/Canvas/Panel/Dark/MaleSelectButton", CharacterType.Player);
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
                folderAssist.CreateFolderInfoEx(CC.Paths.MaleCardPath, new[] { "*.png" });
            else
                folderAssist.CreateFolderInfoEx(CC.Paths.FemaleCardPath, new[] { "*.png" });

            //Different fields for different versions of the game, get the correct one
            var listFileObj = folderAssist.GetType().GetField("_lstFile", AccessTools.all)?.GetValue(folderAssist);
            if (listFileObj == null)
                listFileObj = folderAssist.GetType().GetField("lstFile", AccessTools.all)?.GetValue(folderAssist);
            List<FolderAssist.FileInfo> lstFile = (List<FolderAssist.FileInfo>)listFileObj;

            if (lstFile == null || lstFile.Count == 0)
                return;

            lstFile.Randomize();

            //different fields for different versions of the game, get the correct one
            string filePath = (string)lstFile[0].GetType().GetField("fullPath", AccessTools.all)?.GetValue(lstFile[0]);
            if (filePath.IsNullOrEmpty())
                filePath = (string)lstFile[0].GetType().GetField("FullPath", AccessTools.all)?.GetValue(lstFile[0]);

            SetupCharacter(filePath, characterType);
        }
        /// <summary>
        /// Load and set the character
        /// </summary>
        private static void SetupCharacter(string filePath, CharacterType characterType)
        {
            var chaFileControl = new ChaFileControl();
            if (chaFileControl.LoadCharaFile(filePath))
            {
                object member;
                if (Singleton<FreeHScene>.Instance == null)
                {
                    //Use reflection to get the VR version of the character select screen
                    Type VRHSceneType = Type.GetType("VRCharaSelectScene, Assembly-CSharp");
                    var HSceneObject = FindObjectOfType(VRHSceneType);
                    member = HSceneObject.GetType().GetField("member", AccessTools.all).GetValue(HSceneObject);
                }
                else
                    member = Singleton<FreeHScene>.Instance.GetType().GetField("member", AccessTools.all).GetValue(Singleton<FreeHScene>.Instance);

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
                        if (GameObject.Find("Panel/3P/Stage1").activeInHierarchy)
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
                }
            }
        }
    }
}
