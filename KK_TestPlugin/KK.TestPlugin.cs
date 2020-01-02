using BepInEx;
using ChaCustom;
using KKAPI.Maker;
using System;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_Plugins
{
    /// <summary>
    /// Random stuff
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class TestPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.testplugin";
        public const string PluginName = "Test Plugin";
        public const string PluginNameInternal = "KK_TestPlugin";
        public const string Version = "1.0";

        internal void Main()
        {
            SceneManager.sceneLoaded += (s, lsm) => Logger.LogInfo($"Scene loaded: {s.name}");
            MakerAPI.MakerFinishedLoading += MakerFinishedLoading;
        }

        private void MakerFinishedLoading(object sender, EventArgs e)
        {
            //Disable blinking
            CustomBase.Instance.transform.Find("FrontUIGroup/CvsDraw/Top/tglBlink/imgTglCol").GetComponent<Toggle>().isOn = true;
            //Set mouth pattern to smile
            CustomBase.Instance.transform.Find("FrontUIGroup/CvsDraw/Top/grpMouthPtn/ddMouthPtn").GetComponent<TMP_Dropdown>().value = 1;
        }
    }
}