using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using System;
using System.Collections.Generic;
using MessagePack;

namespace KK_Plugins
{
    public partial class Pushup
    {
        public class PushupController : CharaCustomFunctionController
        {
            private Dictionary<int, PushupInfo> InfoDictionary = new Dictionary<int, PushupInfo>();

            private bool _forceBodyRecalc;

            private ChaFileBody _activeFileBody;
            private bool _updateChaFile;

            public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
            public PushupInfo CurrentInfo
            {
                get
                {
                    InfoDictionary.TryGetValue(CurrentCoordinateIndex, out var info);
                    if (info == null)
                    {
                        info = new PushupInfo(ChaControl.fileBody);
                        InfoDictionary[CurrentCoordinateIndex] = info;
                    }
                    return info;
                }
                set
                {
                    if (value == null)
                        InfoDictionary[CurrentCoordinateIndex] = new PushupInfo(ChaControl.fileBody);
                    else
                        InfoDictionary[CurrentCoordinateIndex] = value;
                }
            }
            public void RecalculateBody() => _forceBodyRecalc = true;

            protected override void OnReload(GameMode currentGameMode)
            {
                base.OnReload(currentGameMode);
                SliderManager.SlidersActive = false;

                InfoDictionary = new Dictionary<int, PushupInfo>();
                _activeFileBody = ChaControl.fileBody;

                _forceBodyRecalc = true;
            }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                _forceBodyRecalc = true;
            }

            protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
            {

            }

            protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
            {
                _forceBodyRecalc = true;
                if (MakerAPI.GetCoordinateLoadFlags()?.Clothes == false) return;

            }

            protected override void Update()
            {
                if (_updateChaFile)
                {
                    _activeFileBody = ChaControl.fileBody;
                    _updateChaFile = false;
                }

                if (_forceBodyRecalc && ChaControl.fileBody == _activeFileBody)
                {
                    Wearing nowWearing = isWearing();
                    if (nowWearing != Wearing.None)
                    {
                        CurrentInfo.CalculatePush(nowWearing);
                        mapBodyInfoToChaFile(CurrentInfo.PushupData);
                    }
                    else
                    {
                        mapBodyInfoToChaFile(CurrentInfo.BaseData);
                    }
                    _forceBodyRecalc = false;
                }

                base.Update();
            }

            internal void ClothesStateChangeEvent() => _forceBodyRecalc = true;

            private Wearing isWearing()
            {
                var braIsOnAndEnabled = BraIsOnAndEnabled;
                var topIsOnAndEnabled = TopIsOnAndEnabled;

                if (topIsOnAndEnabled)
                {
                    return braIsOnAndEnabled ? Wearing.Both : Wearing.Top;
                }

                return braIsOnAndEnabled ? Wearing.Bra : Wearing.None;
            }

            internal void mapBodyInfoToChaFile(BodyData bodyData)
            {
                void setShapeValue(int idx, float val)
                {
                    ChaControl.fileBody.shapeValueBody[idx] = val;
                    ChaControl.SetShapeBodyValue(idx, val);
                }

                ChaControl.ChangeBustSoftness(bodyData.Softness);
                ChaControl.ChangeBustGravity(bodyData.Weight);

                setShapeValue(PushupConstants.IndexSize, bodyData.Size);
                setShapeValue(PushupConstants.IndexVerticalPosition, bodyData.VerticalPosition);
                setShapeValue(PushupConstants.IndexHorizontalAngle, bodyData.HorizontalAngle);
                setShapeValue(PushupConstants.IndexHorizontalPosition, bodyData.HorizontalPosition);
                setShapeValue(PushupConstants.IndexVerticalAngle, bodyData.VerticalAngle);
                setShapeValue(PushupConstants.IndexDepth, bodyData.Depth);
                setShapeValue(PushupConstants.IndexRoundness, bodyData.Roundness);
                setShapeValue(PushupConstants.IndexAreolaDepth, bodyData.AreolaDepth);
                setShapeValue(PushupConstants.IndexNippleWidth, bodyData.NippleWidth);
                setShapeValue(PushupConstants.IndexNippleDepth, bodyData.NippleDepth);
            }

            private bool BraIsOnAndEnabled => ChaControl.IsClothesStateKind((int)ChaFileDefine.ClothesKind.bra) &&
                       ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.bra] == 0 && CurrentInfo.Bra.EnablePushUp;

