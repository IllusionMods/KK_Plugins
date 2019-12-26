using ChaCustom;
using HarmonyLib;
using System;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace KK_Plugins
{
    public partial class Pushup
    {
        public class SliderManager
        {
            public static bool SlidersActive;

            public void InitSliders(PushupController pushUpController)
            {
                var cvsBreast = FindObjectOfType(typeof(CvsBreast));

                SetUpSlider(pushUpController.BaseData.Size, f => pushUpController.BaseData.Size = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexSize] = f, "BustSize", pushUpController, cvsBreast);
                SetUpSlider(pushUpController.BaseData.VerticalPosition, f => pushUpController.BaseData.VerticalPosition = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexVerticalPosition] = f, "BustY", pushUpController, cvsBreast);
                SetUpSlider(pushUpController.BaseData.HorizontalAngle, f => pushUpController.BaseData.HorizontalAngle = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexHorizontalAngle] = f, "BustRotX", pushUpController, cvsBreast);
                SetUpSlider(pushUpController.BaseData.HorizontalPosition, f => pushUpController.BaseData.HorizontalPosition = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexHorizontalPosition] = f, "BustX", pushUpController, cvsBreast);
                SetUpSlider(pushUpController.BaseData.VerticalAngle, f => pushUpController.BaseData.VerticalAngle = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexVerticalAngle] = f, "BustRotY", pushUpController, cvsBreast);
                SetUpSlider(pushUpController.BaseData.Depth, f => pushUpController.BaseData.Depth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexDepth] = f, "BustSharp", pushUpController, cvsBreast);
                SetUpSlider(pushUpController.BaseData.Roundness, f => pushUpController.BaseData.Roundness = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexRoundness] = f, "BustForm", pushUpController, cvsBreast);

                SetUpSlider(pushUpController.BaseData.Softness, f => pushUpController.BaseData.Softness = f, f => pushUpController.ChaControl.fileBody.bustSoftness = f, "BustSoftness", pushUpController, cvsBreast);
                SetUpSlider(pushUpController.BaseData.Weight, f => pushUpController.BaseData.Weight = f, f => pushUpController.ChaControl.fileBody.bustWeight = f, "BustWeight", pushUpController, cvsBreast);

                SetUpSlider(pushUpController.BaseData.AreolaDepth, f => pushUpController.BaseData.AreolaDepth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexAreolaDepth] = f, "AreolaBulge", pushUpController, cvsBreast);
                SetUpSlider(pushUpController.BaseData.NippleWidth, f => pushUpController.BaseData.NippleWidth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexNippleWidth] = f, "NipWeight", pushUpController, cvsBreast);
                SetUpSlider(pushUpController.BaseData.NippleDepth, f => pushUpController.BaseData.NippleDepth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexNippleDepth] = f, "NipStand", pushUpController, cvsBreast);
            }

            private void SetUpSlider(float baseValue, Action<float> sliderSetter, Action<float> bodySetter, string fieldname, PushupController pushupController, object cvsBreast)
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
                        //When user is updating the chest sliders set the BaseData
                        sliderSetter(value);
                        input.text = Math.Round(value * 100).ToString();
                        pushupController.RecalculateBody();
                    }
                    else
                    {
                        //Don't allow changes to the data
                        slider.value = baseValue;
                    }
                });
            }
        }
    }
}