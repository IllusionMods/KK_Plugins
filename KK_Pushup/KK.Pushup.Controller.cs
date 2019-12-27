using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System;
using System.Collections.Generic;
using UniRx;

namespace KK_Plugins
{
    public partial class Pushup
    {
        public class PushupController : CharaCustomFunctionController
        {
            public BodyData BaseData;

            private Dictionary<int, BodyData> PushupDataDictionary = new Dictionary<int, BodyData>();
            private Dictionary<int, ClothData> BraDataDictionary = new Dictionary<int, ClothData>();
            private Dictionary<int, ClothData> TopDataDictionary = new Dictionary<int, ClothData>();

            private bool _forceBodyRecalc;

            protected override void Start()
            {
                CurrentCoordinate.Subscribe(value => { OnCoordinateChanged(value); });
                base.Start();
            }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                MapBodyInfoToChaFile(BaseData);

                var data = new PluginData();
                data.data.Add($"Pushup_{nameof(PushupDataDictionary)}", MessagePackSerializer.Serialize(PushupDataDictionary));
                data.data.Add($"Pushup_{nameof(BraDataDictionary)}", MessagePackSerializer.Serialize(BraDataDictionary));
                data.data.Add($"Pushup_{nameof(TopDataDictionary)}", MessagePackSerializer.Serialize(TopDataDictionary));
                SetExtendedData(data);

                _forceBodyRecalc = true;
            }

            protected override void OnReload(GameMode currentGameMode)
            {
                base.OnReload(currentGameMode);

                BaseData = new BodyData(ChaControl.fileBody);

                PushupDataDictionary = new Dictionary<int, BodyData>();
                BraDataDictionary = new Dictionary<int, ClothData>();
                TopDataDictionary = new Dictionary<int, ClothData>();

                var data = GetExtendedData();
                if (data != null && data.data.TryGetValue($"Pushup_{nameof(PushupDataDictionary)}", out var loadedPushupData) && loadedPushupData != null)
                    PushupDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, BodyData>>((byte[])loadedPushupData);

                if (data != null && data.data.TryGetValue($"Pushup_{nameof(BraDataDictionary)}", out var loadedBraData) && loadedBraData != null)
                    BraDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedBraData);

                if (data != null && data.data.TryGetValue($"Pushup_{nameof(TopDataDictionary)}", out var loadedTopData) && loadedTopData != null)
                    TopDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedTopData);

