using BepInEx;
using ChaCustom;
using Harmony;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
/// <summary>
/// Generates random characters in the character maker
/// </summary>
namespace KK_RandomCharacterGenerator
{
    [BepInDependency(KKAPI.KoikatuAPI.GUID)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class KK_RandomCharacterGenerator : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.randomcharactergenerator";
        public const string PluginName = "Random Character Generator";
        public const string Version = "1.0";

        private static readonly List<CharaMakerSlider> CharaMakerSliders = new List<CharaMakerSlider>();
        private static readonly string[] SliderBlacklist = { "sldPupilX", "sldPupilY", "sldController", "sldPtn", "sldPaint" };
        private static Dictionary<string, float> CharacterSliderTemplate = new Dictionary<string, float>();
        private static readonly System.Random Rand = new System.Random();
        private static MakerSlider DeviationSlider;

        private void Main()
        {
            var harmony = HarmonyInstance.Create(GUID);
            harmony.PatchAll(typeof(KK_RandomCharacterGenerator));

            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            MakerAPI.MakerExiting += (s, e) => CharacterSliderTemplate = new Dictionary<string, float>();

            foreach (var type in typeof(CvsAccessory).Assembly.GetTypes())
            {
                if (type.Name.StartsWith("Cvs", StringComparison.OrdinalIgnoreCase) && type != typeof(CvsDrawCtrl) && type != typeof(CvsColor))
                {
                    var fields = type.GetFields(AccessTools.all);

                    var sliders = fields.Where(x => typeof(Slider).IsAssignableFrom(x.FieldType)).ToList();
                    if (sliders.Count == 0)
                        continue;

                    CharaMakerSliders.Add(new CharaMakerSlider(type, sliders));
                }
            }
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            e.AddControl(new MakerText("Character Randomization", MakerConstants.Body.All, this));

            e.AddControl(new MakerButton("Set current character as template", MakerConstants.Body.All, this)).OnClick.AddListener(delegate
            { SetTemplateCharacter(); });

            e.AddControl(new MakerButton("Randomize", MakerConstants.Body.All, this)).OnClick.AddListener(delegate
            { RandomizeCharacter(); });

            DeviationSlider = e.AddControl(new MakerSlider(MakerConstants.Body.All, "Deviation", 0, 1, 0.1f, this));
        }
        /// <summary>
        /// Generate a float with with some sort of deviation from the mean. Maybe. I don't really know what it does.
        /// </summary>
        private static float RandomFloatDeviation(float mean, float deviation)
        {
            double x1 = 1 - Rand.NextDouble();
            double x2 = 1 - Rand.NextDouble();
            double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
            return (float)(y1 * deviation + mean);
        }

        private static double RandomDouble(double minimum, double maximum) => Rand.NextDouble() * (maximum - minimum) + minimum;
        private static float RandomFloat(double minimum, double maximum) => (float)RandomDouble(minimum, maximum);
        private static float RandomFloat() => (float)Rand.NextDouble();
        private static Color RandomColor() => new Color(RandomFloat(), RandomFloat(), RandomFloat());
        private static bool RandomBool(int percentChance = 50) => Rand.Next(100) < percentChance;
        /// <summary>
        /// Record all the values of the sliders for the current character
        /// </summary>
        private void SetTemplateCharacter()
        {
            var sceneObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            bool InitialSetup = CharacterSliderTemplate.Count == 0;

            foreach (var target in CharaMakerSliders)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type));

