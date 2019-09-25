using BepInEx;
using HarmonyLib;
using Illusion.Extensions;
using KK_Plugins.CommonCode;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UILib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_Plugins
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_FreeHRandom : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.freehrandom";
        public const string PluginName = "Free H Random";
        public const string Version = "1.1.1";

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
            if (sceneName != "FreeH")
                return;

            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Normal/FemaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Heroine);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Normal/MaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Player);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Masturbation/FemaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Heroine);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Lesbian/FemaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Heroine);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Lesbian/PartnerSelectButton")?.GetComponent<RectTransform>(), CharacterType.Partner);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/3P/FemaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Female3P);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/3P/MaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Player);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Dark/MaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Player);
        }
        /// <summary>
        /// Copy the male/female selection button and rewire it in to a Random button
        /// </summary>
        private void CreateRandomButton(RectTransform buttonToCopy, CharacterType characterType)
        {
            if (buttonToCopy == null)
                return;

            var copy = Instantiate(buttonToCopy.gameObject);
            copy.name = $"{buttonToCopy.name}Random";
            Button randomButton = copy.GetComponent<Button>();
            RectTransform testButtonRectTransform = randomButton.transform as RectTransform;
            randomButton.transform.SetParent(buttonToCopy.parent, true);
            randomButton.transform.localScale = buttonToCopy.localScale;
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
        private void RandomizeCharacter(CharacterType characterType)
        {
            FolderAssist folderAssist = new FolderAssist();

            //Get some random cards
            if (characterType == CharacterType.Player)
                folderAssist.CreateFolderInfoEx(CC.Paths.MaleCardPath, new string[] { "*.png" }, true);
            else
                folderAssist.CreateFolderInfoEx(CC.Paths.FemaleCardPath, new string[] { "*.png" }, true);

            //different fields for different versions of the game, get the correct one
            var listFileObj = folderAssist.GetType().GetField("_lstFile", AccessTools.all)?.GetValue(folderAssist);
            if (listFileObj == null)
                listFileObj = folderAssist.GetType().GetField("lstFile", AccessTools.all)?.GetValue(folderAssist);
            List<FolderAssist.FileInfo> lstFile = (List<FolderAssist.FileInfo>)listFileObj;

            if (lstFile.Count() == 0)
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
        private void SetupCharacter(string filePath, CharacterType characterType)
        {
            var chaFileControl = new ChaFileControl();
            if (chaFileControl.LoadCharaFile(filePath, 255, false, true))
            {
                FreeHScene.Member member = (FreeHScene.Member)Singleton<FreeHScene>.Instance.GetType().GetField("member", AccessTools.all).GetValue(Singleton<FreeHScene>.Instance);

                switch (characterType)
                {
                    case CharacterType.Heroine:
                        member.resultHeroine.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        break;
                    case CharacterType.Partner:
                        member.resultPartner.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        break;
                    case CharacterType.Female3P:
                        if (GameObject.Find("FreeHScene/Canvas/Panel/3P/Stage1").activeInHierarchy)
                            member.resultHeroine.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        else
                            member.resultPartner.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        break;
                    case CharacterType.Player:
                        member.resultPlayer.SetValueAndForceNotify(new SaveData.Player(chaFileControl, false));
                        break;
                }
            }
        }
    }
}
