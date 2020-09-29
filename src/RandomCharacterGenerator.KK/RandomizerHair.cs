using Manager;
using System.Linq;
using UnityEngine;

namespace KK_Plugins
{
    public partial class RandomCharacterGenerator
    {
        private class RandomizerHair
        {
            public static void RandomizeType()
            {
                var chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
                var hair = Custom.hair;

                var categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.bo_hair_b);
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

            public static void RandomizeColor()
            {
                var baseColor = RandomColor();
                Color.RGBToHSV(baseColor, out var h, out var s, out var v);
                var startColor = Color.HSVToRGB(h, s, Mathf.Max(v - 0.3f, 0f));
                var endColor = Color.HSVToRGB(h, s, Mathf.Min(v + 0.15f, 1f));
                var hair = Custom.hair;
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
                Custom.face.eyebrowColor = hair.parts[0].baseColor;
                Custom.body.underhairColor = hair.parts[0].baseColor;
            }

            public static void RandomizeEtc()
            {
                var chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
                var hair = Custom.hair;
                for (var i = 0; i < 4; i++)
                {
                    for (var j = 0; j < 4; j++)
                    {
                        hair.parts[i].acsColor[j] = RandomColor();
                    }
                }
                var categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_hairgloss);
                hair.glossId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
            }
        }
    }
}
