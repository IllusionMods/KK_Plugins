using Manager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KK_Plugins
{
    public partial class RandomCharacterGenerator
    {
        private class RandomizerBody
        {
            private List<float> _slidersBody;

            public static void RandomizeBody()
            {
                var chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
                var body = Custom.body;

                var categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.mt_body_detail);
                body.detailId = categoryInfo.Keys.ElementAt(Rand.Next(categoryInfo.Keys.Count));
                body.detailPower = 0.5f;

                RandomSkinColor(body.skinMainColor, out var h, out var s, out var v);
                var color = Color.HSVToRGB(h, s, v);
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
                body.underhairColor = Custom.hair.parts[0].baseColor;
                body.nailColor = RandomColor();
                body.nailGlossPower = RandomFloat();
                body.drawAddLine = RandomBool();

            }

            public void RandomizeSliders()
            {
                if (_slidersBody == null) SetTemplate();

                LoadBodySiders(Custom.body, RandomCharacterGenerator.RandomizeSliders(_slidersBody));
            }

            public void SetTemplate()
            {
                _slidersBody = SaveBodySiders(Custom.body);
            }

            private static List<float> SaveBodySiders(ChaFileBody body)
            {
                var res = new List<float>();
                for (var i = 0; i < body.shapeValueBody.Length; i++)
                    res.Add(body.shapeValueBody[i]);
                res.Add(body.areolaSize);
                res.Add(body.bustSoftness);
                res.Add(body.bustWeight);
                res.Add(body.detailPower);
                res.Add(body.nailGlossPower);
                res.Add(body.nipGlossPower);
                res.Add(body.skinGlossPower);

                return res;
            }

            private static void LoadBodySiders(ChaFileBody body, List<float> list)
            {
                var n = 0;
                for (var i = 0; i < body.shapeValueBody.Length; i++)
                    body.shapeValueBody[i] = list[n++];
                body.areolaSize = list[n++];
                body.bustSoftness = list[n++];
                body.bustWeight = list[n++];
                body.detailPower = list[n++];
                body.nailGlossPower = list[n++];
                body.nipGlossPower = list[n++];
                body.skinGlossPower = list[n++];
            }
        }
    }
}