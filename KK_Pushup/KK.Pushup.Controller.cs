using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MessagePack;
using System;
using System.Collections;
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

            protected override void Start()
            {
                CurrentCoordinate.Subscribe(value => { OnCoordinateChanged(); });
                base.Start();
            }

            protected override void OnCardBeingSaved(GameMode currentGameMode)
            {
                MapBodyInfoToChaFile(BaseData);

                var data = new PluginData();
                data.data.Add("Pushup_PushupData", MessagePackSerializer.Serialize(PushupDataDictionary));
                data.data.Add("Pushup_BraData", MessagePackSerializer.Serialize(BraDataDictionary));
                data.data.Add("Pushup_TopData", MessagePackSerializer.Serialize(TopDataDictionary));
                SetExtendedData(data);

                StartCoroutine(RecalculateBodyCoroutine());
            }

            protected override void OnReload(GameMode currentGameMode)
            {
                BaseData = new BodyData(ChaControl.fileBody);

                PushupDataDictionary = new Dictionary<int, BodyData>();
                BraDataDictionary = new Dictionary<int, ClothData>();
                TopDataDictionary = new Dictionary<int, ClothData>();

                var data = GetExtendedData();
                if (data != null && data.data.TryGetValue("Pushup_PushupData", out var loadedPushupData) && loadedPushupData != null)
                    PushupDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, BodyData>>((byte[])loadedPushupData);

                if (data != null && data.data.TryGetValue("Pushup_BraData", out var loadedBraData) && loadedBraData != null)
                    BraDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedBraData);

                if (data != null && data.data.TryGetValue("Pushup_TopData", out var loadedTopData) && loadedTopData != null)
                    TopDataDictionary = MessagePackSerializer.Deserialize<Dictionary<int, ClothData>>((byte[])loadedTopData);

                RecalculateBody();
                base.OnReload(currentGameMode);
            }

            protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
            {
                var data = new PluginData();
                data.data.Add("PushupCoordinate_PushupData", MessagePackSerializer.Serialize(CurrentPushupData));
                data.data.Add("PushupCoordinate_BraData", MessagePackSerializer.Serialize(CurrentBraData));
                data.data.Add("PushupCoordinate_TopData", MessagePackSerializer.Serialize(CurrentTopData));
                SetCoordinateExtendedData(coordinate, data);
            }

            protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
            {
                RecalculateBody();
                if (MakerAPI.GetCoordinateLoadFlags()?.Clothes == false) return;

                CurrentPushupData = null;
                CurrentBraData = null;
                CurrentTopData = null;

                var data = GetCoordinateExtendedData(coordinate);
                if (data != null && data.data.TryGetValue("PushupCoordinate_BraData", out var loadedBraData) && loadedBraData != null)
                    CurrentBraData = MessagePackSerializer.Deserialize<ClothData>((byte[])loadedBraData);

                if (data != null && data.data.TryGetValue("PushupCoordinate_TopData", out var loadedTopData) && loadedTopData != null)
                    CurrentTopData = MessagePackSerializer.Deserialize<ClothData>((byte[])loadedTopData);

                //Advanced mode data is too character specific and is not loaded from coordinates
                CurrentBraData.UseAdvanced = false;
                CurrentTopData.UseAdvanced = false;
            }

            private void OnCoordinateChanged()
            {
                if (MakerAPI.InsideAndLoaded)
                    ReLoadPushUp();
            }

            public void RecalculateBody()
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
            }

            private IEnumerator RecalculateBodyCoroutine()
            {
                yield return null;
                RecalculateBody();
            }

            internal void ClothesStateChangeEvent()
            {
                if (!Started) return;
                if (CharacterLoading) return;

                RecalculateBody();
            }

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

            private bool characterLoading = false;
            public bool CharacterLoading
            {
                get => characterLoading;
                set
                {
                    characterLoading = value;
                    ChaControl.StartCoroutine(Reset());
                    IEnumerator Reset()
                    {
                        yield return null;
                        characterLoading = false;
                    }
                }
            }
        }
    }
}