                foreach (var cvs in cvsInstances.Where(x => x.name != "ShapeTop"))
                {
                    if (cvs == null)
                        continue;

                    foreach (var sliderInfo in target.Sliders)
                    {
                        var slider = (Slider)sliderInfo.GetValue(cvs);
                        if (slider != null)
                        {
                            if (SliderBlacklist.Any(filter => sliderInfo.Name.StartsWith(filter)))
                                continue;

                            CharacterSliderTemplate[sliderInfo.Name] = slider.value;
                        }
                    }
                }
            }
        }
        private void RandomizeCharacter()
        {
            if (CharacterSliderTemplate.Count == 0)
                SetTemplateCharacter();

            RandomBody(MakerAPI.GetCharacterControl().chaFile);
            RandomFace(MakerAPI.GetCharacterControl().chaFile, true, true);
            RandomHair(MakerAPI.GetCharacterControl().chaFile, true, true, true);
            ChaRandom.RandomName(MakerAPI.GetCharacterControl(), true, true, true);
            ChaRandom.RandomParameter(MakerAPI.GetCharacterControl());
            MakerAPI.GetCharacterControl().Reload();
            RandomizeAllSliders();
        }
        /// <summary>
        /// Randomize all the sliders using the template character as the base
        /// </summary>
        private void RandomizeAllSliders()
        {
            var sceneObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var target in CharaMakerSliders)
            {
                var cvsInstances = sceneObjects.SelectMany(x => x.GetComponentsInChildren(target.Type));

                foreach (var cvs in cvsInstances)
                {
                    if (cvs == null)
                        continue;

                    foreach (var sliderInfo in target.Sliders)
                    {
                        var slider = (Slider)sliderInfo.GetValue(cvs);
                        if (slider != null)
                        {
                            if (SliderBlacklist.Any(filter => sliderInfo.Name.StartsWith(filter)))
                                continue;

                            slider.value = RandomFloatDeviation(CharacterSliderTemplate[sliderInfo.Name], DeviationSlider.Value);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Randomize the body
        /// </summary>
        private static void RandomBody(ChaFileControl file)
        {
            ChaListControl chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
            ChaFileBody body = file.custom.body;

            Dictionary<int, ListInfoBase> categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_body_detail);
            body.detailId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
            body.detailPower = 0.5f;
            float h = 0.06f;
            float s = RandomFloat(0.13, 0.39);
            float v = RandomFloat(0.66, 0.98);
            Color color = Color.HSVToRGB(h, s, v);
            body.skinMainColor = color;
            Color.RGBToHSV(body.skinMainColor, out h, out s, out v);
            s = Mathf.Min(1f, s + 0.1f);
            v = Mathf.Max(0f, v - 0.1f);
            color = Color.HSVToRGB(h, s, v);
            color.r = Mathf.Min(1f, color.r + 0.1f);
            body.skinSubColor = color;
            body.skinGlossPower = RandomFloat();
            categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_sunburn);
            body.sunburnId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
            Color.RGBToHSV(body.skinMainColor, out h, out s, out v);
            s = Mathf.Max(0f, s - 0.1f);
            body.sunburnColor = Color.HSVToRGB(h, s, v);
            categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_nip);
            body.nipId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
            Color.RGBToHSV(body.skinMainColor, out h, out s, out v);
            s = Mathf.Min(1f, s + 0.1f);
            body.nipColor = Color.HSVToRGB(h, s, v);
            body.areolaSize = RandomFloat();
            categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_underhair);
            body.underhairId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
            body.underhairColor = file.custom.hair.parts[0].baseColor;
            body.nailColor = RandomColor();
            body.nailGlossPower = RandomFloat();
            body.drawAddLine = RandomBool();
        }
        /// <summary>
        /// Randomize the hair
        /// </summary>
        private static void RandomHair(ChaFileControl file, bool type, bool color, bool etc)
        {
            ChaListControl chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
            ChaFileHair hair = file.custom.hair;

            if (type)
            {
                Dictionary<int, ListInfoBase> categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.bo_hair_b);
                hair.parts[0].id = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.bo_hair_f);
                hair.parts[1].id = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));

                //Side hair
                if (RandomBool(10))
                {
                    hair.parts[2].id = 0;
                }
                else
                {
                    categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.bo_hair_s);
                    hair.parts[2].id = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                }

                //Ahoge
                if (RandomBool(10))
                {
                    hair.parts[3].id = 0;
                }
                else
                {
                    categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.bo_hair_o);
                    hair.parts[3].id = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                }
            }

            if (color)
            {
                Color baseColor = RandomColor();
                Color.RGBToHSV(baseColor, out float h, out float s, out float v);
                Color startColor = Color.HSVToRGB(h, s, Mathf.Max(v - 0.3f, 0f));
                Color endColor = Color.HSVToRGB(h, s, Mathf.Min(v + 0.15f, 1f));
                hair.parts[0].baseColor = baseColor;
                hair.parts[0].startColor = startColor;
                hair.parts[0].endColor = endColor;
                hair.parts[1].baseColor = baseColor;
                hair.parts[1].startColor = startColor;
                hair.parts[1].endColor = endColor;
                hair.parts[2].baseColor = baseColor;
                hair.parts[2].startColor = startColor;
                hair.parts[2].endColor = endColor;
                hair.parts[3].baseColor = baseColor;
                hair.parts[3].startColor = startColor;
                hair.parts[3].endColor = endColor;
                file.custom.face.eyebrowColor = hair.parts[0].baseColor;
                file.custom.body.underhairColor = hair.parts[0].baseColor;
            }

            if (etc)
            {
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        hair.parts[i].acsColor[j] = RandomColor();
                    }
                }
                Dictionary<int, ListInfoBase> categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_hairgloss);
                hair.glossId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
            }
        }
        /// <summary>
        /// Randomize the face
        /// </summary>
        private static void RandomFace(ChaFileControl file, bool eyes, bool etc)
        {
            ChaListControl chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
            ChaFileFace face = file.custom.face;

            if (eyes)
            {
                Dictionary<int, ListInfoBase> categoryInfo;
                for (int j = 0; j < 2; j++)
                {
                    categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_eye);
                    face.pupil[j].id = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                    face.pupil[j].baseColor = RandomColor();
                    face.pupil[j].subColor = RandomColor();
                    categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_eye_gradation);
                    face.pupil[j].gradMaskId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                    face.pupil[j].gradBlend = RandomFloat();
                    face.pupil[j].gradOffsetY = RandomFloat();
                    face.pupil[j].gradScale = RandomFloat();
                }

                if (RandomBool(95))
                    face.pupil[1].Copy(face.pupil[0]);

                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_eye_hi_up);
                face.hlUpId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                face.hlUpColor = RandomBool(5) ? RandomColor() : Color.white;
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_eye_hi_down);
                face.hlDownId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                face.hlDownColor = RandomBool(5) ? RandomColor() : Color.white;
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_eye_white);
                face.whiteId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                face.whiteBaseColor = RandomBool(5) ? RandomColor() : Color.white;
                face.whiteSubColor = RandomBool(5) ? RandomColor() : Color.white;
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_eyeline_up);
                face.eyelineUpId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_eyeline_down);
                face.eyelineDownId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                Color.RGBToHSV(face.pupil[0].baseColor, out float h, out float s, out float v);
                v = Mathf.Clamp(v - 0.3f, 0f, 1f);
                face.eyelineColor = Color.HSVToRGB(h, s, v);
            }
            if (etc)
            {
                Dictionary<int, ListInfoBase> categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.bo_head);
                face.headId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_face_detail);
                face.detailId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                face.detailPower = RandomFloat();
                face.lipGlossPower = RandomFloat();
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_eyebrow);
                face.eyebrowId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                face.eyebrowColor = file.custom.hair.parts[0].baseColor;
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_nose);
                face.noseId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_mole);
                face.moleId = 0;
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_lipline);
                face.lipLineId = RandomBool() ? categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count)) : 0;
                face.lipLineColor = file.custom.body.skinSubColor;
                //Color.RGBToHSV(file.custom.body.skinMainColor, out float h2, out float s2, out float num3);
                //face.lipLineColor = Color.HSVToRGB(h2, s2, Mathf.Max(num3 - 0.3f, 0f));
                face.lipGlossPower = RandomFloat();
                face.doubleTooth = RandomBool(5);
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