                _forceBodyRecalc = true;
            }

            protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
            {
                var data = new PluginData();
                data.data.Add($"PushupCoordinate_{nameof(PushupDataDictionary)}", MessagePackSerializer.Serialize(PushupDataDictionary));
                data.data.Add($"PushupCoordinate_{nameof(BraDataDictionary)}", MessagePackSerializer.Serialize(BraDataDictionary));
                data.data.Add($"PushupCoordinate_{nameof(TopDataDictionary)}", MessagePackSerializer.Serialize(TopDataDictionary));
                SetCoordinateExtendedData(coordinate, data);
            }

            protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
            {
                _forceBodyRecalc = true;
                if (MakerAPI.GetCoordinateLoadFlags()?.Clothes == false) return;

                PushupDataDictionary = new Dictionary<int, BodyData>();
                BraDataDictionary = new Dictionary<int, ClothData>();
                TopDataDictionary = new Dictionary<int, ClothData>();

                var data = GetCoordinateExtendedData(coordinate);
                if (data != null && data.data.TryGetValue($"PushupCoordinate_{nameof(PushupDataDictionary)}", out var loadedPushupData) && loadedPushupData != null)
                    PushupDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, BodyData>>((byte[])loadedPushupData);

                if (data != null && data.data.TryGetValue($"PushupCoordinate_{nameof(BraDataDictionary)}", out var loadedBraData) && loadedBraData != null)
                    BraDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedBraData);

                if (data != null && data.data.TryGetValue($"PushupCoordinate_{nameof(TopDataDictionary)}", out var loadedTopData) && loadedTopData != null)
                    TopDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedTopData);
            }

            private void OnCoordinateChanged(ChaFileDefine.CoordinateType coordinateType)
            {
                if (MakerAPI.InsideAndLoaded)
                    ReLoadPushUp();
            }

            protected override void Update()
            {
                if (_forceBodyRecalc)
                {
                    Wearing nowWearing = CurrentlyWearing;
                    if (nowWearing != Wearing.None)
                    {
                        CalculatePush(nowWearing);
                        MapBodyInfoToChaFile(CurrentPushupData);
                    }
                    else
                    {
                        MapBodyInfoToChaFile(BaseData);
                    }
                    _forceBodyRecalc = false;
                }

                base.Update();
            }

            internal void ClothesStateChangeEvent() => _forceBodyRecalc = true;

            /// <summary>
            /// Sets the body values to the values stored in the BodyData.
            /// </summary>
            internal void MapBodyInfoToChaFile(BodyData bodyData)
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

            internal void CalculatePush(Wearing wearing)
            {
                if (wearing == Wearing.Bra)
                {
                    CalculatePushFromClothes(CurrentBraData, CurrentBraData.UseAdvanced);
                    return;
                }
                else if (wearing == Wearing.Top)
                {
                    CalculatePushFromClothes(CurrentTopData, CurrentTopData.UseAdvanced);
                    return;
                }
                else if (CurrentTopData.UseAdvanced)
                {
                    CalculatePushFromClothes(CurrentTopData, true);
                    return;
                }
                else if (CurrentBraData.UseAdvanced)
                {
                    CalculatePushFromClothes(CurrentBraData, true);
                    return;
                }

                var combo = new ClothData();
                combo.Firmness = Math.Max(CurrentBraData.Firmness, CurrentTopData.Firmness);
                combo.Lift = Math.Max(CurrentBraData.Lift, CurrentTopData.Lift);
                combo.Squeeze = Math.Max(CurrentBraData.Squeeze, CurrentTopData.Squeeze);
                combo.PushTogether = Math.Max(CurrentBraData.PushTogether, CurrentTopData.PushTogether);
                combo.CenterNipples = Math.Max(CurrentBraData.CenterNipples, CurrentTopData.CenterNipples);
                combo.FlattenNipples = CurrentBraData.FlattenNipples || CurrentTopData.FlattenNipples;
                combo.EnablePushUp = true;

                CalculatePushFromClothes(combo, false);
            }

            internal void CalculatePushFromClothes(ClothData cData, bool useAdvanced)
            {
                if (useAdvanced)
                {
                    cData.CopyTo(CurrentPushupData);
                    return;
                }

                if (1f - cData.Firmness < BaseData.Softness)
                {
                    CurrentPushupData.Softness = 1 - cData.Firmness;
                }
                else
                {
                    CurrentPushupData.Softness = BaseData.Softness;
                }

                if (cData.Lift > BaseData.VerticalPosition)
                {
                    CurrentPushupData.VerticalPosition = cData.Lift;
                }
                else
                {
                    CurrentPushupData.VerticalPosition = BaseData.VerticalPosition;
                }

                if (1f - cData.PushTogether < BaseData.HorizontalAngle)
                {
                    CurrentPushupData.HorizontalAngle = 1 - cData.PushTogether;
                }
                else
                {
                    CurrentPushupData.HorizontalAngle = BaseData.HorizontalAngle;
                }

                if (1f - cData.PushTogether < BaseData.HorizontalPosition)
                {
                    CurrentPushupData.HorizontalPosition = 1 - cData.PushTogether;
                }
                else
                {
                    CurrentPushupData.HorizontalPosition = BaseData.HorizontalPosition;
                }

                if (1f - cData.Squeeze < BaseData.Depth)
                {
                    CurrentPushupData.Depth = 1 - cData.Squeeze;
                    CurrentPushupData.Size = BaseData.Size + (BaseData.Depth - (1 - cData.Squeeze)) / 10;
                }
                else
                {
                    CurrentPushupData.Depth = BaseData.Depth;
                    CurrentPushupData.Size = BaseData.Size;
                }

                if (cData.FlattenNipples)
                {
                    CurrentPushupData.NippleDepth = 0f;
                    CurrentPushupData.AreolaDepth = 0f;
                }
                else
                {
                    CurrentPushupData.NippleDepth = BaseData.NippleDepth;
                    CurrentPushupData.AreolaDepth = BaseData.AreolaDepth;
                }

                CurrentPushupData.NippleWidth = BaseData.NippleWidth;

                var nipDeviation = 0.5f - BaseData.VerticalAngle;
                CurrentPushupData.VerticalAngle = 0.5f - (nipDeviation - (nipDeviation * cData.CenterNipples));

                CurrentPushupData.Weight = BaseData.Weight;
                CurrentPushupData.Roundness = BaseData.Roundness;
            }

            public BodyData GetPushupData(int coordinateIndex)
            {
                PushupDataDictionary.TryGetValue(coordinateIndex, out var bodyData);
                if (bodyData == null)
                {
                    bodyData = new BodyData(ChaControl.fileBody);
                    PushupDataDictionary[coordinateIndex] = bodyData;
                }
                return bodyData;
            }
            public void SetPushupData(int coordinateIndex, BodyData bodyData)
            {
                if (bodyData == null)
                    PushupDataDictionary[coordinateIndex] = new BodyData(ChaControl.fileBody);
                else
                    PushupDataDictionary[coordinateIndex] = bodyData;
            }
            public BodyData CurrentPushupData
            {
                get => GetPushupData(CurrentCoordinateIndex);
                set => SetPushupData(CurrentCoordinateIndex, value);
            }

            public ClothData GetBraData(int coordinateIndex)
            {
                BraDataDictionary.TryGetValue(coordinateIndex, out var clothData);
                if (clothData == null)
                {
                    clothData = new ClothData(BaseData);
                    BraDataDictionary[coordinateIndex] = clothData;
                }
                return clothData;
            }
            public void SetBraData(int coordinateIndex, ClothData clothData)
            {
                if (clothData == null)
                    BraDataDictionary[coordinateIndex] = new ClothData(BaseData);
                else
                    BraDataDictionary[coordinateIndex] = clothData;
            }
            public ClothData CurrentBraData
            {
                get => GetBraData(CurrentCoordinateIndex);
                set => SetBraData(CurrentCoordinateIndex, value);
            }

            public ClothData GetTopData(int coordinateIndex)
            {
                TopDataDictionary.TryGetValue(coordinateIndex, out var clothData);
                if (clothData == null)
                {
                    clothData = new ClothData(BaseData);
                    TopDataDictionary[coordinateIndex] = clothData;
                }
                return clothData;
            }
            public void SetTopData(int coordinateIndex, ClothData clothData)
            {
                if (clothData == null)
                    TopDataDictionary[coordinateIndex] = new ClothData(BaseData);
                else
                    TopDataDictionary[coordinateIndex] = clothData;
            }
            public ClothData CurrentTopData
            {
                get => GetTopData(CurrentCoordinateIndex);
                set => SetTopData(CurrentCoordinateIndex, value);
            }

            private Wearing CurrentlyWearing
            {
                get
                {
                    var braIsOnAndEnabled = BraIsOnAndEnabled;
                    var topIsOnAndEnabled = TopIsOnAndEnabled;

                    if (topIsOnAndEnabled)
                        return braIsOnAndEnabled ? Wearing.Both : Wearing.Top;

                    return braIsOnAndEnabled ? Wearing.Bra : Wearing.None;
                }
            }

            private bool BraIsOnAndEnabled => ChaControl.IsClothesStateKind((int)ChaFileDefine.ClothesKind.bra) &&
                                              ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.bra] == 0 &&
                                              CurrentBraData.EnablePushUp;

            private bool TopIsOnAndEnabled => ChaControl.IsClothesStateKind((int)ChaFileDefine.ClothesKind.top) &&
                                              ChaControl.fileStatus.clothesState[(int)ChaFileDefine.ClothesKind.top] == 0 &&
                                              CurrentTopData.EnablePushUp;

            public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;

            public void RecalculateBody() => _forceBodyRecalc = true;
        }
    }
}