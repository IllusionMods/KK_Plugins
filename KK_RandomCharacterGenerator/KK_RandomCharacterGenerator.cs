using BepInEx;
using ChaCustom;
using Harmony;
using KKAPI.Maker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KK_RandomCharacterGenerator
{
    /// <summary>
    /// Displays the name of each scene in the log when it is loaded
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_RandomCharacterGenerator : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.randomcharactergenerator";
        public const string PluginName = "Random Character Generator";
        public const string Version = "1.0";

        private static readonly List<CharaMakerSlider> CharaMakerSliders = new List<CharaMakerSlider>();

        [DisplayName("Random Hotkey")]
        [Description("Press to receive random character in character maker.")]
        public static SavedKeyboardShortcut RandomHotkey { get; private set; }

        void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_RandomCharacterGenerator));

            RandomHotkey = new SavedKeyboardShortcut(nameof(RandomHotkey), nameof(KK_RandomCharacterGenerator), new KeyboardShortcut(KeyCode.Minus));

            foreach (var type in typeof(CvsAccessory).Assembly.GetTypes())
            {
                if (type.Name.StartsWith("Cvs", StringComparison.OrdinalIgnoreCase) &&
                    type != typeof(CvsDrawCtrl) &&
                    type != typeof(CvsColor))
                {
                    var fields = type.GetFields(AccessTools.all);

                    var sliders = fields.Where(x => typeof(Slider).IsAssignableFrom(x.FieldType)).ToList();
                    if (sliders.Count == 0)
                        continue;

                    CharaMakerSliders.Add(new CharaMakerSlider(type, sliders));
                }
            }
        }

        void Update()
        {
            if (RandomHotkey.IsDown())
            {
                if (MakerAPI.InsideAndLoaded)
                {
                    ChaRandom.RandomBody(MakerAPI.GetCharacterControl(), true, true);
                    //ChaRandom.RandomClothes(MakerAPI.GetCharacterControl(), true, true, true);
                    ChaRandom.RandomFace(MakerAPI.GetCharacterControl(), true, true, true, true);
                    ChaRandom.RandomHair(MakerAPI.GetCharacterControl(), true, true, true);
                    ChaRandom.RandomMakeup(MakerAPI.GetCharacterControl());
                    ChaRandom.RandomName(MakerAPI.GetCharacterControl(), true, true, true);
                    ChaRandom.RandomParameter(MakerAPI.GetCharacterControl());
                    RandomizeAllSliders();
                    MakerAPI.GetCharacterControl().Reload();
                }
            }
        }

        private void RandomizeAllSliders()
        {
            var sceneObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            System.Random rand = new System.Random();

            foreach (var target in CharaMakerSliders)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type));

                foreach (var cvs in cvsInstances)
                {
                    if (cvs == null)
                        continue;

                    foreach (var x in target.Sliders)
                    {
                        var slider = (Slider)x.GetValue(cvs);
                        if (slider != null)
                            slider.value = (float)rand.NextDouble();
                    }
                }
            }
        }

        private class CharaMakerSlider
        {
            public CharaMakerSlider(Type type, List<FieldInfo> sliders)
            {
                Type = type;
                Sliders = sliders;
            }
            public readonly Type Type;
            public readonly List<FieldInfo> Sliders;
        }
    }
}
