using ChaCustom;
using HarmonyLib;
using System;
using System.Globalization;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class Pushup
    {
        public class SliderManager
        {
            private bool DoEvents = true;

            internal SliderManager(PushupController pushUpController) => InitSliders(pushUpController);

            public void ReinitSliders(PushupController pushUpController)
            {
                DoEvents = false;
                InitSliders(pushUpController);
                DoEvents = true;
            }

            private void InitSliders(PushupController pushUpController)
            {
                var cvsBreast = CustomBase.Instance.gameObject.GetComponentInChildren<CvsBreast>(true);
                var cvsAll = CustomBase.Instance.gameObject.GetComponentInChildren<CvsBodyShapeAll>(true);

                SetupSlider(pushUpController.BaseData.Size, f => pushUpController.BaseData.Size = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexSize] = f, "BustSize", pushUpController, cvsBreast, cvsAll);
                SetupSlider(pushUpController.BaseData.VerticalPosition, f => pushUpController.BaseData.VerticalPosition = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexVerticalPosition] = f, "BustY", pushUpController, cvsBreast, cvsAll);
                SetupSlider(pushUpController.BaseData.HorizontalAngle, f => pushUpController.BaseData.HorizontalAngle = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexHorizontalAngle] = f, "BustRotX", pushUpController, cvsBreast, cvsAll);
                SetupSlider(pushUpController.BaseData.HorizontalPosition, f => pushUpController.BaseData.HorizontalPosition = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexHorizontalPosition] = f, "BustX", pushUpController, cvsBreast, cvsAll);
                SetupSlider(pushUpController.BaseData.VerticalAngle, f => pushUpController.BaseData.VerticalAngle = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexVerticalAngle] = f, "BustRotY", pushUpController, cvsBreast, cvsAll);
                SetupSlider(pushUpController.BaseData.Depth, f => pushUpController.BaseData.Depth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexDepth] = f, "BustSharp", pushUpController, cvsBreast, cvsAll);
                SetupSlider(pushUpController.BaseData.Roundness, f => pushUpController.BaseData.Roundness = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexRoundness] = f, "BustForm", pushUpController, cvsBreast, cvsAll);

                SetupSlider(pushUpController.BaseData.Softness, f => pushUpController.BaseData.Softness = f, f => pushUpController.ChaControl.fileBody.bustSoftness = f, "BustSoftness", pushUpController, cvsBreast, cvsAll);
                SetupSlider(pushUpController.BaseData.Weight, f => pushUpController.BaseData.Weight = f, f => pushUpController.ChaControl.fileBody.bustWeight = f, "BustWeight", pushUpController, cvsBreast, cvsAll);

                SetupSlider(pushUpController.BaseData.AreolaDepth, f => pushUpController.BaseData.AreolaDepth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexAreolaDepth] = f, "AreolaBulge", pushUpController, cvsBreast, cvsAll);
                SetupSlider(pushUpController.BaseData.NippleWidth, f => pushUpController.BaseData.NippleWidth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexNippleWidth] = f, "NipWeight", pushUpController, cvsBreast, cvsAll);
                SetupSlider(pushUpController.BaseData.NippleDepth, f => pushUpController.BaseData.NippleDepth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexNippleDepth] = f, "NipStand", pushUpController, cvsBreast, cvsAll);
            }

            private void SetupSlider(float baseValue, Action<float> sliderSetter, Action<float> bodySetter, string fieldname, PushupController pushupController, CvsBreast cvsBreast, CvsBodyShapeAll cvsBodyShapeAll)
            {
                SetupSlider(baseValue, sliderSetter, bodySetter, fieldname, pushupController, cvsBreast);
                SetupSlider(baseValue, sliderSetter, bodySetter, fieldname, pushupController, cvsBodyShapeAll);
            }

            private void SetupSlider(float baseValue, Action<float> sliderSetter, Action<float> bodySetter, string fieldname, PushupController pushupController, object cvs)
            {
                //Find the sliders for the chest area
                var tv = Traverse.Create(cvs).Field($"sld{fieldname}");
                if (!tv.FieldExists()) return;
                var slider = (Slider)tv.GetValue();
                var input = (TMP_InputField)Traverse.Create(cvs).Field($"inp{fieldname}").GetValue();
                
                bodySetter(baseValue);

                slider.value = baseValue;
                input.text = CustomBase.ConvertTextFromRate(0, 100, baseValue);

                if (!DoEvents) return;
                slider.onValueChanged.AsObservable().Subscribe(value =>
                {
                    if (!DoEvents) return;

                    //When user is updating the chest sliders set the BaseData
                    sliderSetter(value);
                    input.text = Math.Round(value * 100).ToString(CultureInfo.InvariantCulture);
                    pushupController.RecalculateBody();
                });
            }
        }
    }
}