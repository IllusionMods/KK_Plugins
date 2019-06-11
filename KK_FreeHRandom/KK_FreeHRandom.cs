using BepInEx;
using Harmony;
using Illusion.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UILib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_FreeHRandom
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_FreeHRandom : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.freehrandom";
        public const string PluginName = "Free H Random";
        public const string PluginNameInternal = "KK_FreeHRandom";
        public const string Version = "1.1";
        private enum CharacterType { Female1, Female2, Female3P, Male }

        private void Main() => SceneManager.sceneLoaded += (s, lsm) => InitUI(s.name);

        private void InitUI(string sceneName)
        {
            if (sceneName != "FreeH")
                return;

            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Normal/FemaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Female1);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Normal/MaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Male);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Masturbation/FemaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Female1);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Lesbian/FemaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Female1);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Lesbian/PartnerSelectButton")?.GetComponent<RectTransform>(), CharacterType.Female2);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/3P/FemaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Female3P);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/3P/MaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Male);
            //CreateRandomButton(GameObject.Find("FreeHScene").transform.Find("Canvas/Panel/Dark/FemaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Female1);
            CreateRandomButton(GameObject.Find("FreeHScene/Canvas/Panel/Dark/MaleSelectButton")?.GetComponent<RectTransform>(), CharacterType.Male);
        }

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
                tmp.GetComponent<TextMeshProUGUI>().text = "Random";
        }
        private void RandomizeCharacter(CharacterType characterType)
        {
            FolderAssist folderAssist = new FolderAssist();

            //Get some random cards
            if (characterType == CharacterType.Male)
                folderAssist.CreateFolderInfoEx(Path.Combine(UserData.Path, "chara/male/"), new string[] { "*.png" }, true);
            else
                folderAssist.CreateFolderInfoEx(Path.Combine(UserData.Path, "chara/female/"), new string[] { "*.png" }, true);

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

        private void SetupCharacter(string filePath, CharacterType characterType)
        {
            var chaFileControl = new ChaFileControl();
            if (chaFileControl.LoadCharaFile(filePath, 255, false, true))
            {
                FreeHScene.Member member = (FreeHScene.Member)Singleton<FreeHScene>.Instance.GetType().GetField("member", AccessTools.all).GetValue(Singleton<FreeHScene>.Instance);

                switch (characterType)
                {
                    case CharacterType.Female1:
                        member.resultHeroine.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        break;
                    case CharacterType.Female2:
                        member.resultPartner.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        break;
                    case CharacterType.Female3P:
                        if (GameObject.Find("FreeHScene/Canvas/Panel/3P/Stage1").activeInHierarchy)
                            member.resultHeroine.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        else
                            member.resultPartner.SetValueAndForceNotify(new SaveData.Heroine(chaFileControl, false));
                        break;
                    case CharacterType.Male:
                        member.resultPlayer.SetValueAndForceNotify(new SaveData.Player(chaFileControl, false));
                        break;
                }
            }
        }
    }

    internal static class Extensions
    {
        private static readonly System.Random rng = new System.Random();
        public static void Randomize<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
