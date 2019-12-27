using ChaCustom;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KK_Plugins
{
    public partial class Pushup
    {
        //Sliders and toggles
        internal static MakerToggle EnablePushupToggle;

        internal static PushupSlider FirmnessSlider;
        internal static PushupSlider LiftSlider;
        internal static PushupSlider PushTogetherSlider;
        internal static PushupSlider SqueezeSlider;
        internal static PushupSlider CenterSlider;

        internal static MakerToggle FlattenNippleToggle;

        internal static MakerToggle AdvancedModeToggle;

        internal static PushupSlider PushSizeSlider;
        internal static PushupSlider PushVerticalPositionSlider;
        internal static PushupSlider PushHorizontalAngleSlider;
        internal static PushupSlider PushHorizontalPositionSlider;
        internal static PushupSlider PushVerticalAngleSlider;
        internal static PushupSlider PushDepthSlider;
        internal static PushupSlider PushRoundnessSlider;

        internal static PushupSlider PushSoftnessSlider;
        internal static PushupSlider PushWeightSlider;

        internal static PushupSlider PushAreolaDepthSlider;
        internal static PushupSlider PushNippleWidthSlider;
        internal static PushupSlider PushNippleDepthSlider;

        internal static MakerRadioButtons SelectButtons;

        private static PushupController _pushUpController;
        private static SliderManager _sliderManager;

        private static ClothData _activeClothData;

        private void MakerFinishedLoading(object sender, EventArgs e)
        {
            ReloadPushup();

            GameObject tglBreast = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/01_BodyTop/tglBreast/BreastTop");
            var tglBreastTrigger = tglBreast.GetOrAddComponent<EventTrigger>();
            var tglBreastPointerEnter = new EventTrigger.Entry();
            tglBreastPointerEnter.eventID = EventTriggerType.PointerEnter;
            tglBreastPointerEnter.callback.AddListener(x => SliderManager.SlidersActive = true);
            tglBreastTrigger.triggers.Add(tglBreastPointerEnter);

            var tglBreastPointerExit = new EventTrigger.Entry();
            tglBreastPointerExit.eventID = EventTriggerType.PointerExit;
            tglBreastPointerExit.callback.AddListener(x => SliderManager.SlidersActive = false);
            tglBreastTrigger.triggers.Add(tglBreastPointerExit);

            GameObject tglPushup = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/03_ClothesTop/tglPushup");
            var tglPushupTrigger = tglPushup.GetOrAddComponent<EventTrigger>();
            var tglPushupEntry = new EventTrigger.Entry();
            tglPushupEntry.eventID = EventTriggerType.PointerEnter;
            tglPushupEntry.callback.AddListener(x => SliderManager.SlidersActive = false);
            tglPushupTrigger.triggers.Add(tglPushupEntry);
        }

        private void MakerExiting(object sender, EventArgs e)
        {
            _pushUpController = null;
            _sliderManager = null;
        }

        private static void ReloadPushup()
        {
            _sliderManager = new SliderManager();

            _pushUpController = GetMakerController();
            _activeClothData = SelectButtons.Value == 0 ? _pushUpController.CurrentBraData : _pushUpController.CurrentTopData;

            _sliderManager.InitSliders(_pushUpController);

            UpdateToggleSubscription(EnablePushupToggle, _activeClothData.EnablePushup, b => { _activeClothData.EnablePushup = b; });

            UpdateSliderSubscription(PushSizeSlider, _activeClothData.Size, f => { _activeClothData.Size = f; });
            UpdateSliderSubscription(PushVerticalPositionSlider, _activeClothData.VerticalPosition, f => { _activeClothData.VerticalPosition = f; });
            UpdateSliderSubscription(PushHorizontalAngleSlider, _activeClothData.HorizontalAngle, f => { _activeClothData.HorizontalAngle = f; });
            UpdateSliderSubscription(PushHorizontalPositionSlider, _activeClothData.HorizontalPosition, f => { _activeClothData.HorizontalPosition = f; });
            UpdateSliderSubscription(PushVerticalAngleSlider, _activeClothData.VerticalAngle, f => { _activeClothData.VerticalAngle = f; });
            UpdateSliderSubscription(PushDepthSlider, _activeClothData.Depth, f => { _activeClothData.Depth = f; });
            UpdateSliderSubscription(PushRoundnessSlider, _activeClothData.Roundness, f => { _activeClothData.Roundness = f; });

            UpdateSliderSubscription(PushSoftnessSlider, _activeClothData.Softness, f => { _activeClothData.Softness = f; });
            UpdateSliderSubscription(PushWeightSlider, _activeClothData.Weight, f => { _activeClothData.Weight = f; });

            UpdateSliderSubscription(PushAreolaDepthSlider, _activeClothData.AreolaDepth, f => { _activeClothData.AreolaDepth = f; });
            UpdateSliderSubscription(PushNippleWidthSlider, _activeClothData.NippleWidth, f => { _activeClothData.NippleWidth = f; });
            UpdateSliderSubscription(PushNippleDepthSlider, _activeClothData.NippleDepth, f => { _activeClothData.NippleDepth = f; });

            UpdateSliderSubscription(FirmnessSlider, _activeClothData.Firmness, f => { _activeClothData.Firmness = f; });
            UpdateSliderSubscription(LiftSlider, _activeClothData.Lift, f => { _activeClothData.Lift = f; });
            UpdateSliderSubscription(PushTogetherSlider, _activeClothData.PushTogether, f => { _activeClothData.PushTogether = f; });
            UpdateSliderSubscription(SqueezeSlider, _activeClothData.Squeeze, f => { _activeClothData.Squeeze = f; });
            UpdateSliderSubscription(CenterSlider, _activeClothData.CenterNipples, f => { _activeClothData.CenterNipples = f; });

            UpdateToggleSubscription(FlattenNippleToggle, _activeClothData.FlattenNipples, b => { _activeClothData.FlattenNipples = b; });

            UpdateToggleSubscription(AdvancedModeToggle, _activeClothData.UseAdvanced, b => { _activeClothData.UseAdvanced = b; });
        }

        private static void UpdateToggleSubscription(MakerToggle toggle, bool value, Action<bool> action)
        {
            var pushObserver = Observer.Create<bool>(b =>
            {
                action(b);
                _pushUpController.RecalculateBody();
            });

            toggle.ValueChanged.Subscribe(pushObserver);
            toggle.SetValue(value);
        }

        private static void UpdateSliderSubscription(PushupSlider slider, float value, Action<float> action)
        {
            slider.onUpdate = f =>
            {
                action(f);
                _pushUpController.RecalculateBody(true);
            };

            var pushObserver = Observer.Create<float>(f => slider.Update(f));

            slider.MakerSlider.ValueChanged.Subscribe(pushObserver);
            slider.MakerSlider.SetValue(value);
        }

        private static PushupController GetMakerController() => MakerAPI.GetCharacterControl().gameObject.GetComponent<PushupController>();

        private void RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent ev)
        {
            MakerCategory category = new MakerCategory("03_ClothesTop", "tglPushup", MakerConstants.Clothes.Bra.Position + 1, "Pushup");

            //Bra or top
            SelectButtons = ev.AddControl(new MakerRadioButtons(category, this, "Type", "Bra", "Top"));
            SelectButtons.ValueChanged.Subscribe(i => ReloadPushup());

            //Basic mode
            EnablePushupToggle = new MakerToggle(category, "Enabled", true, this);
            ev.AddControl(EnablePushupToggle);

            FirmnessSlider = MakeSlider(category, "Firmness", ev, ConfigFirmnessDefault.Value);
            LiftSlider = MakeSlider(category, "Lift", ev, ConfigLiftDefault.Value);
            PushTogetherSlider = MakeSlider(category, "Push Together", ev, ConfigPushTogetherDefault.Value);
            SqueezeSlider = MakeSlider(category, "Squeeze", ev, ConfigSqueezeDefault.Value);
            CenterSlider = MakeSlider(category, "Center Nipples", ev, ConfigNippleCenteringDefault.Value);

            FlattenNippleToggle = new MakerToggle(category, "Flatten Nipples", true, this);
            ev.AddControl(FlattenNippleToggle);

            //Advanced mode
            ev.AddControl(new MakerSeparator(category, this));

            AdvancedModeToggle = new MakerToggle(category, "Advanced Mode", false, this);
            ev.AddControl(AdvancedModeToggle);

            var copyBodyButton = new MakerButton("Copy Body to Advanced", category, this);
            ev.AddControl(copyBodyButton);
            copyBodyButton.OnClick.AddListener(CopyBodyToSliders);

            var copyBasicButton = new MakerButton("Copy Basic to Advanced", category, this);
            ev.AddControl(copyBasicButton);
            copyBasicButton.OnClick.AddListener(CopyBasicToSliders);

            PushSizeSlider = MakeSlider(category, "Size", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexSize], true);
            PushVerticalPositionSlider = MakeSlider(category, "Vertical Position", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexVerticalPosition], true);
            PushHorizontalAngleSlider = MakeSlider(category, "Horizontal Angle", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexHorizontalAngle], true);
            PushHorizontalPositionSlider = MakeSlider(category, "Horizontal Position", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexHorizontalPosition], true);
            PushVerticalAngleSlider = MakeSlider(category, "Vertical Angle", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexVerticalAngle], true);
            PushDepthSlider = MakeSlider(category, "Depth", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexDepth], true);
            PushRoundnessSlider = MakeSlider(category, "Roundness", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexRoundness], true);

            PushSoftnessSlider = MakeSlider(category, "Softness", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.bustSoftness, true);
            PushWeightSlider = MakeSlider(category, "Weight", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.bustWeight, true);

            PushAreolaDepthSlider = MakeSlider(category, "Areola Depth", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexAreolaDepth], true);
            PushNippleWidthSlider = MakeSlider(category, "Nipple Width", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexNippleWidth], true);
            PushNippleDepthSlider = MakeSlider(category, "Nipple Depth", ev, Singleton<CustomBase>.Instance.defChaInfo.custom.body.shapeValueBody[PushupConstants.IndexNippleDepth], true);

            ev.AddSubCategory(category);
        }

        private void CopyBodyToSliders() => CopyToSliders(_pushUpController.BaseData);

        private void CopyBasicToSliders()
        {
            _pushUpController.CalculatePushFromClothes(_activeClothData, false);
            CopyToSliders(_pushUpController.CurrentPushupData);
        }

        private void CopyToSliders(BodyData infoBase)
        {
            PushSoftnessSlider.MakerSlider.SetValue(infoBase.Softness);
            PushWeightSlider.MakerSlider.SetValue(infoBase.Weight);

            PushSizeSlider.MakerSlider.SetValue(infoBase.Size);
            PushVerticalPositionSlider.MakerSlider.SetValue(infoBase.VerticalPosition);
            PushHorizontalAngleSlider.MakerSlider.SetValue(infoBase.HorizontalAngle);
            PushHorizontalPositionSlider.MakerSlider.SetValue(infoBase.HorizontalPosition);
            PushVerticalAngleSlider.MakerSlider.SetValue(infoBase.VerticalAngle);
            PushDepthSlider.MakerSlider.SetValue(infoBase.Depth);
            PushRoundnessSlider.MakerSlider.SetValue(infoBase.Roundness);
            PushAreolaDepthSlider.MakerSlider.SetValue(infoBase.AreolaDepth);
            PushNippleWidthSlider.MakerSlider.SetValue(infoBase.NippleWidth);
            PushNippleDepthSlider.MakerSlider.SetValue(infoBase.NippleDepth);
        }

        private PushupSlider MakeSlider(MakerCategory category, string sliderName, RegisterSubCategoriesEvent e, float defaultValue, bool useConfigMinMax = false)
        {
            float min = 0f;
            float max = 1f;
            if (useConfigMinMax)
            {
                min = (float)ConfigSliderMin.Value / 100;
                max = (float)ConfigSliderMax.Value / 100;
            }

            var slider = new MakerSlider(category, sliderName, min, max, defaultValue, this);
            e.AddControl(slider);
            var pushUpSlider = new PushupSlider();
            pushUpSlider.MakerSlider = slider;

            return pushUpSlider;
        }

        private static void RegisterStudioControls()
        {
            if (!StudioAPI.InsideStudio) return;

            var pushupBraToggle = new CurrentStateCategorySwitch("Pushup Bra", ocichar => ocichar.charInfo.GetComponent<PushupController>().CurrentBraData.EnablePushup);
            StudioAPI.GetOrCreateCurrentStateCategory("Pushup").AddControl(pushupBraToggle);
            pushupBraToggle.Value.Subscribe(value =>
            {
                var controller = GetSelectedController();
                if (controller == null) return;
                if (controller.CurrentBraData.EnablePushup != value)
                {
                    controller.CurrentBraData.EnablePushup = value;
                    controller.RecalculateBody();
                }
            });

            var pushupTopToggle = new CurrentStateCategorySwitch("Pushup Top", ocichar => ocichar.charInfo.GetComponent<PushupController>().CurrentTopData.EnablePushup);
            StudioAPI.GetOrCreateCurrentStateCategory("Pushup").AddControl(pushupTopToggle);
            pushupTopToggle.Value.Subscribe(value =>
            {
                var controller = GetSelectedController();
                if (controller == null) return;
                if (controller.CurrentTopData.EnablePushup != value)
                {
                    controller.CurrentTopData.EnablePushup = value;
                    controller.RecalculateBody();
                }
            });
        }

        private static PushupController GetSelectedController() => FindObjectOfType<MPCharCtrl>()?.ociChar?.charInfo?.GetComponent<PushupController>();
    }
}