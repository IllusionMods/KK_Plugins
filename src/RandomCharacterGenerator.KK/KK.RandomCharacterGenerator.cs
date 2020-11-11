using BepInEx;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace KK_Plugins
{
    /// <summary>
    /// Generates random characters in the character maker
    /// </summary>
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInIncompatibility("info.jbcs.koikatsu.characterrandomizer")]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class RandomCharacterGenerator : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.randomcharactergenerator";
        public const string PluginName = "Random Character Generator";
        public const string PluginNameInternal = Constants.Prefix + "_RandomCharacterGenerator";
        public const string Version = "2.0";

        internal void Main() => MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            var randomizerBody = new RandomizerBody();
            var randomizerFace = new RandomizerFace();
            var randomizerHair = new RandomizerHair();

            var parentCat = MakerConstants.Body.All;
            var cat = new MakerCategory(parentCat.CategoryName, "RandomCharacterGeneratorCategory", parentCat.Position + 5, "Randomize");
            e.AddSubCategory(cat);

            e.AddControl(new MakerButton("Set current character as template", cat, this)).OnClick.AddListener(() =>
            {
                randomizerBody.SetTemplate();
                randomizerFace.SetTemplate();
            });

            var randButton = e.AddControl(new MakerButton("Randomize!", cat, this));

            e.AddControl(new MakerSeparator(cat, this));
            var randomizeBodySliders = e.AddControl(new MakerToggle(cat, "Randomize body sliders", this));
            var randomizeBody = e.AddControl(new MakerToggle(cat, "Randomize body type", this));
            _skinColorRadio = e.AddControl(new MakerRadioButtons(cat, this, "Skin color", "White", "Brown", "Unchanged"));

            e.AddControl(new MakerSeparator(cat, this));
            var randomizeFaceSliders = e.AddControl(new MakerToggle(cat, "Randomize face sliders", this));
            var randomizeFaceEyes = e.AddControl(new MakerToggle(cat, "Randomize eyes", this));
            var randomizeFaceEtc = e.AddControl(new MakerToggle(cat, "Randomize face type", this));

            e.AddControl(new MakerSeparator(cat, this));
            var randomizeHair = e.AddControl(new MakerToggle(cat, "Randomize hair type", this));
            var randomizeHairColor = e.AddControl(new MakerToggle(cat, "Randomize hair color", this));

            e.AddControl(new MakerSeparator(cat, this));
            _deviationSlider = e.AddControl(new MakerSlider(cat, "Deviation", 0, 1, 0.1f, this));

            randomizeBodySliders.Value = true;
            randomizeFaceSliders.Value = true;

            randButton.OnClick.AddListener(() =>
            {
                if (randomizeBody.Value) RandomizerBody.RandomizeBody();
                if (randomizeBodySliders.Value) randomizerBody.RandomizeSliders();
                if (randomizeFaceEyes.Value) RandomizerFace.RandomizeEyes();
                if (randomizeFaceEtc.Value) RandomizerFace.RandomizeEtc();
                if (randomizeFaceSliders.Value) randomizerFace.RandomizeSliders();
                if (randomizeHair.Value) RandomizerHair.RandomizeType();
                if (randomizeHair.Value) RandomizerHair.RandomizeEtc();
                if (randomizeHairColor.Value) RandomizerHair.RandomizeColor();

                MakerAPI.GetCharacterControl().Reload();
            });
        }

        #region Random utils

        public static readonly Random Rand = new Random();
        private static MakerRadioButtons _skinColorRadio;
        private static MakerSlider _deviationSlider;

        public static ChaFileControl Chararacter => MakerAPI.GetCharacterControl().chaFile;
        public static ChaFileCustom Custom => Chararacter.custom;

        public static float RandomFloatDeviation(float mean, float deviation)
        {
            var x1 = 1 - Rand.NextDouble();
            var x2 = 1 - Rand.NextDouble();
            var y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
            return (float)(y1 * deviation + mean);
        }

        public static double RandomDouble(double minimum, double maximum) => Rand.NextDouble() * (maximum - minimum) + minimum;
        public static float RandomFloat(double minimum, double maximum) => (float)RandomDouble(minimum, maximum);
        public static float RandomFloat() => (float)Rand.NextDouble();
        public static Color RandomColor() => new Color(RandomFloat(), RandomFloat(), RandomFloat());
        public static bool RandomBool(int percentChance = 50) => Rand.Next(100) < percentChance;

        public static void RandomSkinColor(Color currentColor, out float h, out float s, out float v)
        {
            switch (_skinColorRadio.Value)
            {
                case 0: // White
                    h = 0.06f;
                    RandomPointInTriangle(0.02f, 1f, 0.1f, 0.91f, 0.11f, 1f, out s, out v);
                    break;
                case 1: // Brown
                    h = 0.06f;
                    s = RandomFloat(0.13, 0.39);
                    v = RandomFloat(0.66, 0.98);
                    break;
                default: // Unchanged
                    Color.RGBToHSV(currentColor, out h, out s, out v);
                    break;
            }
        }

        public static List<float> RandomizeSliders(List<float> list)
        {
            var res = new List<float>(list);
            var dev = _deviationSlider.Value;

            for (var i = 0; i < list.Count; i++)
            {
                var v = RandomFloatDeviation(res[i], dev);
                if (v < -1) v = -1;
                if (v > 2) v = 2;

                res[i] = v;
            }

            return res;
        }

        public static void RandomPointInTriangle(float x1, float y1, float x2, float y2, float x3, float y3, out float x, out float y)
        {
            var r1 = (float)Rand.NextDouble();
            var r2 = (float)Rand.NextDouble();
            var sqr1 = (float)Math.Sqrt(r1);

            x = (1 - sqr1) * x1 + sqr1 * (1 - r2) * x2 + sqr1 * r2 * x3;
            y = (1 - sqr1) * y1 + sqr1 * (1 - r2) * y2 + sqr1 * r2 * y3;
        }

        #endregion
    }
}
