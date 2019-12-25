using ChaCustom;
using HarmonyLib;
using System;
using TMPro;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KK_Plugins
{
    public class SliderManager
    {
        public static bool SlidersActive;

        public void InitSliders(Pushup.PushupController pushUpController)
        {
            var cvsBreast = Object.FindObjectOfType(typeof(CvsBreast));
            var pushUpInfo = pushUpController.CurrentInfo;

            SetUpSlider(pushUpInfo.BaseData.Size, f => pushUpInfo.BaseData.Size = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexSize] = f, "BustSize", pushUpController, cvsBreast);
            SetUpSlider(pushUpInfo.BaseData.VerticalPosition, f => pushUpInfo.BaseData.VerticalPosition = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexVerticalPosition] = f, "BustY", pushUpController, cvsBreast);
            SetUpSlider(pushUpInfo.BaseData.HorizontalAngle, f => pushUpInfo.BaseData.HorizontalAngle = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexHorizontalAngle] = f, "BustRotX", pushUpController, cvsBreast);
            SetUpSlider(pushUpInfo.BaseData.HorizontalPosition, f => pushUpInfo.BaseData.HorizontalPosition = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexHorizontalPosition] = f, "BustX", pushUpController, cvsBreast);
            SetUpSlider(pushUpInfo.BaseData.VerticalAngle, f => pushUpInfo.BaseData.VerticalAngle = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexVerticalAngle] = f, "BustRotY", pushUpController, cvsBreast);
            SetUpSlider(pushUpInfo.BaseData.Depth, f => pushUpInfo.BaseData.Depth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexDepth] = f, "BustSharp", pushUpController, cvsBreast);
            SetUpSlider(pushUpInfo.BaseData.Roundness, f => pushUpInfo.BaseData.Roundness = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexRoundness] = f, "BustForm", pushUpController, cvsBreast);

            SetUpSlider(pushUpInfo.BaseData.Softness, f => pushUpInfo.BaseData.Softness = f, f => pushUpController.ChaControl.fileBody.bustSoftness = f, "BustSoftness", pushUpController, cvsBreast);
            SetUpSlider(pushUpInfo.BaseData.Weight, f => pushUpInfo.BaseData.Weight = f, f => pushUpController.ChaControl.fileBody.bustWeight = f, "BustWeight", pushUpController, cvsBreast);

            SetUpSlider(pushUpInfo.BaseData.AreolaDepth, f => pushUpInfo.BaseData.AreolaDepth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexAreolaDepth] = f, "AreolaBulge", pushUpController, cvsBreast);
            SetUpSlider(pushUpInfo.BaseData.NippleWidth, f => pushUpInfo.BaseData.NippleWidth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexNippleWidth] = f, "NipWeight", pushUpController, cvsBreast);
            SetUpSlider(pushUpInfo.BaseData.NippleDepth, f => pushUpInfo.BaseData.NippleDepth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[Pushup.PushupConstants.IndexNippleDepth] = f, "NipStand", pushUpController, cvsBreast);
        }

        private void SetUpSlider(float baseValue, Action<float> sliderSetter, Action<float> bodySetter, string fieldname, Pushup.PushupController pushupController, Object cvsBreast)
        {
            //Find the sliders for the chest area
            var slider = (Slider)Traverse.Create(cvsBreast).Field($"sld{fieldname}").GetValue();
            var input = (TMP_InputField)Traverse.Create(cvsBreast).Field($"inp{fieldname}").GetValue();
            //var button = (Button)Traverse.Create(cvsBreast).Field($"btn{fieldname}").GetValue();

            slider.onValueChanged = new Slider.SliderEvent();

            bodySetter(baseValue);

            slider.value = baseValue;
            input.text = CustomBase.ConvertTextFromRate(0, 100, baseValue);

            slider.onValueChanged.AsObservable().Subscribe(delegate (float value)
            {
                if (SlidersActive)
                {
                    sliderSetter(value);
                    input.text = Math.Round(value * 100).ToString();
                    pushupController.RecalculateBody();
                }
                else
                {
                    slider.value = baseValue;
                }
            });
        }
    }
}