            private bool TopIsOnAndEnabled => ChaControl.IsClothesStateKind((int)ChaFileDefine.ClothesKind.top) &&
                       ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.top] == 0 && CurrentInfo.Top.EnablePushUp;
        }

        public class PushupInfo
        {
            public BodyData BaseData;
            public BodyData PushupData;

            public ClothData Bra;
            public ClothData Top;

            public PushupInfo(ChaFileBody baseBody)
            {
                BaseData = new BodyData();
                Bra = new ClothData();
                Top = new ClothData();
                PushupData = new BodyData();

                BaseData.Softness = baseBody.bustSoftness;
                BaseData.Weight = baseBody.bustWeight;
                BaseData.Size = baseBody.shapeValueBody[PushupConstants.IndexSize];
                BaseData.VerticalPosition = baseBody.shapeValueBody[PushupConstants.IndexVerticalPosition];
                BaseData.HorizontalAngle = baseBody.shapeValueBody[PushupConstants.IndexHorizontalAngle];
                BaseData.HorizontalPosition = baseBody.shapeValueBody[PushupConstants.IndexHorizontalPosition];
                BaseData.VerticalAngle = baseBody.shapeValueBody[PushupConstants.IndexVerticalAngle];
                BaseData.Depth = baseBody.shapeValueBody[PushupConstants.IndexDepth];
                BaseData.Roundness = baseBody.shapeValueBody[PushupConstants.IndexRoundness];
                BaseData.AreolaDepth = baseBody.shapeValueBody[PushupConstants.IndexAreolaDepth];
                BaseData.NippleWidth = baseBody.shapeValueBody[PushupConstants.IndexNippleWidth];
                BaseData.NippleDepth = baseBody.shapeValueBody[PushupConstants.IndexNippleDepth];
            }

            internal void CalculatePush(Wearing wearing)
            {
                if (wearing == Wearing.Bra)
                {
                    CalculatePushFromClothes(Bra, Bra.UseAdvanced);
                    return;
                }
                else if (wearing == Wearing.Top)
                {
                    CalculatePushFromClothes(Top, Top.UseAdvanced);
                    return;
                }
                else if (Top.UseAdvanced)
                {
                    CalculatePushFromClothes(Top, true);
                    return;
                }
                else if (Bra.UseAdvanced)
                {
                    CalculatePushFromClothes(Bra, true);
                    return;
                }

                var combo = new ClothData();
                combo.Firmness = Math.Max(Bra.Firmness, Top.Firmness);
                combo.Lift = Math.Max(Bra.Lift, Top.Lift);
                combo.Squeeze = Math.Max(Bra.Squeeze, Top.Squeeze);
                combo.PushTogether = Math.Max(Bra.PushTogether, Top.PushTogether);
                combo.CenterNipples = Math.Max(Bra.CenterNipples, Top.CenterNipples);
                combo.FlattenNipples = Bra.FlattenNipples || Top.FlattenNipples;
                combo.EnablePushUp = true;

                CalculatePushFromClothes(combo, false);
            }

            internal void CalculatePushFromClothes(ClothData cData, bool useAdvanced)
            {
                if (useAdvanced)
                {
                    cData.CopyTo(PushupData);
                    return;
                }

                if (1f - cData.Firmness < BaseData.Softness)
                {
                    PushupData.Softness = 1 - cData.Firmness;
                }
                else
                {
                    PushupData.Softness = BaseData.Softness;
                }

                if (cData.Lift > BaseData.VerticalPosition)
                {
                    PushupData.VerticalPosition = cData.Lift;
                }
                else
                {
                    PushupData.VerticalPosition = BaseData.VerticalPosition;
                }

                if (1f - cData.PushTogether < BaseData.HorizontalAngle)
                {
                    PushupData.HorizontalAngle = 1 - cData.PushTogether;
                }
                else
                {
                    PushupData.HorizontalAngle = BaseData.HorizontalAngle;
                }

                if (1f - cData.PushTogether < BaseData.HorizontalPosition)
                {
                    PushupData.HorizontalPosition = 1 - cData.PushTogether;
                }
                else
                {
                    PushupData.HorizontalPosition = BaseData.HorizontalPosition;
                }

                if (1f - cData.Squeeze < BaseData.Depth)
                {
                    PushupData.Depth = 1 - cData.Squeeze;
                    PushupData.Size = BaseData.Size + (BaseData.Depth - (1 - cData.Squeeze)) / 10;
                }
                else
                {
                    PushupData.Depth = BaseData.Depth;
                    PushupData.Size = BaseData.Size;
                }

                if (cData.FlattenNipples)
                {
                    PushupData.NippleDepth = 0f;
                    PushupData.AreolaDepth = 0f;
                }
                else
                {
                    PushupData.NippleDepth = BaseData.NippleDepth;
                    PushupData.AreolaDepth = BaseData.AreolaDepth;
                }

                PushupData.NippleWidth = BaseData.NippleWidth;

                var nipDeviation = 0.5f - BaseData.VerticalAngle;
                PushupData.VerticalAngle = 0.5f - (nipDeviation - (nipDeviation * cData.CenterNipples));

                PushupData.Weight = BaseData.Weight;
                PushupData.Roundness = BaseData.Roundness;
            }
        }

        public class BodyData
        {
            public float Size { get; set; }
            public float VerticalPosition { get; set; }
            public float HorizontalAngle { get; set; }
            public float HorizontalPosition { get; set; }
            public float VerticalAngle { get; set; }
            public float Depth { get; set; }
            public float Roundness { get; set; }

            public float Softness { get; set; }
            public float Weight { get; set; }

            public float AreolaDepth { get; set; }
            public float NippleWidth { get; set; }
            public float NippleDepth { get; set; }
        }

        public class ClothData : BodyData
        {
            public float Firmness { get; set; } = ConfigFirmnessDefault.Value;
            public float Lift { get; set; } = ConfigLiftDefault.Value;
            public float PushTogether { get; set; } = ConfigPushTogetherDefault.Value;
            public float Squeeze { get; set; } = ConfigSqueezeDefault.Value;
            public float CenterNipples { get; set; } = ConfigNippleCenteringDefault.Value;

            public bool EnablePushUp { get; set; } = ConfigEnablePushup.Value;
            public bool FlattenNipples { get; set; } = ConfigEnablePushup.Value;

            public bool UseAdvanced { get; set; } = false;

            public void CopyTo(BodyData data)
            {
                data.Size = Size;
                data.VerticalPosition = VerticalPosition;
                data.HorizontalAngle = HorizontalAngle;
                data.HorizontalPosition = HorizontalPosition;
                data.VerticalAngle = VerticalAngle;
                data.Depth = Depth;
                data.Roundness = Roundness;
                data.Softness = Softness;
                data.Weight = Weight;
                data.AreolaDepth = AreolaDepth;
                data.NippleWidth = NippleWidth;
                data.NippleDepth = NippleDepth;
            }
        }
    }
}