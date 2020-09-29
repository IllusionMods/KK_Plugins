using Manager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins
{
    public partial class RandomCharacterGenerator
    {
        private class RandomizerFace
        {
            private List<float> _slidersFace;

            public static void RandomizeEyes()
            {
                var chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
                var face = Custom.face;

                Dictionary<int, ListInfoBase> categoryInfo;
                for (var j = 0; j < 2; j++)
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
                Color.RGBToHSV(face.pupil[0].baseColor, out var h, out var s, out var v);
                v = Mathf.Clamp(v - 0.3f, 0f, 1f);
                face.eyelineColor = Color.HSVToRGB(h, s, v);
            }

            public static void RandomizeEtc()
            {
                var chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
                var face = Custom.face;

                var categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.bo_head);
                face.headId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_face_detail);
                face.detailId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                face.detailPower = RandomFloat();
                face.lipGlossPower = RandomFloat();
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_eyebrow);
                face.eyebrowId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                face.eyebrowColor = Custom.hair.parts[0].baseColor;
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_nose);
                face.noseId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                //Not randomizing mole because too many bad textures which would need to be removed manually almost every time
                //categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_mole);
                face.moleId = 0;
                categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_lipline);
                face.lipLineId = RandomBool() ? categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count)) : 0;
                face.lipLineColor = Custom.body.skinSubColor;
                //Color.RGBToHSV(file.custom.body.skinMainColor, out float h2, out float s2, out float num3);
                //face.lipLineColor = Color.HSVToRGB(h2, s2, Mathf.Max(num3 - 0.3f, 0f));
                face.lipGlossPower = RandomFloat();
                face.doubleTooth = RandomBool(5);
            }

            public void RandomizeSliders()
            {
                if (_slidersFace == null) SetTemplate();

                LoadFaceSiders(Custom.face, RandomCharacterGenerator.RandomizeSliders(_slidersFace));
            }

            public void SetTemplate()
            {
                _slidersFace = SaveFaceSiders(Custom.face);
            }

            private static List<float> SaveFaceSiders(ChaFileFace face)
            {
                var res = new List<float>();
                for (var i = 0; i < face.shapeValueFace.Length; i++)
                    res.Add(face.shapeValueFace[i]);
                res.Add(face.cheekGlossPower);
                res.Add(face.detailPower);
                res.Add(face.eyelineUpWeight);
                res.Add(face.hlDownY);
                res.Add(face.hlUpY);
                res.Add(face.lipGlossPower);
                res.Add(face.pupilHeight);
                res.Add(face.pupilWidth);

                return res;
            }

            private static void LoadFaceSiders(ChaFileFace face, List<float> list)
            {
                var n = 0;
                for (var i = 0; i < face.shapeValueFace.Length; i++)
                    face.shapeValueFace[i] = list[n++];
                face.cheekGlossPower = list[n++];
                face.detailPower = list[n++];
                face.eyelineUpWeight = list[n++];
                face.hlDownY = list[n++];
                face.hlUpY = list[n++];
                face.lipGlossPower = list[n++];
                face.pupilHeight = list[n++];
                face.pupilWidth = list[n++];
            }
        }
    }
}
