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
            /// <summary>
            /// Controls whether the sliders are allowed to be changed. Should be set to true at all times except when the game is trying to set them to the current body values.
            /// RecalculateBody will disable them before recalculating with bras and tops worn and enable them after.
            /// UpdateCvsBreastPrefix hook will disable them because when the breast tab becomes active it tries to fill in the sliders with current body values.
            /// Moving the mouse on or off the breast tab will enable them once more afterwards.
            /// </summary>
            public static bool SlidersActive = true;
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

                SetupSlider(pushUpController.BaseData.Size, f => pushUpController.BaseData.Size = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexSize] = f, "BustSize", pushUpController, cvsBreast);
                SetupSlider(pushUpController.BaseData.VerticalPosition, f => pushUpController.BaseData.VerticalPosition = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexVerticalPosition] = f, "BustY", pushUpController, cvsBreast);
                SetupSlider(pushUpController.BaseData.HorizontalAngle, f => pushUpController.BaseData.HorizontalAngle = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexHorizontalAngle] = f, "BustRotX", pushUpController, cvsBreast);
                SetupSlider(pushUpController.BaseData.HorizontalPosition, f => pushUpController.BaseData.HorizontalPosition = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexHorizontalPosition] = f, "BustX", pushUpController, cvsBreast);
                SetupSlider(pushUpController.BaseData.VerticalAngle, f => pushUpController.BaseData.VerticalAngle = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexVerticalAngle] = f, "BustRotY", pushUpController, cvsBreast);
                SetupSlider(pushUpController.BaseData.Depth, f => pushUpController.BaseData.Depth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexDepth] = f, "BustSharp", pushUpController, cvsBreast);
                SetupSlider(pushUpController.BaseData.Roundness, f => pushUpController.BaseData.Roundness = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexRoundness] = f, "BustForm", pushUpController, cvsBreast);

                SetupSlider(pushUpController.BaseData.Softness, f => pushUpController.BaseData.Softness = f, f => pushUpController.ChaControl.fileBody.bustSoftness = f, "BustSoftness", pushUpController, cvsBreast);
                SetupSlider(pushUpController.BaseData.Weight, f => pushUpController.BaseData.Weight = f, f => pushUpController.ChaControl.fileBody.bustWeight = f, "BustWeight", pushUpController, cvsBreast);

                SetupSlider(pushUpController.BaseData.AreolaDepth, f => pushUpController.BaseData.AreolaDepth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexAreolaDepth] = f, "AreolaBulge", pushUpController, cvsBreast);
                SetupSlider(pushUpController.BaseData.NippleWidth, f => pushUpController.BaseData.NippleWidth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexNippleWidth] = f, "NipWeight", pushUpController, cvsBreast);
                SetupSlider(pushUpController.BaseData.NippleDepth, f => pushUpController.BaseData.NippleDepth = f, f => pushUpController.ChaControl.fileBody.shapeValueBody[PushupConstants.IndexNippleDepth] = f, "NipStand", pushUpController, cvsBreast);
            }

            private void SetupSlider(float baseValue, Action<float> sliderSetter, Action<float> bodySetter, string fieldname, PushupController pushupController, object cvsBreast)
            {
                //Find the sliders for the chest area
                var slider = (Slider)Traverse.Create(cvsBreast).Field($"sld{fieldname}").GetValue();
                var input = (TMP_InputField)Traverse.Create(cvsBreast).Field($"inp{fieldname}").GetValue();

                bodySetter(baseValue);

                slider.value = baseValue;
                input.text = CustomBase.ConvertTextFromRate(0, 100, baseValue);

                if (!DoEvents) return;
                slider.onValueChanged.AsObservable().Subscribe(value =>
                {
                    if (!DoEvents) return;
                    if (SlidersActive)
                    {
                        //When user is updating the chest sliders set the BaseData
                        sliderSetter(value);
                        input.text = Math.Round(value * 100).ToString(CultureInfo.InvariantCulture);
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